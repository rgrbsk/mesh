using Microsoft.EntityFrameworkCore;
using Usuario = Erp.Model.Usuario.Usuario;

namespace Erp.Data.Tenancy
{
    /// <summary>
    /// O login mora no banco central, mas os dados do tenant apontam para o
    /// usuário (quem pediu, quem aprovou, quem recebe a notificação). Por isso
    /// cada banco de tenant guarda uma CÓPIA dos seus usuários — sem senha. Esta
    /// classe mantém a cópia igual ao original. No tenant que mora no banco
    /// central não há cópia: a tabela é a mesma.
    /// </summary>
    public sealed class EspelhoUsuarios
    {
        private readonly ConexoesTenant _conexoes;

        public EspelhoUsuarios(ConexoesTenant conexoes) => _conexoes = conexoes;

        public async Task Sincronizar(Guid usuarioId)
        {
            await using var central = _conexoes.Central();

            var original = await central.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (original?.EmpresaId is not { } empresaId)
                return;

            var banco = await central.Empresas.Where(e => e.Id == empresaId).Select(e => e.Banco).FirstOrDefaultAsync();
            if (_conexoes.EhCentral(banco))
                return;

            await using var tenant = _conexoes.Criar(banco);
            await Gravar(tenant, original);
        }

        /// <summary>Cria ou atualiza a cópia no banco do tenant.</summary>
        public static async Task Gravar(AppDbContext tenant, Usuario original)
        {
            var copia = await tenant.Usuarios.FirstOrDefaultAsync(u => u.Id == original.Id);
            var nova = copia is null;
            copia ??= new Usuario();

            copia.Id = original.Id;
            copia.UserName = original.UserName;
            copia.NormalizedUserName = original.NormalizedUserName;
            copia.Email = original.Email;
            copia.NormalizedEmail = original.NormalizedEmail;
            copia.EmailConfirmed = original.EmailConfirmed;
            copia.Nome = original.Nome;
            copia.Sobrenome = original.Sobrenome;
            copia.Status = original.Status;
            copia.CPF = original.CPF;
            copia.Cargo = original.Cargo;
            copia.NumeroContato = original.NumeroContato;
            copia.Observacao = original.Observacao;
            copia.Tag = original.Tag;
            copia.TemaPreferencial = original.TemaPreferencial;
            copia.DataCadastro = original.DataCadastro;
            copia.DataModificacao = original.DataModificacao;
            copia.EmpresaId = original.EmpresaId;
            // Sem credencial na cópia: quem autentica é o banco central.
            copia.PasswordHash = null;
            copia.SecurityStamp = Guid.NewGuid().ToString();
            copia.LockoutEnabled = true;

            if (nova)
                tenant.Usuarios.Add(copia);

            await tenant.SaveChangesAsync();
        }

        /// <summary>Tira a cópia antes de excluir o original. Falha se o usuário
        /// tem histórico no tenant — e aí o original também não deve sair.</summary>
        public async Task Remover(Guid usuarioId)
        {
            await using var central = _conexoes.Central();

            var empresaId = await central.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.EmpresaId).FirstOrDefaultAsync();
            if (empresaId is null)
                return;

            var banco = await central.Empresas.Where(e => e.Id == empresaId).Select(e => e.Banco).FirstOrDefaultAsync();
            if (_conexoes.EhCentral(banco))
                return;

            await using var tenant = _conexoes.Criar(banco);
            await tenant.Usuarios.Where(u => u.Id == usuarioId).ExecuteDeleteAsync();
        }
    }
}
