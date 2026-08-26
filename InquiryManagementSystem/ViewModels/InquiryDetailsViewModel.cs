using InquiryManagementSystem.Models;

namespace InquiryManagementSystem.ViewModels
{
    public class InquiryListViewModel
    {
        public List<Inquiry> Inquiries { get; set; } = new();

        public int TotalCount { get; set; }

        public int PendingCount { get; set; }

        public int RespondedCount { get; set; }

        public string? Search { get; set; }

        public string? Status { get; set; }
    }
}