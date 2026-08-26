using System.ComponentModel.DataAnnotations;

namespace InquiryManagementSystem.Models
{
    public class Inquiry
    {
        public int InquiryId { get; set; }

        [Required]
        public string EmailMessageId { get; set; } = string.Empty;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string VehicleDetails { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime ReceivedAt { get; set; }

        public string Status { get; set; } = "New";

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}