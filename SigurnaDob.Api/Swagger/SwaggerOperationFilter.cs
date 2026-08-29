using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SigurnaDob.Api.Swagger;

public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any())
        {
            operation.Security = [];
        }

        if (context.MethodInfo.Name != "Login")
            return;

        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var requestJson) == true)
        {
            requestJson.Example = JsonNode.Parse(
                """
                {
                  "email": "admin@sigurna-dob.local",
                  "password": "Admin123!"
                }
                """);
        }

        if (operation.Responses?.TryGetValue("200", out var okResponse) == true &&
            okResponse.Content?.TryGetValue("application/json", out var responseJson) == true)
        {
            responseJson.Example = JsonNode.Parse(
                """
                {
                  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.example",
                  "expiresAtUtc": "2026-08-29T12:00:00Z",
                  "user": {
                    "id": 1,
                    "email": "admin@sigurna-dob.local",
                    "displayName": "Admin korisnik",
                    "roles": ["User", "Admin"],
                    "employeeId": null,
                    "familyContactId": null
                  }
                }
                """);
        }
    }
}
