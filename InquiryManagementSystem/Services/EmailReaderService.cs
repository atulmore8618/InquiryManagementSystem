using InquiryManagementSystem.Data;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace InquiryManagementSystem.Services
{
    public class EmailReaderService : IEmailReaderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public EmailReaderService(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<int> CheckEmailsAsync()
        {
            var imapHost = _configuration["EmailSettings:ImapHost"];
            var imapPort = int.Parse(_configuration["EmailSettings:ImapPort"] ?? "993");
            var email = _configuration["EmailSettings:SenderEmail"];
            var password = _configuration["EmailSettings:Password"];

            var newInquiries = 0;

            using var client = new ImapClient();

            await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(email, password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var unseenUids = await inbox.SearchAsync(SearchQuery.NotSeen);

            foreach (var uid in unseenUids)
            {
                var message = await inbox.GetMessageAsync(uid);

                var messageId = message.MessageId ?? $"UID-{uid}";

                var alreadyExists = _context.Inquiries
                    .Any(x => x.EmailMessageId == messageId);

                if (!alreadyExists)
                {
                    var sender = message.From.Mailboxes.FirstOrDefault();

                    var senderEmailAddress = sender?.Address ?? "unknown@unknown.com";

                    var senderName = string.IsNullOrWhiteSpace(sender?.Name)
                        ? senderEmailAddress
                        : sender!.Name;

                    var subject = string.IsNullOrWhiteSpace(message.Subject)
                        ? "(No Subject)"
                        : message.Subject;

                    var body = message.TextBody
                        ?? message.HtmlBody
                        ?? "";

                    var inquiry = new Models.Inquiry
                    {
                        EmailMessageId = messageId,
                        CustomerName = senderName,
                        CustomerEmail = senderEmailAddress,
                        Subject = subject,
                        VehicleDetails = subject,
                        Message = body,
                        ReceivedAt = message.Date.LocalDateTime,
                        CreatedAt = DateTime.Now,
                        Status = "New"
                    };

                    _context.Inquiries.Add(inquiry);
                    _context.SaveChanges();

                    newInquiries++;
                }

                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
            }

            await client.DisconnectAsync(true);

            return newInquiries;
        }
    }
}