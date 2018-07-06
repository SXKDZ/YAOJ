using Microsoft.EntityFrameworkCore;
using YAOJ.Models;

namespace YAOJ.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Record> Records { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Problem>().ToTable("Problem");
            modelBuilder.Entity<Record>().ToTable("Record");
        }
    }
}
