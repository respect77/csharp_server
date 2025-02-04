using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;

namespace Common
{
    public class DBAgent
    {
        private readonly string ConnectionString;
        public DBAgent()
        {
            ConnectionString = "Server=localhost;Database=mysql;User=root;Password=1234;Pooling=true;Min Pool Size=5;Max Pool Size=100;";
        }

        public async Task<IEnumerable<T>?> Query<T>(string query)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                var result = await connection.QueryAsync<T>(query);
                //Console.WriteLine($"User Count: {result}");
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error(ex.Message);
                return null;
            }
            
        }
    }
}
