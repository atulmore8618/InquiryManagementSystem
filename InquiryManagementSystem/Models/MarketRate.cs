using System.ComponentModel.DataAnnotations;

namespace InquiryManagementSystem.Models
{
    public class MarketRate
    {
        public int MarketRateId { get; set; }

        public int InquiryId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Rate { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = "Admin";
    }
}