using Microsoft.EntityFrameworkCore;
public class ApplicationDbContext : DbContext
{
    public DbSet<PoiZurich> PoiZurich { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            "Host=10.5.6.34;Port=5432;Database=dgis;Username=postgres;Password=bg@123#$%"
        );
    }
}