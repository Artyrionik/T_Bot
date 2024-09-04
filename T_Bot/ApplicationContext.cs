using Microsoft.EntityFrameworkCore;

namespace T_Bot
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Pattern> Patterns => Set<Pattern>();
        public ApplicationContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Test.db");
        }
    }
}
