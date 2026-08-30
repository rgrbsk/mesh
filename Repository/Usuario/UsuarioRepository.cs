using Erp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AppUsuario = Erp.Model.Usuario.Usuario;

namespace Erp.Repository.Usuario
{
    /// <summary>
    /// Leitura sai da fábrica de contexto (ver PessoaRepository para o porquê);
    /// escrita vai pelo UserManager, que é quem cuida de senha, normalização de
    /// e-mail e security stamp.
    /// </summary>
    public class UsuarioRepository
    {
        private readonly IDbContextFactory<AppDbContext> _fabrica;
        private readonly UserManager<AppUsuario> _identity;

        public UsuarioRepository(IDbContextFactory<AppDbContext> fabrica, UserManager<AppUsuario> identity)
        {
            _fabrica = fabrica;
            _identity = identity;
        }

        /// <summary>Usuários para seletores (responsável pelo centro de custo,
        /// aprovador, solicitante).</summary>
        public Task<List<AppUsuario>> Buscar() => Buscar(null);

        /// <summary>Lista aplicando o filtro montado no BbFilterBuilder — a
        /// expressão vai para o Where do EF, não para memória.</summary>
        public async Task<List<AppUsuario>> Buscar(Expression<Func<AppUsuario, bool>>? filtro)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            var query = contexto.Usuarios.AsNoTracking().AsQueryable();

            if (filtro is not null)
                query = query.Where(filtro);

            return await query
                .OrderBy(u => u.Nome)
                .ThenBy(u => u.Sobrenome)
                .ToListAsync();
        }

        public async Task<AppUsuario?> ObterPorId(Guid id)
        {
            await using var contexto = await _fabrica.CreateDbContextAsync();

            return await contexto.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Cria com senha inicial. O e-mail vira o login: o Identity exige
        /// UserName, e um login diferente do e-mail só daria duas coisas para o
        /// usuário esquecer.
        /// </summary>
        public async Task<IdentityResult> Criar(AppUsuario usuario, string senha)
        {
            usuario.UserName = usuario.Email;
            usuario.EmailConfirmed = true;
            usuario.DataCadastro = DateTime.UtcNow;
            usuario.DataModificacao = usuario.DataCadastro;

            return await _identity.CreateAsync(usuario, senha);
        }

        /// <summary>
        /// Copia os campos editáveis por cima do usuário rastreado pelo Identity.
        /// Salvar a instância vinda da grade (AsNoTracking) sobrescreveria hash de
        /// senha e security stamp com o que estava na tela.
        /// </summary>
        public async Task<IdentityResult> Atualizar(AppUsuario editado)
        {
            var usuario = await _identity.FindByIdAsync(editado.Id.ToString());
            if (usuario is null)
                return IdentityResult.Failed(new IdentityError { Description = "Usuário não encontrado." });

            usuario.Nome = editado.Nome;
            usuario.Sobrenome = editado.Sobrenome;
            usuario.CPF = editado.CPF;
            usuario.Cargo = editado.Cargo;
            usuario.NumeroContato = editado.NumeroContato;
            usuario.Observacao = editado.Observacao;
            usuario.Status = editado.Status;
            usuario.DataModificacao = DateTime.UtcNow;

            if (!string.Equals(usuario.Email, editado.Email, StringComparison.OrdinalIgnoreCase))
            {
                await _identity.SetEmailAsync(usuario, editado.Email);
                await _identity.SetUserNameAsync(usuario, editado.Email);
            }

            return await _identity.UpdateAsync(usuario);
        }

        /// <summary>Troca a senha sem pedir a atual — é a redefinição feita por
        /// quem administra, não a troca feita pelo dono da conta.</summary>
        public async Task<IdentityResult> RedefinirSenha(Guid id, string novaSenha)
        {
            var usuario = await _identity.FindByIdAsync(id.ToString());
            if (usuario is null)
                return IdentityResult.Failed(new IdentityError { Description = "Usuário não encontrado." });

            var token = await _identity.GeneratePasswordResetTokenAsync(usuario);

            return await _identity.ResetPasswordAsync(usuario, token, novaSenha);
        }

        public async Task<IdentityResult> Excluir(Guid id)
        {
            var usuario = await _identity.FindByIdAsync(id.ToString());
            if (usuario is null)
                return IdentityResult.Failed(new IdentityError { Description = "Usuário não encontrado." });

            return await _identity.DeleteAsync(usuario);
        }
    }
}
