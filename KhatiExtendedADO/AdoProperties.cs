using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace KhatiExtendedADO
{
    
    public class AdoProperties : IAdoProperties
    {
        public virtual string ConnectionString()
        {
            return "";
        }
        public virtual int TimeOut()
        {
            return 300;
        }
        public async Task<Response<string>> SqlWriteAsync(string query, Dictionary<string, object> parameters)
        {
            try
            {
                if (!query.Contains("@"))
                {
                    return new Response<string>
                    {
                        Success = false,
                        Message = "Query must be parameterized (contain @parameters).",
                        Data = null,
                        Exception = "Parameterless query rejected for security."
                    };
                }

                if (parameters == null || parameters.Count == 0)
                {
                    return new Response<string>
                    {
                        Success = false,
                        Message = "Parameter dictionary is empty. Parameterized query must have values.",
                        Data = null,
                        Exception = "Missing parameter values."
                    };
                }

                using (SqlConnection sc = new SqlConnection(ConnectionString()))
                using (SqlCommand com = new SqlCommand(query, sc))
                {
					com.CommandTimeout = TimeOut();

					foreach (var param in parameters)
                    {
                        com.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }

                    await sc.OpenAsync();
                    await com.ExecuteNonQueryAsync();
                }

                return new Response<string>
                {
                    Success = true,
                    Data = "No Data Available",
                    Message = "Executed Successfully",
                    Exception = null
                };
            }
            catch (Exception ex)
            {
                return new Response<string>
                {
                    Success = false,
                    Data = null,
                    Message = ex.Message,
                    Exception = ex.ToString()
                };
            }
        }
        public async Task<Response<TResponse>> SqlReadAsync<TResponse>(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {

                if (query.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!query.Contains("@") || parameters == null || parameters.Count == 0)
                    {
                        return new Response<TResponse>
                        {
                            Success = false,
                            Data = default,
                            Message = "Query contains a WHERE clause but no parameters were provided.",
                            Exception = "Unsafe or incomplete parameterized query."
                        };
                    }
                }


                using var connection = new SqlConnection(ConnectionString());
                using var command = new SqlCommand(query, connection);

                command.CommandTimeout = TimeOut();

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                string? json = ToJson(reader);
                var result = JsonConvert.DeserializeObject<TResponse>(json);

                return new Response<TResponse>
                {
                    Success = true,
                    Data = result,
                    Message = "Execution Successful",
                    Exception = null
                };
            }
            catch (Exception ex)
            {
                return new Response<TResponse>
                {
                    Success = false,
                    Data = default,
                    Message = ex.Message,
                    Exception = ex.ToString()
                };
            }
        }
        public async Task<Response<TResponse>> SqlReadScalerModelAsync<TResponse>(string query, Dictionary<string, object>? parameters = null) 
        {
            try
            {
                if (query.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!query.Contains("@") || parameters == null || parameters.Count == 0)
                    {
                        return new Response<TResponse>
                        {
                            Success = false,
                            Data = default,
                            Message = "Query contains a WHERE clause but no parameters were provided.",
                            Exception = "Unsafe or incomplete parameterized query."
                        };
                    }
                }

                using var connection = new SqlConnection(ConnectionString());
                using var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = TimeOut()
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                string? json = ToJson(reader);
                var result = JsonConvert.DeserializeObject<List<TResponse>>(json);

                return new Response<TResponse>
                {
                    Success = true,
                    Data = result == null? default(TResponse): result.FirstOrDefault(),
                    Message = "Execution Successful",
                    Exception = null
                };
            }
            catch (Exception ex)
            {
                return new Response<TResponse>
                {
                    Success = false,
                    Data = default(TResponse),
                    Message = ex.Message,
                    Exception = ex.ToString()
                };
            }
        }
        public async Task<Response<TResponse>> SqlReadScalerValueAsync<TResponse>(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                if (query.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!query.Contains("@") || parameters == null || parameters.Count == 0)
                    {
                        return new Response<TResponse>
                        {
                            Success = false,
                            Data = default,
                            Message = "Query contains a WHERE clause but no parameters were provided.",
                            Exception = "Unsafe or incomplete parameterized query."
                        };
                    }
                }

                using var connection = new SqlConnection(ConnectionString());
                using var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = TimeOut()
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                await connection.OpenAsync();

                object? scalarResult = await command.ExecuteScalarAsync();
                await connection.CloseAsync();

                return new Response<TResponse>
                {
                    Success = true,
                    Data = scalarResult == null ? default : (TResponse)Convert.ChangeType(scalarResult, typeof(TResponse)),
                    Message = "Execution Successful",
                    Exception = null
                };
            }
            catch (Exception ex)
            {
                return new Response<TResponse>
                {
                    Success = false,
                    Data = default,
                    Message = ex.Message,
                    Exception = ex.ToString()
                };
            }
        }
        public async Task<(bool success, string? message, string? errorMessage)> SqlBulkUploadAsync<T>(List<T> model,
            string tableName) where T : class
        {
            SqlConnection con = new SqlConnection(ConnectionString());
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con);
            try
            {

                sqlBulkCopy.DestinationTableName = tableName;
                sqlBulkCopy.BulkCopyTimeout = TimeOut();
                await con.OpenAsync();
                DataTable dt = ToDataTable(model);
                await sqlBulkCopy.WriteToServerAsync(dt);
                await con.CloseAsync();
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
