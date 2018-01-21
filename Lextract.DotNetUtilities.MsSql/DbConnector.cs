using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace Lextract.DotNetUtilities.MsSql
{
    /// <summary>
    /// Base class for connect to MsSql server and execute T-SQL statements and 
    /// return raw data and/or mapped objects.
    /// </summary>
    public abstract class DbConnector : IDisposable
    {
        public SqlConnection Conexion { get; private set; }
        public SqlCommand Comando { get; private set; }

        /// <summary>
        /// Creates a new instance based on SqlConnection object
        /// </summary>
        /// <param name="connection">Instance of SqlConnection</param>
        public DbConnector(SqlConnection connection)
        {
            Conexion = connection;
        }

        /// <summary>
        /// Creates a new instance based on a string, 
        /// valid strings can queried in https://www.connectionstrings.com/sql-server/
        /// </summary>
        /// <param name="connection">Connection string</param>
        public DbConnector(string connectionString)
        {
            Conexion = new SqlConnection(connectionString);
        }

        /// <summary>
        /// Sets procedure name to be executed on connection established
        /// </summary>
        /// <param name="procedureName">Procedure name (with schema name are always preferable)</param>
        public void SetProcedure(string procedureName)
        {
            // TODO: Implementar validacion que el nombre no tenga espacios en blanco, expresion regular ANSI tsql
            Comando = new SqlCommand(procedureName, Conexion);
            Comando.CommandType = CommandType.StoredProcedure;
        }

        /// <summary>
        /// Sets script text to be executed on connection established
        /// </summary>
        /// <param name="commandText">T-SQL script statements</param>
        public void SetCommand(string commandText)
        {
            Comando = new SqlCommand(commandText, Conexion);
            Comando.CommandType = CommandType.Text;
        }

        /// <summary>
        /// Creates a parameter inside procedure assigned
        /// </summary>
        /// <param name="parameter">Instance of SqlParameter</param>
        public void AddParameter(SqlParameter parameter)
        {
            Comando.Parameters.Add(parameter);
        }

        /// <summary>
        /// Creates or gets a parameter inside procedure established and assigns the value
        /// </summary>
        /// <param name="parameter">Parameter name</param>
        /// <param name="value">Value of parameter</param>
        /// <param name="dbType">Sql database type</param>
        public void AddParameterValue(string parameter, object value, SqlDbType dbType = SqlDbType.Variant)
        {            
            SqlParameter param;
            if (Comando.Parameters.Contains(parameter))
                param = Comando.Parameters[parameter];
            else param = new SqlParameter(parameter, dbType);
            param.Value = value;
        }

        /// <summary>
        /// Executes a Transact-SQL statement against the connection and returns the number 
        /// of rows affected.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public int ExecuteNonQuery()
        {
            int retorno = 0;
            OpenConnection();
            retorno = Comando.ExecuteNonQuery();
            return retorno;
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result 
        /// set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set, or a null reference (Nothing 
        /// in Visual Basic) if the result set is empty. Returns a maximum of 2033 characters.</returns>
        public object ExecuteScalar()
        {
            OpenConnection();
            object retorno = Comando.ExecuteScalar();
            return retorno;
        }

        /// <summary>
        /// Executes procedure or script previously assigned and loads raw data on a table
        /// </summary>
        /// <returns>A set of results</returns>
        public DataTable ExecuteReaderRaw()
        {
            OpenConnection();
            DataTable tabla = new DataTable();
            using (SqlDataReader reader = Comando.ExecuteReader())
                tabla.Load(reader);
            CloseConnection();
            return tabla;
        }

        /// <summary>
        /// Executes reader data set
        /// </summary>
        /// <returns>Data set</returns>
        public DataSet ExecuteReaderDataSet()
        {
            OpenConnection();
            DataSet dataSet = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(Comando);
            da.Fill(dataSet);
            CloseConnection();
            return dataSet;
        }

        private T MapearPropiedades<T>(SqlDataReader registro, Dictionary<string, int> columnasIndice)
        {
            T retorno = Activator.CreateInstance<T>();
            Type typeT = typeof(T);

            foreach (PropertyInfo propiedad in typeT.GetProperties())
            {
                if (columnasIndice.ContainsKey(propiedad.Name))
                {
                    object dato = registro[columnasIndice[propiedad.Name]];
                    if (dato.GetType() != typeof(System.DBNull))
                        typeT.GetProperty(propiedad.Name).SetValue(retorno, dato, null);
                }
            }
            return retorno;
        }

        private T MapearPropiedades2<T>(SqlDataReader registro, string[] propiedades, int[] indices)
        {
            T retorno = Activator.CreateInstance<T>();
            Type typeT = typeof(T);
            for (int i = 0; i < propiedades.Length; i++)
            {
                typeT.GetProperty(propiedades[i]).SetValue(retorno, registro[i], null);
            }
            return retorno;
        }

        /// <summary>
        /// Executes procedure or script previously assigned and maps each record to a object of type T
        /// </summary>
        /// <typeparam name="T">Type on which wants mapped</typeparam>
        /// <returns>List of objects of type T</returns>
        public List<T> ExecuteReaderMapping<T>()
        {
            List<T> retorno = new List<T>();
            OpenConnection();
            using (SqlDataReader reader = Comando.ExecuteReader())
            {
                Dictionary<string, int> columnasIndice = new Dictionary<string, int>();
                int indice = 0;
                foreach (DataRow columnaInfo in reader.GetSchemaTable().Rows)
                {
                    string columnName = System.Convert.ToString(columnaInfo["ColumnName"]);
                    columnasIndice.Add(columnName, indice);
                    //DataColumn column = new DataColumn(columnName, (Type)(drow["DataType"]));
                    //column.Unique = (bool)drow["IsUnique"];
                    //column.AllowDBNull = (bool)drow["AllowDBNull"];
                    //column.AutoIncrement = (bool)drow["IsAutoIncrement"];
                    //listCols.Add(column);
                    //dt.Columns.Add(column);
                    indice++;
                }

                while (reader.Read())
                {
                    // reader[i] valor de la propiedad
                    T registro = MapearPropiedades<T>(reader, columnasIndice);
                    retorno.Add(registro);
                }
            }

            CloseConnection();
            return retorno;
        }

        /// <summary>
        /// Executes procedure or script previously assigned and maps first column to a type T
        /// </summary>
        /// <typeparam name="T">Type on which wants mapped</typeparam>
        /// <returns>List of objects of type T</returns>
        public List<T> ExecuteReaderToType<T>()
        {
            List<T> retorno = new List<T>();
            OpenConnection();
            using (SqlDataReader reader = Comando.ExecuteReader())
            {
                Type typeT = typeof(T);
                while (reader.Read())
                {
                    T valor = (T)Convert.ChangeType(reader[0].ToString(), typeT);
                    retorno.Add(valor);
                }
            }
            CloseConnection();
            return retorno;
        }

        /// <summary>
        /// Executes procedure or script previously assigned and maps first record to a object of type T
        /// </summary>
        /// <typeparam name="T">Type on which wants mapped</typeparam>
        /// <returns>Object of type T</returns>
        public T ExecuteReaderMappingFirst<T>()
        {
            OpenConnection();
            using (SqlDataReader reader = Comando.ExecuteReader())
            {
                Dictionary<string, int> columnasIndice = new Dictionary<string, int>();
                int indice = 0;

                foreach (DataRow columnaInfo in reader.GetSchemaTable().Rows)
                {
                    string columnName = System.Convert.ToString(columnaInfo["ColumnName"]);
                    columnasIndice.Add(columnName, indice);
                    //DataColumn column = new DataColumn(columnName, (Type)(drow["DataType"]));
                    //column.Unique = (bool)drow["IsUnique"];
                    //column.AllowDBNull = (bool)drow["AllowDBNull"];
                    //column.AutoIncrement = (bool)drow["IsAutoIncrement"];
                    //listCols.Add(column);
                    //dt.Columns.Add(column);
                    indice++;
                }

                T retorno = Activator.CreateInstance<T>();
                bool atLeastOne = false;
                while (reader.Read())
                {
                    atLeastOne = true;
                    retorno = MapearPropiedades<T>(reader, columnasIndice);
                    reader.Close();
                    break;
                }
                if (!atLeastOne)
                    retorno = default(T);
                CloseConnection();
                return retorno;
            }

        }

        /// <summary>
        /// Executes a bulk copy on a target table
        /// </summary>
        /// <param name="sourceTable">Source data</param>
        /// <param name="targetTable">Target table</param>
        public void BulkCopy(DataTable sourceTable, string targetTable)
        {
            OpenConnection();
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Conexion))
            {
                bulkCopy.DestinationTableName = targetTable;
                bulkCopy.WriteToServer(sourceTable);
            }
            CloseConnection();
        }

        /// <summary>
        /// Opens connection
        /// </summary>
        protected void OpenConnection()
        {
            if (Conexion.State != ConnectionState.Open)
                Conexion.Open();
        }

        /// <summary>
        /// Closes connection
        /// </summary>
        protected void CloseConnection()
        {
            if (Conexion.State != ConnectionState.Open)
                Conexion.Close();
        }

        public void Dispose()
        {
            Conexion.Close();
        }
    }
}