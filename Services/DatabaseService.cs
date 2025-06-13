using System.Data;
using Microsoft.Data.SqlClient;
using WebAppTester.Models;

namespace WebAppTester.Services
{
    /// <summary>
    /// Service for executing database queries and handling results
    /// </summary>
    public class DatabaseService
    {
        /// <summary>
        /// Executes a database query and returns the results
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="action">Database action details</param>
        /// <param name="variables">Dictionary of variables for substitution</param>
        /// <returns>DataTable containing query results</returns>
        public async Task<DataTable> ExecuteQueryAsync(string connectionString, DatabaseAction action, Dictionary<string, string>? variables = null)
        {
            var resultTable = new DataTable();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(action.Query, connection))
                {
                    // Add parameters with variable substitution
                    foreach (var param in action.Parameters)
                    {
                        var paramValue = param.Value;
                        // Handle variable substitution in parameter values
                        if (variables != null && paramValue is string stringValue && stringValue.Contains("${"))
                        {
                            foreach (var variable in variables)
                            {
                                stringValue = stringValue.Replace($"${{{variable.Key}}}", variable.Value);
                            }
                            command.Parameters.AddWithValue(param.Key, string.IsNullOrEmpty(stringValue) ? DBNull.Value : (object)stringValue);
                        }
                        else
                        {
                            command.Parameters.AddWithValue(param.Key, paramValue ?? DBNull.Value);
                        }
                    }

                    // Execute query and fill DataTable
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(resultTable);
                    }
                }
            }

            return resultTable;
        }

        /// <summary>
        /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns affected rows
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="action">Database action details</param>
        /// <param name="variables">Dictionary of variables for substitution</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteNonQueryAsync(string connectionString, DatabaseAction action, Dictionary<string, string>? variables = null)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(action.Query, connection))
                {
                    // Add parameters with variable substitution
                    foreach (var param in action.Parameters)
                    {
                        var paramValue = param.Value;

                        // Handle variable substitution in parameter values
                        if (variables != null && paramValue is string stringValue && stringValue.Contains("${"))
                        {
                            foreach (var variable in variables)
                            {
                                stringValue = stringValue.Replace($"${{{variable.Key}}}", variable.Value);
                            }
                            command.Parameters.AddWithValue(param.Key, string.IsNullOrEmpty(stringValue) ? DBNull.Value : (object)stringValue);
                        }
                        else
                        {
                            command.Parameters.AddWithValue(param.Key, paramValue ?? DBNull.Value);
                        }
                    }

                    // Execute non-query command
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
