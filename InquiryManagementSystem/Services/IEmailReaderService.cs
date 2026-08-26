namespace InquiryManagementSystem.Services
{
    public interface IEmailReaderService
    {
        Task<int> CheckEmailsAsync();
    }
}