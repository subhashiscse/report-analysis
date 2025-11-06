// using Microsoft.EntityFrameworkCore;
// using NetTopologySuite.Geometries;
//
// public class ZurichDbContext : DbContext
// {
//     public DbSet<PoiZurich> PoiZurichCh { get; set; }
//
//     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//     {
//         optionsBuilder.UseNpgsql(
//             "Host=10.5.6.34;Port=5432;Database=dgis;Username=postgres;Password=bg@123#$%",
//             o => o.UseNetTopologySuite()
//         );
//     }
// }