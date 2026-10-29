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

        /// <summary>Title of the brick (field "description" used by the web application).</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Content of the brick (field "brick" used by the web application).</summary>
        [JsonPropertyName("brick")]
        public string? Content { get; set; }

        /// <summary>Legacy title field, replaced by "description" when the brick is edited in the web application.</summary>
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        /// <summary>Legacy content field, replaced by "brick" when the brick is edited in the web application.</summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonIgnore]
        public string Title => !string.IsNullOrEmpty(Description) ? Description : Key ?? string.Empty;

        [JsonIgnore]
        public string Text => !string.IsNullOrEmpty(Content) ? Content : Value ?? string.Empty;

        public override string ToString() => $"{Title}: {Text}";
    }
}
