namespace DCElectricWebAPI.Models
{
    public class OpenProjectSettings
    {
        public string baseUrl { get; set; } = string.Empty;
        public string apiKey { get; set; } = string.Empty;
        public int projectId { get; set; }
        // Safety cap on how many tickets a single request will pull from OpenProject.
        public int maxTickets { get; set; } = 500;
    }

    public class SupportTicket
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CommentCount { get; set; }
        public int? ParentId { get; set; }
        public string ParentSubject { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
