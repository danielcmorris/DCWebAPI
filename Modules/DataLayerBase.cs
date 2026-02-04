using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Diagnostics;

namespace DCElectricWebAPI.Modules
{
    public class DataLayerBase : IDisposable
    {
        // Use 'readonly' to ensure the string is never wiped after constructor
        public readonly string _connectionString;

        public bool IsProduction
        {
            get
            {
                return _connectionString.IndexOf("Production") > 0;
            }
        }


        public DataLayerBase(IConfiguration configuration)
        {
            // Standard .NET way to get the merged connection string
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        /// <summary>
        /// Returns a NEW, OPENED connection. 
        /// Relying on Connection Pooling is the standard for Web APIs.
        /// </summary>
        public DbConnection Connection
        {
            get
            {
                var conn = new SqlConnection(_connectionString);
                conn.Open();
                return conn;
            }
        }

        // Dapper methods now use a 'using' block or the Connection property directly.
        // Because Connection creates a NEW instance, we don't need a static locker anymore.

        public IEnumerable<T> Query<T>(string sql, object parameters = null, IDbTransaction transaction = null, bool buffered = true, int? timeout = null, CommandType? commandType = null) where T : class
        {
            using (var db = Connection)
            {
                return db.Query<T>(sql, parameters, transaction, buffered, timeout, commandType);
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null, IDbTransaction transaction = null, int? timeout = null, CommandType? commandType = null) where T : class
        {
            using (var db = Connection)
            {
                return await db.QueryAsync<T>(sql, parameters, transaction, timeout, commandType);
            }
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object parameters = null, IDbTransaction transaction = null, int? timeout = null, CommandType? commandType = null) where T : class
        {
            using (var db = Connection)
            {
                return await db.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction, timeout, commandType);
            }
        }

        public async Task<int> ExecuteAsync(string sql, object parameters = null, IDbTransaction transaction = null, int? timeout = null, CommandType? commandType = null)
        {
            using (var db = Connection)
            {
                return await db.ExecuteAsync(sql, parameters, transaction, timeout, commandType);
            }
        }

        public void LogIt(string LogType, string Header, string Message)
        {
            var parameters = new { LogType, Header, Message };
            var sql = "exec newLog @LogType, @Header, @Message";

            using (var db = Connection)
            {
                db.Execute(sql, parameters);
            }
        }

        public DataSet GetDataOrException(string sql, string strTableName = "")
        {
            sql = sql.Replace("\r\n", " ");
            var ds = new DataSet();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var dcmd = new SqlCommand(sql, connection))
                {
                    dcmd.CommandType = CommandType.Text;
                    var da = new SqlDataAdapter(dcmd);
                    da.Fill(ds);
                }
            }
            return ds;
        }
        public async Task<T> QuerySingleAsync<T>(string sql, object parameters = null, IDbTransaction transaction = null, int? timeout = null, CommandType? commandType = null)
        {
            using (var db = Connection)
            {
                return await db.QuerySingleAsync<T>(sql, parameters, transaction, timeout, commandType);
            }
        }

        // And for the non-async version just in case:
        public T QuerySingle<T>(string sql, object parameters = null, IDbTransaction transaction = null, int? timeout = null, CommandType? commandType = null)
        {
            using (var db = Connection)
            {
                return db.QuerySingle<T>(sql, parameters, transaction, timeout, commandType);
            }
        }


        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object parameters = null, IDbTransaction transaction = null, int? timeout = null, CommandType? commandType = null)
        {
            using (var db = Connection)
            {
                return await db.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction, timeout, commandType);
            }
        }
        // Cleanup: Since we no longer hold a private _connection, 
        // Dispose is just here for interface compatibility.
        public void Dispose() { }

        // ... Keep your CreateListFromTable and helper methods below ...
    }
}