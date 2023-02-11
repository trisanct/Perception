using Microsoft.EntityFrameworkCore;
using Perception.Models;

namespace Perception
{
    public class PerceptionContext : DbContext
    {
        public PerceptionContext(DbContextOptions<PerceptionContext> options) : base(options)
        {
        }
        public DbSet<Record> Records { get; set; }
        public DbSet<FileMap> Files { get; set; }
        public DbSet<FileNode> Nodes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Record>()
                .Property(r=>r.Time)
                .HasDefaultValueSql("getdate()");
            base.OnModelCreating(modelBuilder);
        }
    }
}