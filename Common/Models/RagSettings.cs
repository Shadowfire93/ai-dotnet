namespace Common.Models
{
    public class RagSettings
    {
        public string Model { get; set; } = "bge-large";
        public string EmbeddingsModel { get; set; } = "bge-large";
        public string GroundingPrompt { get; set; } = string.Empty;
        // Add more settings as needed
    }
}
