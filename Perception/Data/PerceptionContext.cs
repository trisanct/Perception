using Microsoft.EntityFrameworkCore;

namespace Perception.Data
{
    public class PerceptionContext : DbContext
    {
        public PerceptionContext(DbContextOptions<PerceptionContext> options) : base(options)
        {
            //Console.WriteLine("233");
        }
        public DbSet<Record> Records { get; set; }
        public DbSet<FileMap> Files { get; set; }
        public DbSet<FileNode> Nodes { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Dataset> Datasets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Record>()
                .Property(r => r.Time)
                .HasDefaultValueSql("getdate()");
            modelBuilder.Entity<FileMap>()
                .HasMany(f => f.Results)
                .WithOne()
                .HasForeignKey(r => r.FileId)
                .OnDelete(DeleteBehavior.Cascade);
            base.OnModelCreating(modelBuilder);
        }
    }
}