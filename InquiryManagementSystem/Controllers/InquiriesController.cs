using InquiryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using InquiryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;

namespace InquiryManagementSystem.Controllers
{
    [Authorize]
    public class InquiriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public InquiriesController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Index(string? search, string? status)
        {
            var query = _context.Inquiries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.CustomerName.Contains(search) ||
                    x.CustomerEmail.Contains(search) ||
                    x.Subject.Contains(search) ||
                    x.VehicleDetails.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(x => x.Status == status);
            }

            var viewModel = new ViewModels.InquiryListViewModel
            {
                Inquiries = query
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList(),
                TotalCount = _context.Inquiries.Count(),
                PendingCount = _context.Inquiries.Count(x => x.Status != "Responded"),
                RespondedCount = _context.Inquiries.Count(x => x.Status == "Responded"),
                Search = search,
                Status = status
            };

            return View(viewModel);
        }

        public IActionResult Details(int id)
        {
            var inquiry = _context.Inquiries
                .FirstOrDefault(x => x.InquiryId == id);

            if (inquiry == null)
            {
                return NotFound();
            }

            var viewModel = new ViewModels.InquiryDetailsViewModel
            {
                Inquiry = inquiry,
                RateHistory = _context.MarketRates
                    .Where(x => x.InquiryId == id)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList(),
                EmailHistory = _context.EmailLogs
                    .Where(x => x.InquiryId == id)
                    .OrderByDescending(x => x.SentAt)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRate(int inquiryId, decimal rate, string? remarks)
        {
            var inquiry = _context.Inquiries
                .FirstOrDefault(x => x.InquiryId == inquiryId);

            if (inquiry == null)
            {
                return NotFound();
            }

            if (rate <= 0)
            {
                TempData["Error"] = "Rate must be greater than zero.";
                return RedirectToAction("Details", new { id = inquiryId });
            }

            // ---- 1. SAVE THE RATE FIRST (data is safe before we try email) ----

            var marketRate = new Models.MarketRate
            {
                InquiryId = inquiryId,
                Rate = rate,
                Remarks = remarks,
                CreatedBy = "Admin",
                CreatedAt = DateTime.Now
            };

            _context.MarketRates.Add(marketRate);

            inquiry.Status = "RateSubmitted";
            inquiry.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            // ---- 2. NOW TRY TO EMAIL THE CUSTOMER ----

            var formattedRate = rate.ToString("N0",
                new System.Globalization.CultureInfo("en-IN"));

            var emailSubject = "Market Rate for Your Vehicle";

            var emailBody =
$@"Hello {inquiry.CustomerName},

Thank you for your inquiry regarding: {inquiry.Subject}

Based on the information provided, the current estimated
market rate for your vehicle is:

Rs. {formattedRate}

Regards,
Market Rate Team";

            var emailLog = new Models.EmailLog
            {
                InquiryId = inquiryId,
                RecipientEmail = inquiry.CustomerEmail,
                Subject = emailSubject,
                Body = emailBody,
                EmailType = "MarketRateResponse",
                SentAt = DateTime.Now
            };

            try
            {
                await _emailService.SendEmailAsync(
                    inquiry.CustomerEmail, emailSubject, emailBody);

                emailLog.Status = "Sent";
                inquiry.Status = "Responded";

                TempData["Success"] = "Market rate saved and email sent to customer.";
            }
            catch (Exception ex)
            {
                emailLog.Status = "Failed";
                emailLog.ErrorMessage = ex.Message;

                TempData["Warning"] = "Rate saved, but the email could not be sent. See Email History for the error.";
            }

            _context.EmailLogs.Add(emailLog);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = inquiryId });
        }

    }
}