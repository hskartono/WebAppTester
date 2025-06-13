using System.Collections.Generic;

namespace WebAppTester.Models
{
    /// <summary>
    /// Root configuration class that represents the YAML test file
    /// </summary>
    public class TestConfiguration
    {
        /// <summary>
        /// Base URL for all API requests
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Database connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// List of test steps to execute
        /// </summary>
        public List<TestStep> Steps { get; set; } = new List<TestStep>();
        
        /// <summary>
        /// Storage for variables that can be used across test steps
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents a single test step to be executed
    /// </summary>
    public class TestStep
    {
        /// <summary>
        /// Name of the test step
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what the test step does
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// API request details, null if this is a database step
        /// </summary>
        public ApiRequest? ApiRequest { get; set; }
        
        /// <summary>
        /// Database action details, null if this is an API step
        /// </summary>
        public DatabaseAction? DatabaseAction { get; set; }
        
        /// <summary>
        /// Indicates if this step is an authentication step that should extract a token
        /// </summary>
        public AuthenticationDetails? Authentication { get; set; }
    }

    /// <summary>
    /// Authentication details for extracting tokens from responses
    /// </summary>
    public class AuthenticationDetails
    {
        /// <summary>
        /// Path to extract the token from the response JSON
        /// </summary>
        public string TokenPath { get; set; } = "$.token";
        
        /// <summary>
        /// Name of the variable to store the token
        /// </summary>
        public string VariableName { get; set; } = "bearerToken";
        
        /// <summary>
        /// Token type for authorization header (e.g., "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";
    }    /// <summary>
    /// Represents an API request to be made
    /// </summary>
    public class ApiRequest
    {
        /// <summary>
        /// HTTP method (GET, POST, PUT, PATCH, DELETE)
        /// </summary>
        public string Method { get; set; } = "GET";
        
        /// <summary>
        /// Endpoint path (will be appended to BaseUrl)
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;
        
        /// <summary>
        /// Headers to include in the request
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Request body for POST, PUT, PATCH requests
        /// </summary>
        public object? Body { get; set; }
        
        /// <summary>
        /// Assertions to run against the response
        /// </summary>
        public List<Assertion> Assertions { get; set; } = new List<Assertion>();
        
        /// <summary>
        /// Whether to use the authenticated token in this request
        /// </summary>
        public bool UseAuthentication { get; set; } = false;
    }

    /// <summary>
    /// Represents a database action to be performed
    /// </summary>
    public class DatabaseAction
    {
        /// <summary>
        /// SQL query to execute
        /// </summary>
        public string Query { get; set; } = string.Empty;
        
        /// <summary>
        /// Query parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Assertions to run against the query results
        /// </summary>
        public List<Assertion> Assertions { get; set; } = new List<Assertion>();
    }

    /// <summary>
    /// Represents an assertion to validate results
    /// </summary>
    public class Assertion
    {
        /// <summary>
        /// Type of assertion (equals, contains, greaterThan, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to the property to check (for API responses)
        /// </summary>
        public string? PropertyPath { get; set; }
        
        /// <summary>
        /// Column name (for database results)
        /// </summary>
        public string? Column { get; set; }
        
        /// <summary>
        /// Expected value to compare against
        /// </summary>
        public object? ExpectedValue { get; set; }
    }
}
