using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace KhatiExtendedADO
{
    
    public class AdoProperties : IAdoProperties
    {
        public virtual string ConnectionString()
        {
            return "";
        }
        public Response<string> SqlWrite(string Query)
        {
            try
            {
                SqlConnection sc = new SqlConnection();
                SqlCommand com = new SqlCommand();
                sc.ConnectionString = (ConnectionString());
                sc.Open();
                com.Connection = sc;
                com.CommandText = (Query);
                com.ExecuteNonQuery();
                sc.Close();
                var model = new Response<string>()
                {
                    Success = true,
                    Data = "No Data Available",
                    Message = "Executed Successfully",
                    Exception = null
                };
                return model;
            }
            catch (Exception ex)
            {
                var model = new Response<string>()
                {
                    Success = false,
                    Data = null,
                    Message = ex.Message.ToString(),
                    Exception = ex.ToString()
                };
                return model;
            }
        }
        public Response<TResponse> SqlRead<TResponse>(string Query)
        {
            try
            {
                var connection = new SqlConnection(ConnectionString());
                connection.Open();
                SqlCommand comand = new SqlCommand(
                Query, connection);
                comand.CommandTimeout = 300;
                var reading = comand.ExecuteReader();
                string? json = ToJson(reading);
                var result = JsonConvert.DeserializeObject<TResponse>(json);
                connection.Close();
                reading.Close();
                var model = new Response<TResponse>()
                {
                    Success = true,
                    Data = result,
                    Message = "Execution Successfull",
                    Exception = null
                };

                return model;
            }
            catch (Exception ex)
            {
                var model = new Response<TResponse>()
                {
                    Success = false,
                    Data = default(TResponse),
                    Message = ex.Message.ToString(),
                    Exception = ex.ToString()
                };

                return model;
            }
        }
        public Response<TResponse> SqlReadScalerModel<TResponse>(string Query) where TResponse : class
        {
            try
            {
                var connection = new SqlConnection(ConnectionString());
                connection.Open();
                SqlCommand comand = new SqlCommand(
                Query, connection);
                comand.CommandTimeout = 300;
                var reading = comand.ExecuteReader();
                string? json = ToJson(reading);
                var result = JsonConvert.DeserializeObject<List<TResponse>>(json);
                connection.Close();
                reading.Close();
                var model = new Response<TResponse>()
                {
                    Success = true,
                    Data = result == null? default(TResponse) : result.Any()? result.FirstOrDefault(): default(TResponse),
                    Message = "Execution Successfull",
                    Exception = null
                };

                return model;
            }
            catch (Exception ex)
            {
                var model = new Response<TResponse>()
                {
                    Success = false,
                    Data = default(TResponse),
                    Message = ex.Message.ToString(),
                    Exception = ex.ToString()
                };

                return model;
            }
        }
        public Response<TResponse> SqlReadScalerValue<TResponse>(string Query)
        {
            try
            {
                var connection = new SqlConnection(ConnectionString());
                connection.Open();
                SqlCommand comand = new SqlCommand(
                Query, connection);
                comand.CommandTimeout = 300;
                var reading = (TResponse)comand.ExecuteScalar();
                connection.Close();
                var model = new Response<TResponse>()
                {
                    Success = true,
                    Data = reading,
                    Message = "Execution Successfull",
                    Exception = null
                };

                return model;
            }
            catch (Exception ex)
            {
                var model = new Response<TResponse>()
                {
                    Success = false,
                    Data = default(TResponse),
                    Message = ex.Message.ToString(),
                    Exception = ex.ToString()
                };

                return model;
            }
        }
        public (bool success, string? message, string? errorMessage) SqlBulkUpload<T>(List<T> model,
            string tableName) where T : class
        {
            SqlConnection con = new SqlConnection(ConnectionString());
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con);
            try
            {

                sqlBulkCopy.DestinationTableName = tableName;
                sqlBulkCopy.BulkCopyTimeout = 120;
                con.Open();
                DataTable dt = ToDataTable(model);
                sqlBulkCopy.WriteToServer(dt);
                con.Close();
                return (true, "Data Inserted Successfully", null);
            }
            catch (Exception ex)
            {

                if (ex.Message.Contains("Received an invalid column length from the bcp client for colid"))
                {
                    string pattern = @"\d+";
                    Match match = Regex.Match(ex.Message.ToString(), pattern);
                    var index = Convert.ToInt32(match.Value) - 1;

                    FieldInfo fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings", BindingFlags.NonPublic | BindingFlags.Instance);
                    var sortedColumns = fi.GetValue(sqlBulkCopy);
                    var items = (Object[])sortedColumns.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sortedColumns);

                    FieldInfo itemdata = items[index].GetType().GetField("_metadata", BindingFlags.NonPublic | BindingFlags.Instance);
                    var metadata = itemdata.GetValue(items[index]);

                    var column = metadata.GetType().GetField("column", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
                    var length = metadata.GetType().GetField("length", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
                }

                return (false, "Data Insertion Failed", ex.Message);
            }
        }

        #region others
        private DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                dataTable.Columns.Add(prop.Name);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            return dataTable;
        }
        private string ToJson(SqlDataReader rdr)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.WriteStartArray();

                while (rdr.Read())
                {
                    jsonWriter.WriteStartObject();

                    int fields = rdr.FieldCount;

                    for (int i = 0; i < fields; i++)
                    {
                        jsonWriter.WritePropertyName(rdr.GetName(i));
                        jsonWriter.WriteValue(rdr[i]);
                    }

                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndArray();

                return sw.ToString();
            }
        }
        #endregion
    }
}
