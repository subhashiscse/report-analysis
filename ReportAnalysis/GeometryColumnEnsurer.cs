using Npgsql;


namespace ReportAnalysis
{
    public class GeometryColumnEnsurer
    {
        private static readonly string ConnectionString = "Host=10.5.6.34;Port=5432;Database=dgis;Username=postgres;Password=your_password";

        // ============================================
        // METHOD 1: Check and Create if not exists
        // ============================================
        public static async Task EnsureGeomColumnExistsAsync(string tableName)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                try
                {
                    // Step 1: Check if the_geom column exists
                    if (await ColumnExistsAsync(connection, tableName, "the_geom"))
                    {
                        Console.WriteLine($"✓ the_geom column already exists in {tableName}");
                        return;
                    }

                    Console.WriteLine($"✗ the_geom column NOT found. Creating...");

                    // Step 2: Enable PostGIS extension
                    await EnablePostGISAsync(connection);

                    // Step 3: Add the_geom column
                    await AddGeomColumnAsync(connection, tableName);

                    // Step 4: Update geometry values
                    await UpdateGeomValuesAsync(connection, tableName);

                    // Step 5: Create spatial index
                    await CreateSpatialIndexAsync(connection, tableName);

                    Console.WriteLine("✓ the_geom column created successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    throw;
                }
            }
        }

        // ============================================
        // METHOD 2: Check if column exists
        // ============================================
        private static async Task<bool> ColumnExistsAsync(NpgsqlConnection connection, string tableName, string columnName)
        {
            string query = @"SELECT column_name FROM information_schema.columns 
                        WHERE table_name = @tableName AND column_name = @columnName";

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@columnName", columnName);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
        }

