using BPMWebApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BPMWebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Agrega DbSets para tus modelos si es necesario
        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<Operario> Operarios { get; set; }
    }
}