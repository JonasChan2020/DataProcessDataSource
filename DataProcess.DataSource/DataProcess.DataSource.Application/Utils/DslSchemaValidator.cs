using NJsonSchema;
using Newtonsoft.Json.Linq;

namespace DataProcess.DataSource.Application.Utils;

/// <summary>
/// DSL JSON Schema 校验工具
/// </summary>
public static class DslSchemaValidator
{
    private static readonly string DslSchemaJson = @"{
      ""type"": ""object"",
      ""properties"": {
        ""table"": { ""type"": ""string"" },
        ""select"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
        ""where"": { ""type"": ""object"" },
        ""orderBy"": { ""type"": ""array"" },
        ""limit"": { ""type"": ""integer"", ""minimum"": 0 },
        ""offset"": { ""type"": ""integer"", ""minimum"": 0 }
      },
      ""required"": [""table""]
    }";

    public static void Validate(string dslJson)
    {
        var schema = JsonSchema.FromJsonAsync(DslSchemaJson).Result;
        var errors = schema.Validate(JObject.Parse(dslJson));
        if (errors.Any())
            throw Oops.Bah("DSL参数结构非法: " + string.Join(";", errors.Select(e => e.ToString())));
    }
}