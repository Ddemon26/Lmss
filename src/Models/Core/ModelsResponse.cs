using System.Text.Json.Serialization;
namespace Lmss.Models.Core;

/// <summary>
///     Response from the /models endpoint.
/// </summary>
public record ModelsResponse(
    [property: JsonPropertyName( "data" )] List<ModelInfo> Data
);

public record ModelInfo(
    [property: JsonPropertyName( "id" )] string Id
);