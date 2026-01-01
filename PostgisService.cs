using Npgsql;
using NetTopologySuite.Geometries;
using Npgsql.NetTopologySuite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenMap
{
    public class PostgisService
    {
        private readonly string _connectionString;

        public PostgisService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Geometry>> GetGeometriesAsync(string sql)
        {
            var result = new List<Geometry>();

            // Create a data source and configure type mapping
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
            dataSourceBuilder.UseNetTopologySuite();
            await using var dataSource = dataSourceBuilder.Build();

            await using var conn = await dataSource.OpenConnectionAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var geom = reader.GetValue(0) as Geometry;
                if (geom != null)
                {
                    result.Add(geom);
                }
            }
            return result;
        }

        /// <summary>
        /// Saves a geometry to the specified table in PostGIS
        /// </summary>
        /// <param name="geometry">The geometry to save</param>
        /// <param name="tableName">The target table name</param>
        /// <param name="geometryColumn">The geometry column name (default: 'geom')</param>
        /// <param name="srid">The SRID of the geometry (default: 4326 for WGS84)</param>
        /// <returns>True if the operation was successful</returns>
        public async Task<bool> SaveGeometryAsync(Geometry geometry, string tableName, string geometryColumn = "geom", int srid = 4326)
        {
            if (geometry == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveGeometryAsync: geometry is null");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"SaveGeometryAsync: Starting save to table '{tableName}', column '{geometryColumn}'");
                System.Diagnostics.Debug.WriteLine($"SaveGeometryAsync: Geometry type: {geometry.GeometryType}, SRID: {geometry.SRID}");
                
                // Create a data source and configure type mapping
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
                dataSourceBuilder.UseNetTopologySuite();
                await using var dataSource = dataSourceBuilder.Build();

                // Ensure the geometry has the correct SRID
                if (geometry.SRID == 0)
                {
                    geometry.SRID = srid;
                    System.Diagnostics.Debug.WriteLine($"SaveGeometryAsync: Set SRID to {srid}");
                }

                // Create the INSERT command
                var sql = $"INSERT INTO \"{tableName}\" (\"{geometryColumn}\") VALUES (@geom) RETURNING id";
                System.Diagnostics.Debug.WriteLine($"SaveGeometryAsync: Executing SQL: {sql}");
                
                await using var conn = await dataSource.OpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                
                // Add the geometry parameter
                cmd.Parameters.AddWithValue("geom", geometry);
                
                // Execute the command
                var result = await cmd.ExecuteScalarAsync();
                var success = result != null;
                
                System.Diagnostics.Debug.WriteLine($"SaveGeometryAsync: ExecuteScalar result: {result}");
                System.Diagnostics.Debug.WriteLine($"SaveGeometryAsync: Save operation {(success ? "succeeded" : "failed")}");
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving geometry to PostGIS: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error details: {ex}");
                return false;
            }
        }
    }
}