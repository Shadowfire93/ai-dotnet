namespace Common.Models
{
    public class RagRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public List<string> History { get; set; } = new();
        public RagSettings Settings { get; set; } = new();
    }
}
