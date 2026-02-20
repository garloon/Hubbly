using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hubbly.Domain.Entities;

/// <summary>
/// Configuration of user's 3D avatar
/// </summary>
public class AvatarConfig
{
    /// <summary>
    /// Avatar gender: "male" or "female"
    /// </summary>
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = "male";

    /// <summary>
    /// ID of base 3D model (e.g., "male_base" or "female_base")
    /// </summary>
    [JsonPropertyName("baseModelId")]
    public string BaseModelId { get; set; } = "male_base";

    /// <summary>
    /// Avatar pose. For MVP only "standing"
    /// </summary>
    [JsonPropertyName("pose")]
    public string Pose { get; set; } = "standing";

    /// <summary>
    /// Additional components (clothing, accessories, etc.)
    /// Key: component type (e.g., "hair", "shirt")
    /// Value: component ID
    /// </summary>
    [JsonPropertyName("components")]
    public Dictionary<string, string> Components { get; set; } = new();

    // Static methods for creating default configs

    /// <summary>
    /// Default male avatar
    /// </summary>
    public static AvatarConfig DefaultMale => new AvatarConfig
    {
        Gender = "male",
        BaseModelId = "male_base",
        Pose = "standing",
        Components = new Dictionary<string, string>()
    };

    /// <summary>
    /// Default female avatar
    /// </summary>
    public static AvatarConfig DefaultFemale => new AvatarConfig
    {
        Gender = "female",
        BaseModelId = "female_base",
        Pose = "standing",
        Components = new Dictionary<string, string>()
    };

    /// <summary>
    /// Creates default config based on gender
    /// </summary>
    public static AvatarConfig DefaultForGender(string gender) =>
        gender?.ToLower() == "female" ? DefaultFemale : DefaultMale;

    // Serialization/Deserialization

    /// <summary>
    /// Converts config to JSON string
    /// </summary>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Creates config from JSON string
    /// </summary>
    public static AvatarConfig FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return DefaultMale;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<AvatarConfig>(json, options);
            return config ?? DefaultMale;
        }
        catch (JsonException)
        {
            // If JSON is corrupted, return default
            return DefaultMale;
        }
    }

    /// <summary>
    /// Validates config
    /// </summary>
    public bool IsValid()
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(Gender)) return false;
        if (string.IsNullOrWhiteSpace(BaseModelId)) return false;
        if (string.IsNullOrWhiteSpace(Pose)) return false;

        // Check valid values
        var validGenders = new[] { "male", "female" };
        var validPoses = new[] { "standing", "sitting", "lean", "handsonhips", "armscrossed" };

        if (!validGenders.Contains(Gender.ToLower())) return false;
        if (!validPoses.Contains(Pose.ToLower())) return false;

        // Base model must match gender
        if (Gender == "male" && !BaseModelId.Contains("male", StringComparison.OrdinalIgnoreCase))
            return false;
        if (Gender == "female" && !BaseModelId.Contains("female", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    /// <summary>
    /// Returns path to 3D model based on config
    /// </summary>
    public string GetModelPath()
    {
        // Form model path
        // In MVP: "assets/avatars/{BaseModelId}.glb"
        // In future: URL to CDN
        return $"assets/avatars/{BaseModelId}.glb";
    }

    /// <summary>
    /// Returns emoji for preview
    /// </summary>
    public string GetPreviewEmoji() =>
        Gender.ToLower() == "female" ? "👩" : "👨";
}
