using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>A JSON schema the agent's answer must follow.</summary>
/// <param name="Name">A short name for the format, e.g. "ofsted_findings" (letters, digits, '_' and '-').</param>
/// <param name="Description">What the output is, to help the model.</param>
public sealed record AgentOutputSchema(string Name, string JsonSchema, string? Description = null)
{
    // The schema exporter needs a type resolver; the same options read answers back, so names match.
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
    };

    /// <summary>A schema for <typeparamref name="T"/>: every property required, no others allowed.</summary>
    public static AgentOutputSchema For<T>(string name, string? description = null)
    {
        var schema = WebOptions.GetJsonSchemaAsNode(typeof(T), new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (_, node) =>
            {
                if (node is JsonObject obj && obj["properties"] is JsonObject properties)
                {
                    obj["additionalProperties"] = false;
                    obj["required"] = new JsonArray([.. properties.Select(property => (JsonNode)property.Key)]);
                }

                return node;
            },
        });

        return new AgentOutputSchema(name, schema.ToJsonString(), description);
    }

    internal static JsonSerializerOptions SerializerOptions => WebOptions;
}
