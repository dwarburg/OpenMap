using Npgsql;
using NetTopologySuite.Geometries;
using NpgsqlTypes;
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
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

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