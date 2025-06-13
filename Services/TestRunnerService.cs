using System.Data;
using Newtonsoft.Json.Linq;
using WebAppTester.Models;
using WebAppTester.Services;

namespace WebAppTester.Services
{
    /// <summary>
    /// Service for running test steps from a configuration
    /// </summary>
    public class TestRunnerService
    {
        private readonly YamlConfigurationService _configService;
        private readonly ApiService _apiService;
        private readonly DatabaseService _dbService;
        private readonly AssertionService _assertionService;
        
        public TestRunnerService()
        {
            _configService = new YamlConfigurationService();
            _apiService = new ApiService();
            _dbService = new DatabaseService();
            _assertionService = new AssertionService();
        }

        /// <summary>
        /// Runs a test suite from a YAML configuration file
        /// </summary>
        /// <param name="yamlFilePath">Path to the YAML configuration file</param>
        /// <returns>Test run results</returns>
        public async Task<TestRunResult> RunTestsAsync(string yamlFilePath)
        {
            // Load configuration
            var config = _configService.LoadConfiguration(yamlFilePath);

            // Initialize Variables dictionary if it's null
            if (config.Variables == null)
            {
                config.Variables = new Dictionary<string, string>();
            }

            var result = new TestRunResult
            {
                StartTime = DateTime.Now,
                ConfigFileName = Path.GetFileName(yamlFilePath),
                StepResults = new List<StepResult>()
            };

            Console.WriteLine($"Running tests from {result.ConfigFileName}");
            Console.WriteLine($"Base URL: {config.BaseUrl}");
            Console.WriteLine($"Total steps: {config.Steps.Count}");
            Console.WriteLine();

            // Execute each step
            foreach (var step in config.Steps)
            {
                var stepResult = new StepResult
                {
                    StepName = step.Name,
                    Description = step.Description,
                    StartTime = DateTime.Now
                };

                Console.WriteLine($"Running step: {step.Name}");
                Console.WriteLine($"Description: {step.Description}");

                try
                {
                    // Execute API request
                    if (step.ApiRequest != null)
                    {
                        stepResult = await ExecuteApiStepAsync(config.BaseUrl, step.ApiRequest, stepResult, config.Variables);

                        // Check if this is an authentication step and extract token
                        if (step.Authentication != null && stepResult.Success && stepResult.JsonResponse != null)
                        {
                            ExtractAuthToken(step.Authentication, stepResult.JsonResponse, config.Variables);
                        }
                    }
                    // Execute Database action
                    else if (step.DatabaseAction != null)
                    {
                        stepResult = await ExecuteDatabaseStepAsync(config.ConnectionString, step.DatabaseAction, stepResult, config.Variables);
                    }
                    else
                    {
                        stepResult.Success = false;
                        stepResult.ErrorMessage = "Step does not contain either ApiRequest or DatabaseAction";
                    }
                }
                catch (Exception ex)
                {
                    stepResult.Success = false;
                    stepResult.ErrorMessage = $"Error executing step: {ex.Message}";
                }

                stepResult.EndTime = DateTime.Now;
                stepResult.DurationMs = (stepResult.EndTime - stepResult.StartTime).TotalMilliseconds;

                result.StepResults.Add(stepResult);

                Console.WriteLine($"Step result: {(stepResult.Success ? "PASSED" : "FAILED")}");
                if (!stepResult.Success && !string.IsNullOrEmpty(stepResult.ErrorMessage))
                {
                    Console.WriteLine($"Error: {stepResult.ErrorMessage}");
                }

                if (stepResult.AssertionResults.Count > 0)
                {
                    Console.WriteLine("Assertion results:");
                    foreach (var assertionResult in stepResult.AssertionResults)
                    {
                        Console.WriteLine($"  - {assertionResult.AssertionType}: {(assertionResult.Success ? "PASSED" : "FAILED")} - {assertionResult.Message}");
                    }
                }

                Console.WriteLine();
            }

            result.EndTime = DateTime.Now;
            result.DurationMs = (result.EndTime - result.StartTime).TotalMilliseconds;
            result.TotalSteps = result.StepResults.Count;
            result.PassedSteps = result.StepResults.Count(s => s.Success);
            result.FailedSteps = result.StepResults.Count(s => !s.Success);

            // Output summary
            Console.WriteLine("Test Run Summary:");
            Console.WriteLine($"Total Steps: {result.TotalSteps}");
            Console.WriteLine($"Passed Steps: {result.PassedSteps}");
            Console.WriteLine($"Failed Steps: {result.FailedSteps}");
            Console.WriteLine($"Duration: {result.DurationMs:N0}ms");

            return result;
        }        /// <summary>
        /// Executes an API test step
        /// </summary>
        private async Task<StepResult> ExecuteApiStepAsync(string baseUrl, ApiRequest request, StepResult stepResult, Dictionary<string, string>? variables = null)
        {
            Console.WriteLine($"API Request: {request.Method} {request.Endpoint}");
            
            var (response, jsonResponse) = await _apiService.ExecuteRequestAsync(baseUrl, request, variables);
            
            stepResult.StatusCode = (int)response.StatusCode;
            stepResult.JsonResponse = jsonResponse;
            Console.WriteLine($"Status Code: {stepResult.StatusCode} ({response.StatusCode})");
            
            // Validate assertions if any
            if (request.Assertions.Count > 0 && jsonResponse != null)
            {
                stepResult.AssertionResults = _assertionService.ValidateApiAssertions(request.Assertions, jsonResponse);
                stepResult.Success = stepResult.AssertionResults.All(a => a.Success);
            }
            else
            {
                // If no assertions, consider success if status code is 2xx
                stepResult.Success = ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300);
            }
            
