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
                var geom = reader.GetFieldValue<Geometry>(0);
                result.Add(geom);
            }
            return result;
        }
    }
}