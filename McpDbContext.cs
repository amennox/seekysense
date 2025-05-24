using Microsoft.EntityFrameworkCore;
using McpServer.Models;

namespace McpServer.Data
{
    public class McpDbContext : DbContext
    {
        public McpDbContext(DbContextOptions<McpDbContext> options) : base(options) { }

        public DbSet<Scope> Scopes { get; set; }
        public DbSet<BusinessAuth> BusinessAuths { get; set; }
        public DbSet<UserAuth> UserAuths { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Scope>().HasKey(s => s.ScopeId);
            modelBuilder.Entity<BusinessAuth>().HasKey(ba => ba.Id);
            modelBuilder.Entity<UserAuth>().HasKey(ua => ua.Id);
        }
    }
}