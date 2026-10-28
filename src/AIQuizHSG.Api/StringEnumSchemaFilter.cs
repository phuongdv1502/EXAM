using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AIQuizHSG.Api;

// Swagger hiện enum bằng tên chuỗi, khớp JsonStringEnumConverter của API.
public sealed class StringEnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        if (schema is not OpenApiSchema openApi || !type.IsEnum)
            return;

        openApi.Type = JsonSchemaType.String;
        openApi.Format = null;
        openApi.Enum = Enum.GetNames(type)
            .Select(name => (JsonNode)JsonValue.Create(name)!)
            .ToList();
    }
}
