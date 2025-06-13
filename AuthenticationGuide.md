# Authentication Guide for WebAppTester

This guide explains how to use authentication and token management features in WebAppTester.

## Authentication Workflow

WebAppTester supports a common authentication workflow for API testing:

1. Send a login request to obtain an authentication token
2. Extract the token from the response
3. Use that token in subsequent API requests

## Setting Up Authentication

To implement authentication in your YAML test files, follow these steps:

### 1. Create a Login Step

First, create a step that logs in to your API:

```yaml
- name: Login to API
  description: Authenticates with the API and obtains a token
  apiRequest:
    method: POST
    endpoint: /api/auth/login
    headers:
      Content-Type: application/json
    body:
      username: admin
      password: password123
  authentication:
    tokenPath: $.data.token
    variableName: bearerToken
    tokenType: Bearer
```

The `authentication` section has these properties:
- `tokenPath`: JSONPath expression to locate the token in the response (e.g., `$.data.token`)
- `variableName`: Name to store the token under (default: `bearerToken`)
- `tokenType`: Type of token for Authorization header (default: `Bearer`)

### 2. Use the Token in Subsequent Requests

Once the token is extracted, use it in subsequent requests by setting `useAuthentication: true`:

```yaml
- name: Get User Profile
  description: Retrieves user profile using the authenticated token
  apiRequest:
    method: GET
    endpoint: /api/users/profile
    headers:
      Accept: application/json
    useAuthentication: true
    assertions:
      - type: equals
        propertyPath: $.id
        expectedValue: 1
```

When `useAuthentication` is set to `true`, WebAppTester automatically adds the `Authorization` header with the token.

## Variable Substitution

You can reference any stored variable in headers or request bodies using the `${variableName}` syntax:

```yaml
- name: Create Item with User ID
  apiRequest:
    method: POST
    endpoint: /api/items
    headers:
      Content-Type: application/json
    body:
      name: Test Item
      description: "Created by user with ID: ${userId}"
```

## Complete Example

See `LoginSampleTest.yaml` for a complete example of authentication and token reuse.

## Tips and Best Practices

1. **Token Extraction**: Make sure your `tokenPath` correctly identifies the token in the API response
2. **Error Handling**: Add appropriate assertions to your login step to validate successful authentication
3. **Token Types**: Common token types include `Bearer`, `Basic`, and `JWT`
4. **Security**: Be careful with sensitive credentials in test files; consider using environment variables

## Troubleshooting

- If authentication fails, check the token path expression and API response format
- Ensure your API endpoints are correctly defined
- Verify network connectivity to your API server

## Additional Features

- You can extract and store multiple variables from different API responses
- Stored variables persist for the entire test run
- Variables can be used in both API requests and database queries
