using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SigurnaDob.Api.Swagger;

public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        KeepJsonContentOnly(operation);

        var allowAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (allowAnonymous)
            operation.Security = [];
    }

    private static void KeepJsonContentOnly(OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content is { Count: > 0 } requestContent &&
            requestContent.TryGetValue("application/json", out var requestJson))
        {
            requestContent.Clear();
            requestContent["application/json"] = requestJson;
        }

        if (operation.Responses is null)
            return;

        foreach (var response in operation.Responses.Values)
        {
            if (response.Content is not { Count: > 0 } responseContent)
                continue;

            if (!responseContent.TryGetValue("application/json", out var responseJson))
                continue;

            responseContent.Clear();
            responseContent["application/json"] = responseJson;
        }
    }
}
