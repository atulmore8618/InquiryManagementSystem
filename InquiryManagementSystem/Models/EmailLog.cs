using System.ComponentModel.DataAnnotations;

namespace InquiryManagementSystem.Models
{
    public class EmailLog
    {
        public int EmailLogId { get; set; }

        public int InquiryId { get; set; }

        [Required]
        public string RecipientEmail { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string EmailType { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}