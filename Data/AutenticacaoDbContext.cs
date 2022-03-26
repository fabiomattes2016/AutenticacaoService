using Microsoft.EntityFrameworkCore;

namespace AutenticacaoService.Data
{
    public class AutenticacaoDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public DbSet<User> Users { get; set; }

        public AutenticacaoDbContext(IConfiguration configuration) => _configuration = configuration;

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.EnableSensitiveDataLogging();
            options.UseNpgsql(_configuration.GetConnectionString("Postgres"));
        }
    }
}