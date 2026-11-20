using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Classes
{
    public class DbManager : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        private const string ConnectionString = "Server=localhost;Port=3306;Database=license_db;User=root;Password=1234";
        public DbManager() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(
                    ConnectionString,
                    new MySqlServerVersion(new Version(8, 0, 36))
                );
            }
        }
    }
}
