namespace IP.Chatbot.Models
{
    public class ChatMessage
    {
        public string UserType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public List<RelavantSource> RelavantSources { get; set; } = new();
        public string CreatedDate { get; set; } = string.Empty;
    }
}
