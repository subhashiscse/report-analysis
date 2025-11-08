using Microsoft.EntityFrameworkCore;
using ReportAnalysis;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<PoiZurich> PoiZurichCh { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PoiZurich>().ToTable("poi_zurich_ch");
    }
}