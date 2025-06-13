using System.Data;
using Newtonsoft.Json.Linq;
using WebAppTester.Models;

namespace WebAppTester.Services
{
    /// <summary>
    /// Service for validating assertions against API responses and database results
    /// </summary>
    public class AssertionService
    {
        /// <summary>
        /// Validates assertions against an API response
        /// </summary>
        /// <param name="assertions">List of assertions to validate</param>
        /// <param name="response">API response as JToken</param>
        /// <returns>List of assertion results</returns>
        public List<AssertionResult> ValidateApiAssertions(List<Assertion> assertions, JToken response)
        {
            var results = new List<AssertionResult>();
            
            foreach (var assertion in assertions)
            {
                var result = new AssertionResult
                {
                    AssertionType = assertion.Type,
                    PropertyPath = assertion.PropertyPath
                };
                
                try
                {
                    // Get the property value from the response using the path
                    JToken? propertyToken = null;
                    
                    if (string.IsNullOrEmpty(assertion.PropertyPath))
                    {
                        propertyToken = response;
                    }
                    else
                    {
                        propertyToken = response.SelectToken(assertion.PropertyPath!);
                    }
                    
                    if (propertyToken == null)
                    {
                        result.Success = false;
                        result.Message = $"Property path '{assertion.PropertyPath}' not found in response";
                    }
                    else
                    {
                        // Validate based on assertion type
                        result = ValidateAssertion(assertion, propertyToken.ToString(), result);
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Error validating assertion: {ex.Message}";
                }
                
                results.Add(result);
            }
            
            return results;
        }
        
        /// <summary>
        /// Validates assertions against database query results
        /// </summary>
        /// <param name="assertions">List of assertions to validate</param>
        /// <param name="dataTable">Database query results</param>
        /// <returns>List of assertion results</returns>
        public List<AssertionResult> ValidateDatabaseAssertions(List<Assertion> assertions, DataTable dataTable)
        {
            var results = new List<AssertionResult>();
            
            foreach (var assertion in assertions)
            {
                var result = new AssertionResult
                {
                    AssertionType = assertion.Type,
                    Column = assertion.Column
                };
                
                try
                {
                    // Check if column exists
                    if (string.IsNullOrEmpty(assertion.Column) || !dataTable.Columns.Contains(assertion.Column))
                    {
                        result.Success = false;
                        result.Message = $"Column '{assertion.Column}' not found in query results";
                        results.Add(result);
                        continue;
                    }
                    
                    // For row count assertions
                    if (assertion.Column.Equals("RowCount", StringComparison.OrdinalIgnoreCase))
                    {
                        var actualValue = dataTable.Rows.Count.ToString();
                        result = ValidateAssertion(assertion, actualValue, result);
                        results.Add(result);
                        continue;
                    }
                    
                    // Make sure we have rows
                    if (dataTable.Rows.Count == 0)
                    {
                        result.Success = false;
                        result.Message = "No rows returned from database query";
                        results.Add(result);
                        continue;
                    }
                    
                    // Get the first row value by default
                    var value = dataTable.Rows[0][assertion.Column].ToString();
                    result = ValidateAssertion(assertion, value, result);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Error validating assertion: {ex.Message}";
                }
                
                results.Add(result);
            }
            
            return results;
        }
        
        /// <summary>
        /// Validates a single assertion based on its type
        /// </summary>
        /// <param name="assertion">Assertion to validate</param>
        /// <param name="actualValue">Actual value as string</param>
        /// <param name="result">Assertion result to update</param>
        /// <returns>Updated assertion result</returns>
        private AssertionResult ValidateAssertion(Assertion assertion, string? actualValue, AssertionResult result)
        {
            var expectedValue = assertion.ExpectedValue?.ToString();
            result.ActualValue = actualValue;
            result.ExpectedValue = expectedValue;
            
            switch (assertion.Type.ToLower())
            {
                case "equals":
                    result.Success = string.Equals(actualValue, expectedValue);
                    break;
                    
                case "notequals":
                    result.Success = !string.Equals(actualValue, expectedValue);
                    break;
                    
                case "contains":
                    result.Success = actualValue?.Contains(expectedValue ?? "") == true;
                    break;
                    
                case "notcontains":
                    result.Success = actualValue?.Contains(expectedValue ?? "") != true;
                    break;
                    
                case "startswith":
                    result.Success = actualValue?.StartsWith(expectedValue ?? "") == true;
                    break;
                    
                case "endswith":
                    result.Success = actualValue?.EndsWith(expectedValue ?? "") == true;
                    break;
                    
                case "greater":
                case "greaterthan":
                    if (double.TryParse(actualValue, out double actualNum) && 
                        double.TryParse(expectedValue, out double expectedNum))
                    {
                        result.Success = actualNum > expectedNum;
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "Values could not be converted to numbers for comparison";
                    }
                    break;
                    
                case "less":
                case "lessthan":
                    if (double.TryParse(actualValue, out double actualNumLess) && 
                        double.TryParse(expectedValue, out double expectedNumLess))
                    {
                        result.Success = actualNumLess < expectedNumLess;
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "Values could not be converted to numbers for comparison";
                    }
                    break;
                    
                case "notempty":
                    result.Success = !string.IsNullOrEmpty(actualValue);
                    break;
                    
                case "empty":
                    result.Success = string.IsNullOrEmpty(actualValue);
                    break;
                    
                default:
                    result.Success = false;
                    result.Message = $"Unknown assertion type: {assertion.Type}";
                    break;
            }
            
            // Set message if not already set
            if (string.IsNullOrEmpty(result.Message))
            {
                result.Message = result.Success 
                    ? "Assertion passed" 
                    : $"Assertion failed. Expected: {expectedValue}, Actual: {actualValue}";
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Represents the result of a single assertion validation
    /// </summary>
    public class AssertionResult
    {
        /// <summary>
        /// Type of assertion that was validated
        /// </summary>
        public string AssertionType { get; set; } = string.Empty;
        
        /// <summary>
        /// Property path for API assertions
        /// </summary>
        public string? PropertyPath { get; set; }
        
        /// <summary>
        /// Column name for database assertions
        /// </summary>
        public string? Column { get; set; }
        
        /// <summary>
        /// Whether the assertion passed
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Descriptive message about the assertion result
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// The actual value that was compared
        /// </summary>
        public string? ActualValue { get; set; }
        
        /// <summary>
        /// The expected value from the assertion
        /// </summary>
        public string? ExpectedValue { get; set; }
    }
}
