using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebAppTester.Models;

namespace WebAppTester.Services
{
    /// <summary>
    /// Service for executing API requests and handling responses
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Executes an API request and returns the response
        /// </summary>
        /// <param name="baseUrl">Base URL from the test configuration</param>
        /// /// <param name="request">API request details</param>
        /// <param name="variables">Dictionary of variables for substitution</param>
        /// <returns>HTTP response and parsed JSON object if applicable</returns>
        public async Task<(HttpResponseMessage Response, JToken? JsonResponse)> ExecuteRequestAsync(
            string baseUrl, 
            ApiRequest request, 
            Dictionary<string, string>? variables = null)
        {
            // Create request URI by combining base URL and endpoint
            var requestUri = new Uri(new Uri(baseUrl), request.Endpoint);
            
            // Create HTTP request message
            var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), requestUri);
            // Add headers with variable substitution
            foreach (var header in request.Headers)
            {
                var headerValue = header.Value;
                
                // Handle variable substitution in header values
                if (variables != null && headerValue.Contains("${"))
                {
                    foreach (var variable in variables)
                    {
                        headerValue = headerValue.Replace($"${{{variable.Key}}}", variable.Value);
                    }
                }
                
                // Skip Content-Type header as it will be set with the request content
                if (!string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, headerValue);
                }
            }
            
            // Add authentication token if required
            if (request.UseAuthentication && variables != null && variables.ContainsKey("bearerToken"))
            {
                var tokenType = variables.ContainsKey("tokenType") ? variables["tokenType"] : "Bearer";
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue(tokenType, variables["bearerToken"]);
            }
              // Add request body for POST, PUT, PATCH
            if (request.Body != null && 
                (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                 request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || 
                 request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
            {
                // Convert body to JObject to support variable substitution
                JObject bodyObj;
                
                if (request.Body is JObject jObj)
                {
                    bodyObj = jObj;
                }
                else
                {
                    bodyObj = JObject.FromObject(request.Body);
                }
                
                // Handle variable substitution in body
                if (variables != null)
                {
                    SubstituteVariablesInBody(bodyObj, variables);
                }
                  string jsonBody = JsonConvert.SerializeObject(bodyObj);
                httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                
                // Set Content-Type header if specified in request headers
                if (request.Headers.ContainsKey("Content-Type"))
                {
                    httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(request.Headers["Content-Type"]);
                }
            }
            
            // Execute request
            HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);
            
            // Parse JSON response if content exists
            JToken? jsonResponse = null;
            string responseContent = await response.Content.ReadAsStringAsync();
            
            if (!string.IsNullOrWhiteSpace(responseContent) && 
                response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
            {
                try
                {
                    jsonResponse = JToken.Parse(responseContent);
                }
                catch (JsonReaderException)
                {
                    // Not valid JSON, ignore
                }
            }
            
            return (response, jsonResponse);
        }
          /// <summary>
        /// Recursively substitute variables in a JSON body
        /// </summary>
        private void SubstituteVariablesInBody(JToken token, Dictionary<string, string>? variables)
        {
            if (variables == null)
                return;
                
            if (token is JObject obj)
            {
                foreach (var property in obj.Properties().ToList())
                {
                    if (property.Value is JValue val && val.Type == JTokenType.String)
                    {                        string? stringValue = val.Value<string>();
                        if (stringValue != null && stringValue.Contains("${"))
                        {
                            foreach (var variable in variables)
                            {
                                stringValue = stringValue.Replace($"${{{variable.Key}}}", variable.Value);
                            }
                            property.Value = stringValue;
                        }
                    }
                    else
                    {
                        SubstituteVariablesInBody(property.Value, variables);
                    }
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    SubstituteVariablesInBody(item, variables);
                }
            }
        }
    }
}
