using InquiryManagementSystem.Models;

namespace InquiryManagementSystem.ViewModels
{
    public class InquiryDetailsViewModel
    {
        public Inquiry Inquiry { get; set; } = null!;

        public List<MarketRate> RateHistory { get; set; } = new();

        public List<EmailLog> EmailHistory { get; set; } = new();
    }
}