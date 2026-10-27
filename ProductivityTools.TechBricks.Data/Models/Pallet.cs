using System.Text.Json.Serialization;

namespace ProductivityTools.TechBricks.Data.Models
{
    public class Pallet
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("owners")]
        public List<string> Owners { get; set; } = new();

        [JsonPropertyName("bricks")]
        public List<Brick> Bricks { get; set; } = new();

        public override string ToString() => Name;
    }

    public class Brick
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        public override string ToString() => $"{Key}: {Value}";
    }
}