            return stepResult;
        }        /// <summary>
        /// Executes a database test step
        /// </summary>
        private async Task<StepResult> ExecuteDatabaseStepAsync(string connectionString, DatabaseAction action, StepResult stepResult, Dictionary<string, string>? variables = null)
        {
            Console.WriteLine($"Database Query: {action.Query}");
            
            try
            {
                DataTable results = await _dbService.ExecuteQueryAsync(connectionString, action, variables);
                Console.WriteLine($"Database Results: {results.Rows.Count} rows returned");
                
                // Validate assertions if any
                if (action.Assertions.Count > 0)
                {
                    stepResult.AssertionResults = _assertionService.ValidateDatabaseAssertions(action.Assertions, results);
                    stepResult.Success = stepResult.AssertionResults.All(a => a.Success);
                }
                else
                {
                    // If no assertions, consider success if query executed without error
                    stepResult.Success = true;
                }
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = $"Database error: {ex.Message}";
            }
            
            return stepResult;
        }
          /// <summary>
        /// Extracts authentication token from a response
        /// </summary>
        private void ExtractAuthToken(AuthenticationDetails auth, JToken response, Dictionary<string, string> variables)
        {
            try
            {
                // Use JsonPath to extract token
                JToken? tokenValue = response.SelectToken(auth.TokenPath);
                
                if (tokenValue != null)
                {
                    string tokenString = tokenValue.ToString();
                    
                    // Store token in variables dictionary
                    variables[auth.VariableName] = tokenString;
                    
                    // Store token type if needed
                    if (!string.IsNullOrEmpty(auth.TokenType) && !variables.ContainsKey("tokenType"))
                    {
                        variables["tokenType"] = auth.TokenType;
                    }
                    
                    Console.WriteLine($"Extracted authentication token: {tokenString.Substring(0, Math.Min(10, tokenString.Length))}...");
                }
                else
                {
                    Console.WriteLine($"Warning: Could not extract token using path '{auth.TokenPath}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting token: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Represents the results of a test run
    /// </summary>
    public class TestRunResult
    {
        /// <summary>
        /// Name of the configuration file
        /// </summary>
        public string ConfigFileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Start time of the test run
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// End time of the test run
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Duration of the test run in milliseconds
        /// </summary>
        public double DurationMs { get; set; }
        
        /// <summary>
        /// Total number of steps executed
        /// </summary>
        public int TotalSteps { get; set; }
        
        /// <summary>
        /// Number of steps that passed
        /// </summary>
        public int PassedSteps { get; set; }
        
        /// <summary>
        /// Number of steps that failed
        /// </summary>
        public int FailedSteps { get; set; }
        
        /// <summary>
        /// Results of individual test steps
        /// </summary>
        public List<StepResult> StepResults { get; set; } = new List<StepResult>();
    }

    /// <summary>
    /// Represents the result of a single test step
    /// </summary>
    public class StepResult
    {
        /// <summary>
        /// Name of the step
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// Description of the step
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether the step was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code (for API steps)
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Error message if the step failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Start time of the step
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the step
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the step in milliseconds
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Results of assertions executed in this step
        /// </summary>
        public List<AssertionResult> AssertionResults { get; set; } = new List<AssertionResult>();

        /// <summary>
        /// JSON response from the API request
        /// </summary>
        public JToken? JsonResponse { get; set; }
    }
}
