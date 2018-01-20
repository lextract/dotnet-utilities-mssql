using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace Lextract.DotNetUtilities.MsSql
{
    public class DbConnector : IDisposable
    {
        public SqlConnection Conexion { get; private set; }
        public SqlCommand Comando { get; private set; }
        public DbConnector(SqlConnection connection)
        {
            this.Conexion = connection;
        }

        public DbConnector(string connectionString)
        {
            Conexion = new SqlConnection(connectionString);
        }

        public void SetProcedure(string procedureName)
        {
            // TODO: Implementar validacion que el nombre no tenga espacios en blanco
            Comando = new SqlCommand(procedureName, Conexion);
            Comando.CommandType = CommandType.StoredProcedure;
        }

        public void SetCommand(string commandText)
        {
            Comando = new SqlCommand(commandText, Conexion);
            Comando.CommandType = CommandType.Text;
        }

        public void AddParameter(string parameter)
        {
            Comando.Parameters.Add(parameter, SqlDbType.Variant);
        }

        public void AddParameter(string parameter, SqlDbType dbType)
        {
            Comando.Parameters.Add(parameter, dbType);
        }

        public void AddParameter(SqlParameter parameter)
        {
            Comando.Parameters.Add(parameter);
        }

        public void AddParameterValue(string parameter, object value)
        {
            if (Comando.Parameters.Contains(parameter))
                Comando.Parameters[parameter].Value = value;
            else Comando.Parameters.AddWithValue(parameter, value);
        }

        public int ExecuteNonQuery()
        {
            int retorno = 0;
            AbrirConexion();
            retorno = Comando.ExecuteNonQuery();
            return retorno;
        }

        public object ExecuteScalar()
        {
            AbrirConexion();
            object retorno = Comando.ExecuteScalar();
            return retorno;
        }

        public DataTable ExecuteReaderRaw()
        {
            AbrirConexion();
            DataTable tabla = new DataTable();
            using (SqlDataReader reader = Comando.ExecuteReader())
                tabla.Load(reader);

            CerrarConexion();
            return tabla;
        }


        public DataSet ExecuteReaderDataSet()
        {
            AbrirConexion();
            DataSet dataSet = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(Comando);
            da.Fill(dataSet);
            //dataSet.Load(reader);
            CerrarConexion();
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

        public List<T> ExecuteReaderMapping<T>()
        {
            List<T> retorno = new List<T>();
            AbrirConexion();
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

            CerrarConexion();
            return retorno;
        }

        public List<T> ExecuteReaderToType<T>()
        {
            List<T> retorno = new List<T>();
            AbrirConexion();

            using (SqlDataReader reader = Comando.ExecuteReader())
            {
                Type typeT = typeof(T);

                while (reader.Read())
                {
                    T valor = (T)Convert.ChangeType(reader[0].ToString(), typeT);
                    retorno.Add(valor);
                }
            }
            CerrarConexion();
            return retorno;
        }

        public T ExecuteReaderMappingFirst<T>()
        {
            AbrirConexion();
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
                CerrarConexion();
                return retorno;
            }

        }

        public void BulkCopy(DataTable sourceTable, string targetTable)
        {
            AbrirConexion();
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Conexion))
            {
                bulkCopy.DestinationTableName = targetTable;
                bulkCopy.WriteToServer(sourceTable);
            }
            CerrarConexion();
        }

        protected void AbrirConexion()
        {
            if (Conexion.State != ConnectionState.Open)
                Conexion.Open();
        }

        protected void CerrarConexion()
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