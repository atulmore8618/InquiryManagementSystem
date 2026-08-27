# Automated Inquiry Management System

An ASP.NET Core MVC application that automatically captures vehicle market-rate inquiries from an email inbox, lets the team manage them on a dashboard, and emails the market rate back to the customer — with complete rate and email history.

Built as a technical assessment, focused on a clean, working end-to-end MVP.

---

## Project Flow

```
Customer sends email to the company inbox
                │
                ▼
EmailPollingService (BackgroundService, runs every 60 seconds)
                │
                ▼
EmailReaderService connects to Gmail via IMAP
   • finds unread messages
   • checks Message-Id against DB  →  duplicate? skip
   • extracts sender name, email, subject, body
                │
                ▼
Inquiry saved in SQL Server  (Status: New)
                │
                ▼
Admin Dashboard  (stat cards, search, status filter)
                │
                ▼
Admin opens Inquiry Details → enters Market Rate + remarks
                │
                ▼
Rate saved to MarketRates  (Status: RateSubmitted)   ← saved FIRST
                │
                ▼
EmailService sends the rate to the customer via SMTP
   • success → EmailLog "Sent",  Status: Responded
   • failure → EmailLog "Failed" + error message, rate still safe
                │
                ▼
Rate History + Email History visible on the inquiry page
```

**Status lifecycle:** `New → RateSubmitted → Responded`

---

## Features

- **Automatic email capture** — Gmail IMAP polled every minute by a hosted BackgroundService; new mail becomes an inquiry with zero manual entry
- **Duplicate protection** — inquiries keyed on the email `Message-Id`; re-reading the same message never creates duplicates
- **Dashboard** — Total / Pending / Responded stat cards, keyword search (customer, email, subject, vehicle), status filter
- **Inquiry details** — customer, email, subject, vehicle details, full message, received time, status
- **Market rate with history** — every submission inserts a new row; previous rates are never overwritten
- **Automatic customer email** — market rate sent via SMTP, amount formatted in Indian style (₹28,50,000)
- **Email audit log** — every attempt recorded with Sent/Failed status and the captured SMTP error on failure
- **Save-first design** — the rate is committed *before* the email attempt; an SMTP outage can never lose business data, and the UI reports honestly ("Rate saved, email failed")
- **Admin authentication** — cookie-based login protecting all inquiry pages, with return-URL support and open-redirect protection
- **Responsive UI** — Bootstrap 5; cards and forms reflow on mobile, tables scroll horizontally

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core MVC (C#) |
| ORM / Database | Entity Framework Core + SQL Server (LocalDB) |
| Email receiving | MailKit — IMAP, port 993 (SSL) |
| Email sending | SmtpClient — SMTP, port 587 (STARTTLS) |
| Background processing | BackgroundService (IHostedService) |
| Authentication | Cookie authentication (claims-based) |
| Frontend | Razor Views + Bootstrap 5 |

---

## Database Design

```
Inquiries ──────< MarketRates      (one inquiry → many rates  = rate history)
        └───────< EmailLogs        (one inquiry → many emails = audit trail)
```

### Inquiries
| Column | Purpose |
|---|---|
| InquiryId (PK) | Identity |
| EmailMessageId | Unique email Message-Id — duplicate-protection key |
| CustomerName | Sender's display name |
| CustomerEmail | Sender's address (reply target) |
| Subject | Email subject |
| VehicleDetails | Vehicle info (subject for the MVP) |
| Message | Full email body |
| ReceivedAt | When the email arrived |
| Status | New / RateSubmitted / Responded |
| CreatedAt, UpdatedAt | Audit timestamps |

### MarketRates  *(insert-only — this is the rate history)*
| Column | Purpose |
|---|---|
| MarketRateId (PK) | Identity |
| InquiryId (FK) | Owning inquiry |
| Rate | decimal(18,2) |
| Remarks | Optional note per rate |
| CreatedBy | Who entered it |
| CreatedAt | When |

### EmailLogs  *(audit of every send attempt)*
| Column | Purpose |
|---|---|
| EmailLogId (PK) | Identity |
| InquiryId (FK) | Owning inquiry |
| RecipientEmail | Actual recipient at send time |
| Subject, Body | Exact content sent |
| EmailType | e.g. MarketRateResponse |
| SentAt | Attempt time |
| Status | Sent / Failed |
| ErrorMessage | SMTP error when failed (nullable) |

---

## Project Structure

```
InquiryManagementSystem
├── Controllers
│   ├── AccountController.cs        login / logout (cookie auth)
│   ├── HomeController.cs           root redirect to dashboard
│   └── InquiriesController.cs      dashboard, details, submit rate
├── Services
│   ├── IEmailService / EmailService            SMTP sending
│   └── IEmailReaderService / EmailReaderService  IMAP reading
├── BackgroundServices
│   └── EmailPollingService.cs      60-second polling loop (scoped per cycle)
├── Models
│   ├── Inquiry.cs / MarketRate.cs / EmailLog.cs
├── ViewModels
│   ├── InquiryListViewModel.cs / InquiryDetailsViewModel.cs
├── Data
│   └── ApplicationDbContext.cs  (+ EF Core migrations)
└── Views
    ├── Inquiries (Index, Details) / Account (Login) / Shared
```

---

## Setup

1. **Clone** the repository and open the solution in Visual Studio 2022.
2. **Gmail prerequisites** (the account used as inquiry inbox + sender):
   - 2-Step Verification enabled
   - An **App Password** created (Google Account → Security → App passwords)
   - IMAP enabled (Gmail → Settings → Forwarding and POP/IMAP)
3. **Create `appsettings.Development.json`** next to `appsettings.json` (this file is git-ignored — real credentials never enter the repo):

```json
   {
     "EmailSettings": {
       "SmtpHost": "smtp.gmail.com",
       "SmtpPort": "587",
       "ImapHost": "imap.gmail.com",
       "ImapPort": "993",
       "SenderEmail": "your-inbox@gmail.com",
       "SenderName": "Market Rate Team",
       "Password": "your-16-char-app-password"
     },
     "AdminCredentials": {
       "Username": "admin",
       "Password": "your-admin-password"
     }
   }
```
4. **Database:** Package Manager Console → `Update-Database`
5. **Run** (Ctrl+F5) — the root URL redirects to the admin login.
6. **Try the full loop:** send an email to the configured inbox → it appears on the dashboard within a minute → open it, submit a rate → the customer receives the email → both histories update.

---

## Design Notes & Trade-offs

- **IMAP + App Password over the Gmail API** — deliberate MVP choice; reading sits behind `IEmailReaderService`, so swapping to the Gmail API or Microsoft Graph is a one-class change.
- **Scoped services inside a singleton poller** — the BackgroundService creates a DI scope each polling cycle (`IServiceScopeFactory`), so every cycle gets a fresh short-lived `DbContext` instead of a captive long-lived one.
- **Two-phase save on rate submission** — rate committed first, email attempted second, outcome logged; a mail outage degrades gracefully instead of losing data.
- **Minimal auth by intent** — config-based admin credentials for the assessment; production would use ASP.NET Core Identity (hashed passwords, lockout, roles).
- **Vehicle details = email subject** in the MVP; a structured parser or LLM extraction is the natural extension point.

## Possible Extensions

- Retry button for failed emails (EmailLogs already stores everything needed)
- Dashboard pagination
- Structured vehicle parsing (make / model / year / fuel / km)
- Multiple admin users via ASP.NET Core Identity
