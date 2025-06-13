# Web API Tester

A .NET application for testing Web APIs and database operations using YAML configuration files.

## Features

- Execute HTTP requests (GET, POST, PUT, PATCH)
- Perform database queries and commands
- Support for assertions on both API responses and database results
- Detailed test reports

## Requirements

- .NET 9.0 SDK
- Microsoft SQL Server (for database operations)

## Usage

```bash
dotnet run -- path/to/your/test-config.yaml
```

## YAML Configuration Format

The YAML configuration file defines the test steps to be executed. Here's the basic structure:

```yaml
# Base URL for all API requests
baseUrl: https://api.example.com

# Database connection string
connectionString: Server=localhost;Database=TestDB;Trusted_Connection=True;

# Test steps to execute
steps:
  - name: Step Name
    description: Step description
    apiRequest:
      method: GET/POST/PUT/PATCH
      endpoint: /api/endpoint
      headers:
        Header-Name: header-value
      body: {}  # For POST, PUT, PATCH
      useAuthentication: false  # Set to true to use bearer token
      assertions:
        - type: equals
          propertyPath: $.property.path
          expectedValue: expected-value
    # Optional authentication details to extract token
    authentication:
      tokenPath: $.token.path
      variableName: bearerToken
      tokenType: Bearer

  - name: Database Step
    description: Execute database query
    databaseAction:
      query: SELECT * FROM Table WHERE Column = @Param
      parameters:
        "@Param": value
      assertions:
        - type: equals
          column: ColumnName
          expectedValue: expected-value
```

## Assertion Types

### For API Responses

- `equals`: Checks if the value equals the expected value
- `notEquals`: Checks if the value does not equal the expected value
- `contains`: Checks if the value contains the expected value
- `notContains`: Checks if the value does not contain the expected value
- `startsWith`: Checks if the value starts with the expected value
- `endsWith`: Checks if the value ends with the expected value
- `greater` / `greaterThan`: Checks if the value is greater than the expected value
- `less` / `lessThan`: Checks if the value is less than the expected value
- `notEmpty`: Checks if the value is not empty
- `empty`: Checks if the value is empty

### For Database Results

- All the assertion types above
- `rowCount`: Special column name to assert the number of rows returned

## Example

See `SampleTest.yaml` for a complete example configuration.
See `LoginSampleTest.yaml` for an example with authentication and token reuse.

## Authentication and Variables

WebAppTester supports authentication workflows where a token from a login request can be extracted and used in subsequent requests:

1. Create a login step with `authentication` details to extract the token
2. Set `useAuthentication: true` on subsequent API requests to use the token
3. Variables can be referenced in request bodies and headers using `${variableName}` syntax

For detailed documentation on authentication features, see [AuthenticationGuide.md](AuthenticationGuide.md).

## License

MIT
