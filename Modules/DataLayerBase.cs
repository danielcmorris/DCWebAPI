using Dapper;
using DCElectricWebAPI.Models;
using Microsoft.Extensions.Options;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;


namespace DCElectricWebAPI.Modules
{

    public class DataLayerBase : IDisposable

    {

       



        protected string _connectionString;
        public string ConnectionString => _connectionString;
        DbConnection _connection = null;
        static object objLocker = new object();

        public DataLayerBase()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));

            var root = builder.Build();
            _connectionString = root.GetConnectionString("DefaultConnection");

             
 
        }




        public DbConnection Connection
        {
            get
            {
                lock (objLocker)
                {
                    if (_connection == null)
                    {
                        _connection = new SqlConnection(this.ConnectionString);
                        _connection.Open();
                    }

                    return _connection;
                }
            }
        }

        public void Dispose()
        {
            lock (_connection)
            {
                _connection?.Close();
                _connection?.Dispose();
            }
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null, IDbTransaction transaction = null, Boolean? buffered = true, int? timeout = null, CommandType? commandType = null) where T : class
        {
            return this.Connection.Query<T>(sql, parameters, transaction, buffered.Value, timeout, commandType);
        }
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null, IDbTransaction transaction = null, Boolean? buffered = true, int? timeout = null, CommandType? commandType = null) where T : class
        {
            return await Connection.QueryAsync<T>(sql, parameters, transaction, timeout, commandType);
        }
        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object parameters = null, IDbTransaction transaction = null, Boolean? buffered = true, int? timeout = null, CommandType? commandType = null) where T : class
        {
            return await Connection.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction, timeout, commandType);
        }

        public dynamic Query(string sql, object parameters = null, IDbTransaction transaction = null, Boolean? buffered = true, int? timeout = null, CommandType? commandType = null)
        {
            return this.Connection.Query(sql, parameters, transaction, buffered.Value, timeout, commandType);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object parameters = null, IDbTransaction transaction = null, Boolean? buffered = true, int? timeout = null, CommandType? commandType = null)
        {
            return await Connection.QuerySingleAsync<T>(sql, parameters, transaction, timeout, commandType);
        }

        public async Task<int> ExecuteAsync(string sql, object parameters = null, IDbTransaction transaction = null, Boolean? buffered = true, int? timeout = null, CommandType? commandType = null)
        {
            return await Connection.ExecuteAsync(sql, parameters, transaction, timeout, commandType);
        }

        public void LogIt(string LogType, string Header, string Message)
        {
            var parameters = new
            {
                LogType = LogType,
                Header = Header,
                Message = Message
            };

            var sql = "exec newLog @LogType, @Header, @Message";
            using (var connection = new SqlConnection(this.ConnectionString))
            {
                connection.Execute(sql, parameters);
            }

        }

        public DataSet GetData(string sql, string strTableName = "")
        {
            try
            {
                return GetDataOrException(sql, strTableName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
            return new DataSet();
        }

        public DataSet GetDataOrException(string sql, string strTableName = "")
        {
            sql = sql.Replace("\r\n", "");
            SqlDataAdapter da = new SqlDataAdapter();
            DataSet ds = new DataSet();
            SqlCommand dcmd = new SqlCommand();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                dcmd.Connection = connection;
                dcmd.CommandText = sql;
                dcmd.CommandType = CommandType.Text;
                da.SelectCommand = dcmd;
                da.Fill(ds);
            }

            return ds;
        }

        public string RunSQL_String(string sql)
        {
            string retval = "";
            using (var connection = new SqlConnection(this.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                try
                {
                    cmd.Connection.Open();
                    retval = (string)cmd.ExecuteScalar();
                    cmd.Connection.Close();
                }
                catch
                {
                    retval = "";
                }
            }



            return retval;


        }

        internal object GetRootFolders()
        {
            throw new NotImplementedException();
        }

        // function that creates a list of an object from the given data table
        public List<T> GetData<T>(string sql, bool allowException = false) where T : new()
        {
            List<T> lst = new List<T>();
            DataSet ds = allowException ? GetDataOrException(sql) : GetData(sql);
            if (ds.Tables.Count > 0)
            {
                lst = CreateListFromTable<T>(ds.Tables[0]);
            }
            return lst;

        }
        // function that creates a list of an object from the given data table
        public static List<T> CreateListFromTable<T>(DataTable tbl) where T : new()
        {
            // define return list
            List<T> lst = new List<T>();

            // go through each row
            foreach (DataRow r in tbl.Rows)
            {
                // add to the list
                lst.Add(CreateItemFromRow<T>(r));
            }

            // return the list
            return lst;
        }

        // function that creates an object from the given data row
        public static T CreateItemFromRow<T>(DataRow row) where T : new()
        {
            // create a new object
            T item = new T();

            // set the item
            SetItemFromRow(item, row);

            // return 
            return item;
        }

        public static void SetItemFromRow<T>(T item, DataRow row) where T : new()
        {
            // go through each column
            foreach (DataColumn c in row.Table.Columns)
            {
                // find the property for the column
                PropertyInfo p = item.GetType().GetProperty(c.ColumnName,
                    BindingFlags.SetProperty |
                    BindingFlags.IgnoreCase |
                    BindingFlags.Public |
                    BindingFlags.Instance);

                // if exists, set the value
                if (p != null && row[c] != DBNull.Value)
                {

                    try
                    {
                        p.SetValue(item, row[c], null);

                    }
                    catch (Exception e)
                    {
                        Debugger.Break();
                    }

                }
            }
        }

        public static dynamic ConvertTableToExpando(DataTable dt)
        {
            List<dynamic> expandoList = new List<dynamic>();


            foreach (DataRow row in dt.Rows)
            {
                //create a new ExpandoObject() at each row
                var expandoDict = new System.Dynamic.ExpandoObject() as IDictionary<String, Object>;
                foreach (DataColumn col in dt.Columns)
                {
                    //put every column of this row into the new dictionary
                    //formatting string
                    var myColName = col.ToString();
                    myColName = myColName.Substring(0, 1).ToLower() + myColName.Substring(1, myColName.Length - 1);
                    if (col.DataType == typeof(System.Single)
                           || col.DataType == typeof(System.Double)
                           || col.DataType == typeof(System.Decimal)
                           || col.DataType == typeof(System.Byte)
                           || col.DataType == typeof(System.Int16)
                           || col.DataType == typeof(System.Int32)
                           || col.DataType == typeof(System.Int64))
                    {
                        // this column is numeric
                        int myInt;

                        bool success = int.TryParse(row[col.ColumnName].ToString(), out myInt);
                        if (success)
                        {
                            expandoDict.Add(myColName, myInt);
                        }
                        else
                        {
                            double myDbl;
                            bool success2 = double.TryParse(row[col.ColumnName].ToString(), out myDbl);
                            if (success2)
                            {
                                expandoDict.Add(myColName, myDbl);
                            }
                            else
                            {
                                expandoDict.Add(myColName, row[col.ColumnName].ToString());
                            }

                        }
                    }
                    else
                    {
                        // this column is not numeric
                        expandoDict.Add(myColName, row[col.ColumnName].ToString());
                    }

                    //expandoDict.Add(col.ToString(), row[col.ColumnName].ToString());
                }

                //add this "row" to the list
                expandoList.Add(expandoDict);


            }

            return expandoList;
        }

        public Int32 RunSQLOrException(string sql, bool NoResponseRequired = false)
        {
            int retval = 0;
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = new SqlConnection(ConnectionString);
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            cmd.Connection.Open();
            if (NoResponseRequired)
            {
                cmd.ExecuteNonQuery();
            }
            else
            {
                retval = Convert.ToInt32(cmd.ExecuteScalar());
            }

            cmd.Connection.Close();

            return retval;
        }

        public Int32 RunSQL(string sql)
        {
            int retval = 0;
            try
            {
                retval = RunSQLOrException(sql);
            }
            catch (Exception e)
            {
                //TODO: Log error
                retval = -1;
            }
            return retval;
        }

        public Int32 RunSQL(string sql, object parameters = null, IDbTransaction transaction = null, int? timeout = null, CommandType? commandType = null)
        {
            return this.Connection.Execute(sql, parameters, transaction, timeout, commandType);
        }

        protected DataTable ConvertToDataTable<TSource>(IEnumerable<TSource> source)
        {
            var props = typeof(TSource).GetProperties();

            var dt = new DataTable();
            dt.Columns.AddRange(
              props.Select(p => new DataColumn(p.Name, p.PropertyType)).ToArray()
            );

            source.ToList().ForEach(
              i => dt.Rows.Add(props.Select(p => p.GetValue(i, null)).ToArray())
            );

            return dt;
        }


    }

}