        // ============================================
        // METHOD 3: Enable PostGIS extension
        // ============================================
        private static async Task EnablePostGISAsync(NpgsqlConnection connection)
        {
            string sql = "CREATE EXTENSION IF NOT EXISTS postgis";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✓ PostGIS extension enabled");
            }
        }

        // ============================================
        // METHOD 4: Add geometry column
        // ============================================
        private static async Task AddGeomColumnAsync(NpgsqlConnection connection, string tableName)
        {
            // Check if lng and lat columns exist first
            if (!await ColumnExistsAsync(connection, tableName, "lng") ||
                !await ColumnExistsAsync(connection, tableName, "lat"))
            {
                throw new Exception("Table must have 'lng' and 'lat' columns");
            }

            string sql = $@"ALTER TABLE {tableName} 
                       ADD COLUMN the_geom geometry(Point, 4326)";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Added the_geom column");
            }
        }

        // ============================================
        // METHOD 5: Update geometry values
        // ============================================
        private static async Task UpdateGeomValuesAsync(NpgsqlConnection connection, string tableName)
        {
            string sql = $@"UPDATE {tableName} 
                       SET the_geom = ST_POINT(lng, lat) 
                       WHERE the_geom IS NULL";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                int rowsUpdated = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"✓ Updated {rowsUpdated} geometry values");
            }
        }

        // ============================================
        // METHOD 6: Create spatial index
        // ============================================
        private static async Task CreateSpatialIndexAsync(NpgsqlConnection connection, string tableName)
        {
            string indexName = $"idx_{tableName}_geom";
            string sql = $@"CREATE INDEX IF NOT EXISTS {indexName} 
                       ON {tableName} USING GIST (the_geom)";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Spatial index created");
            }
        }

        // ============================================
        // METHOD 7: Create geometry table (Alternative)
        // ============================================
        public static async Task CreateGeomTableAsync(string sourceTable, string geomTable)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                try
                {
                    await EnablePostGISAsync(connection);

                    string sql = $@"CREATE TABLE IF NOT EXISTS {geomTable} AS 
                               SELECT t.*, ST_POINT(t.lng, t.lat) AS the_geom 
                               FROM {sourceTable} t";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"✓ Geometry table created: {geomTable}");
                    }

                    await CreateSpatialIndexAsync(connection, geomTable);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    throw;
                }
            }
        }

        // ============================================
        // METHOD 8: Query with Geometry
        // ============================================
        public static async Task<List<Dictionary<string, object>>> QueryWithGeometryAsync(
            string tableName, string bbox, string poiTypes)
        {
            var results = new List<Dictionary<string, object>>();

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                try
                {
                    // Ensure geometry column exists BEFORE querying
                    await EnsureGeomColumnExistsAsync(tableName);

                    // Parse bounding box
                    string[] bboxArray = bbox.Split(",");
                    if (bboxArray.Length != 4)
                    {
                        throw new ArgumentException("Bbox must have 4 values: minLng,minLat,maxLng,maxLat");
                    }

                    double minLng = double.Parse(bboxArray[0]);
                    double minLat = double.Parse(bboxArray[1]);
                    double maxLng = double.Parse(bboxArray[2]);
                    double maxLat = double.Parse(bboxArray[3]);

                    // Build query
                    var queryBuilder = new System.Text.StringBuilder();
                    queryBuilder.Append($@"SELECT *, ST_AsGeoJSON(the_geom) as geojson 
                                      FROM {tableName} 
                                      WHERE the_geom && ST_MakeEnvelope(@minLng, @minLat, @maxLng, @maxLat, 4326)");

                    // Add poi_type filter
                    if (!poiTypes.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] poiTypeList = poiTypes.Split(",");
                        string inClause = "'" + string.Join("','", poiTypeList) + "'";
                        queryBuilder.Append($" AND poi_type IN ({inClause})");
                    }

                    using (var command = new NpgsqlCommand(queryBuilder.ToString(), connection))
                    {
                        command.Parameters.AddWithValue("@minLng", minLng);
                        command.Parameters.AddWithValue("@minLat", minLat);
                        command.Parameters.AddWithValue("@maxLng", maxLng);
                        command.Parameters.AddWithValue("@maxLat", maxLat);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                results.Add(row);
                            }
                        }
                    }

                    Console.WriteLine($"✓ Query returned {results.Count} results");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Query Error: {ex.Message}");
                    throw;
                }
            }

            return results;
        }
    }

    // ============================================
    // USAGE EXAMPLE IN CONSOLE APPLICATION
    // ============================================
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Geometry Column Ensurer ===\n");

                // Option 1: Ensure column exists on existing table
                await GeometryColumnEnsurer.EnsureGeomColumnExistsAsync("poi_zurich_ch");

                // Option 2: Create separate geometry table
                // await GeometryColumnEnsurer.CreateGeomTableAsync("poi_zurich_ch", "poi_zurich_ch_geo");

                Console.WriteLine("\n=== Running Spatial Query ===\n");

                // Query with bounding box and poi types
                string bbox = "8.5,47.3,8.6,47.4";  // minLng, minLat, maxLng, maxLat
                string poiTypes = "all";  // or "restaurant,cafe,hotel"

                var results = await GeometryColumnEnsurer.QueryWithGeometryAsync("poi_zurich_ch", bbox, poiTypes);

                Console.WriteLine($"\nFound {results.Count} POIs:");
                foreach (var result in results)
                {
                    Console.WriteLine($"  GeoJSON: {result["geojson"]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    // ============================================
    // USAGE IN ASP.NET CORE SERVICE
    // ============================================
    /*
    public class POIService
    {
        public POIService()
        {
            // Ensure geometry column exists when service initializes
            GeometryColumnEnsurer.EnsureGeomColumnExistsAsync("poi_zurich_ch").Wait();
        }

        public async Task<List<Dictionary<string, object>>> GetPOIsByBboxAsync(string bbox, string poiTypes)
        {
            // Now safe to query - the_geom definitely exists
            return await GeometryColumnEnsurer.QueryWithGeometryAsync("poi_zurich_ch", bbox, poiTypes);
        }
    }

    // In Startup.cs or Program.cs:
    services.AddScoped<POIService>();
    */
}
