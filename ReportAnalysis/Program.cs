// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Npgsql;
//
// namespace ZurichPOIApp
// {
//     class Program
//     {
//         static async Task Main(string[] args)
//         {
//             string connectionString = "Host=10.5.6.34;Port=5432;Database=dgis;Username=postgres;Password=bg@123#$%";
//
//             string bbox = "8.5,47.3,8.6,47.4";   // minX, minY, maxX, maxY
//             string poiTypes = "restaurant,cafe"; // or "all"
//             string displayPropertyName = "poi_name,poi_type";
//
//             if (string.IsNullOrEmpty(bbox))
//             {
//                 Console.WriteLine("Invalid input: bbox is required.");
//                 return;
//             }
//
//             string[] bboxParts = bbox.Split(',');
//             string sqlQuery = $@"
//                 SELECT *, ST_AsGeoJSON(the_geom) AS geojson
//                 FROM poi_zurich_ch
//                 WHERE the_geom && ST_MakeEnvelope({bboxParts[0]}, {bboxParts[1]}, {bboxParts[2]}, {bboxParts[3]}, 4326)";
//
//             if (!poiTypes.Equals("all", StringComparison.OrdinalIgnoreCase))
//             {
//                 var poiTypeList = poiTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
//                 string poiTypeStr = string.Join(",", Array.ConvertAll(poiTypeList, t => $"'{t.Trim()}'"));
//                 sqlQuery += $" AND poi_type IN ({poiTypeStr});";
//             }
//             else
//             {
//                 sqlQuery += ";";
//             }
//
//             Console.WriteLine("SQL Query:\n" + sqlQuery);
//             Console.WriteLine("\n🔗 Checking database connection...");
//
//             try
//             {
//                 await using var conn = new NpgsqlConnection(connectionString);
//                 await conn.OpenAsync();
//                 Console.WriteLine("Connection successful!");
//
//                 var results = await GetQueryGeoJSONData(conn, sqlQuery);
//
//                 Console.WriteLine("\n--- Query Results ---");
//                 if (results.Count == 0)
//                 {
//                     Console.WriteLine("No POIs found in the given area.");
//                 }
//
//                 foreach (var row in results)
//                 {
//                     string name = row.ContainsKey("poi_name") ? row["poi_name"]?.ToString() ?? "N/A" : "N/A";
//                     string type = row.ContainsKey("poi_type") ? row["poi_type"]?.ToString() ?? "N/A" : "N/A";
//                     string geojson = row.ContainsKey("geojson") ? row["geojson"]?.ToString() ?? "{}" : "{}";
//
//                     Console.WriteLine($"Name: {name}, Type: {type}");
//                     Console.WriteLine($"GeoJSON: {geojson}");
//                     Console.WriteLine("-------------------------");
//                 }
//
//                 Console.WriteLine("Done fetching data.");
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine("Connection or query failed!");
//                 Console.WriteLine("Error: " + ex.Message);
//             }
//         }
//
//         private static async Task<List<Dictionary<string, object>>> GetQueryGeoJSONData(NpgsqlConnection conn, string query)
//         {
//             var results = new List<Dictionary<string, object>>();
//
//             await using var cmd = new NpgsqlCommand(query, conn);
//             await using var reader = await cmd.ExecuteReaderAsync();
//
//             while (await reader.ReadAsync())
//             {
//                 var row = new Dictionary<string, object>();
//
//                 for (int i = 0; i < reader.FieldCount; i++)
//                 {
//                     row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
//                 }
//
//                 results.Add(row);
//             }
//
//             return results;
//         }
//     }
// }

using Microsoft.EntityFrameworkCore;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        /*string connectionString =
            "Host=10.5.6.34;Port=5432;Database=dgis;Username=postgres;Password=bg@123#$%";
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        string query = @"
            SELECT id, wkt_building
            FROM poi_zurich_ch
            LIMIT 20
        ";

        await using var cmd = new NpgsqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Console.WriteLine(
                $"ID: {reader[0]}, "
            );
            Console.WriteLine(reader[1]); 
        }*/
        using var db = new ApplicationDbContext();

        var data = await db.PoiZurich
            .Take(20)
            .ToListAsync();

        foreach (var item in data)
        {
            Console.WriteLine($"ID: {item.Id}");
            Console.WriteLine($"WKT: {item.Wkt_Building}");
            Console.WriteLine("--------------------------");
        }
    }
}