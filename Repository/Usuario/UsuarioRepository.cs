using Erp.Data;
using Microsoft.EntityFrameworkCore;

namespace Erp.Repository.Usuario
{
    
    public class UsuarioRepository
    {
        private readonly AppDbContext _context;
        public UsuarioRepository(AppDbContext context)
        {
            this._context = context;
        }
    }
}
