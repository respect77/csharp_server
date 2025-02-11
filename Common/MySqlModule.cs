using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;

namespace Common
{
    public static partial class ExtendsMethod
    {
        public static async Task<int> ExecuteAsync(this MySqlTransaction transaction, string query)
        {
            return await transaction.Connection.ExecuteAsync(query, transaction: transaction);
        }
    }

    public class MySqlModule
    {
        private readonly string _connectionString;
        public MySqlModule(string connectString)
        {
            _connectionString = connectString;
        }

        public async Task<T?> QuerySingleAsync<T>(string query)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await connection.QueryFirstAsync<T>(query).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error(ex.Message);
                return default;
            }
        }

        public async Task<IEnumerable<T>?> QueryMultiAsync<T>(string query)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await connection.QueryAsync<T>(query).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error(ex.Message);
                return null;
            }
        }
        /*
        public async Task QueryMultiAsync2<T>(string query)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                await foreach (var row in connection.QueryUnbufferedAsync<T>("select 'abc' as [Value] union all select @txt", new { txt = "def" })
                .ConfigureAwait(false))
                {
                    //T value = row;
                    //results.Add(value);
                }
                return ;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error(ex.Message);
                return;
            }
        }
        */

        public async Task<bool> ExecAsync(string query)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                int rows = await connection.ExecuteAsync(query).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error(ex.Message);
                return false;
            }
        }

        public async Task<bool> TransactionAsync(List<string> queries)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
                try
                {
                    foreach (var query in queries)
                    {
                        await transaction.ExecuteAsync(query).ConfigureAwait(false);
                    }
                    await transaction.CommitAsync().ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    LogManager.Instance.Error(ex.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.Error(ex.Message);
                return false;
            }
        }
    }
}
