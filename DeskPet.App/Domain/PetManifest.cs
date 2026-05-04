using System.Text.Json.Serialization;

namespace DeskPet.App.Domain;

public sealed class PetManifest
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; init; }

    [JsonPropertyName("petId")]
    public string PetId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("species")]
    public string Species { get; init; } = string.Empty;

    [JsonPropertyName("style")]
    public string Style { get; init; } = string.Empty;

    [JsonPropertyName("canvas")]
    public PetCanvas Canvas { get; init; } = new();

    [JsonPropertyName("animations")]
    public Dictionary<string, AnimationDefinition> Animations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class PetCanvas
{
    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("scale")]
    public double Scale { get; init; } = 2;
}

public sealed class AnimationDefinition
{
    [JsonPropertyName("file")]
    public string File { get; init; } = string.Empty;

    [JsonPropertyName("frameWidth")]
    public int FrameWidth { get; init; }

    [JsonPropertyName("frameHeight")]
    public int FrameHeight { get; init; }

    [JsonPropertyName("frames")]
    public int Frames { get; init; }

    [JsonPropertyName("fps")]
    public int Fps { get; init; }

    [JsonPropertyName("loop")]
    public bool Loop { get; init; }
}
