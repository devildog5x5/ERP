using System;
using System.Linq;
using System.Web.Http;
using Ledgerly.Server.Data;
using Ledgerly.Server.Services;
using Ledgerly.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Controllers;

[RoutePrefix("api/crm")]
public class CrmController : ApiController
{
    private AppUser? UserEntity =>
        Request.Properties.TryGetValue("LedgerlyUser", out var u) ? u as AppUser : RequestAuth.GetUser(Request);

    // ---------- Leads ----------
    [HttpGet, Route("leads"), RequirePermission("crm")]
    public IHttpActionResult ListLeads()
    {
        using var db = Db.Create();
        return Ok(db.CrmLeads.OrderByDescending(x => x.CreatedAt).AsEnumerable().Select(ToLeadDto).ToList());
    }

    [HttpPost, Route("leads"), RequirePermission("crm")]
    public IHttpActionResult CreateLead([FromBody] CrmLeadDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
        using var db = Db.Create();
        var e = new CrmLead
        {
            Name = dto.Name.Trim(),
            CompanyName = Null(dto.CompanyName),
            Email = Null(dto.Email),
            Phone = Null(dto.Phone),
            Source = Null(dto.Source),
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "new" : dto.Status.Trim().ToLowerInvariant(),
            OwnerUserId = dto.OwnerUserId ?? UserEntity?.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.CrmLeads.Add(e);
        db.SaveChanges();
        return Ok(ToLeadDto(e));
    }

    [HttpPut, Route("leads/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult UpdateLead(int id, [FromBody] CrmLeadDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
        using var db = Db.Create();
        var e = db.CrmLeads.Find(id);
        if (e is null) return NotFound();
        e.Name = dto.Name.Trim();
        e.CompanyName = Null(dto.CompanyName);
        e.Email = Null(dto.Email);
        e.Phone = Null(dto.Phone);
        e.Source = Null(dto.Source);
        if (!string.IsNullOrWhiteSpace(dto.Status)) e.Status = dto.Status.Trim().ToLowerInvariant();
        e.OwnerUserId = dto.OwnerUserId;
        db.SaveChanges();
        return Ok(ToLeadDto(e));
    }

    [HttpDelete, Route("leads/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult DeleteLead(int id)
    {
        using var db = Db.Create();
        var e = db.CrmLeads.Find(id);
        if (e is null) return NotFound();
        db.CrmLeads.Remove(e);
        db.SaveChanges();
        return Ok();
    }

    [HttpPost, Route("leads/{id:int}/convert"), RequirePermission("crm")]
    public IHttpActionResult ConvertLead(int id, [FromBody] CrmLeadConvertDto? dto)
    {
        using var db = Db.Create();
        var lead = db.CrmLeads.Find(id);
        if (lead is null) return NotFound();
        if (lead.Status == "converted") return BadRequest("Lead already converted");

        var account = new CrmAccount
        {
            Name = string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.Name : lead.CompanyName!,
            BillingEmail = lead.Email,
            OwnerUserId = lead.OwnerUserId ?? UserEntity?.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.CrmAccounts.Add(account);
        db.SaveChanges();

        db.CrmContacts.Add(new CrmContact
        {
            AccountId = account.Id,
            LeadId = lead.Id,
            FirstName = lead.Name,
            Email = lead.Email,
            Phone = lead.Phone,
            IsPrimary = true,
            IsActive = true
        });

        if (dto?.CreateCustomer != false)
        {
            var customer = new Customer
            {
                Name = account.Name,
                Email = lead.Email,
                Phone = lead.Phone,
                IsActive = true
            };
            db.Customers.Add(customer);
            db.SaveChanges();
            account.CustomerId = customer.Id;
            lead.ConvertedCustomerId = customer.Id;
        }

        lead.Status = "converted";
        lead.ConvertedAccountId = account.Id;
        db.SaveChanges();
        return Ok(new { lead = ToLeadDto(lead), account = ToAccountDto(account) });
    }

    // ---------- Accounts ----------
    [HttpGet, Route("accounts"), RequirePermission("crm")]
    public IHttpActionResult ListAccounts()
    {
        using var db = Db.Create();
        var rows = db.CrmAccounts.Include(a => a.Customer).Where(a => a.IsActive)
            .OrderBy(a => a.Name).AsEnumerable().Select(ToAccountDto).ToList();
        return Ok(rows);
    }

    [HttpPost, Route("accounts"), RequirePermission("crm")]
    public IHttpActionResult CreateAccount([FromBody] CrmAccountDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
        using var db = Db.Create();
        var e = new CrmAccount
        {
            Name = dto.Name.Trim(),
            CustomerId = dto.CustomerId,
            Industry = Null(dto.Industry),
            Website = Null(dto.Website),
            BillingEmail = Null(dto.BillingEmail),
            OwnerUserId = dto.OwnerUserId ?? UserEntity?.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.CrmAccounts.Add(e);
        db.SaveChanges();
        return Ok(ToAccountDto(e));
    }

    [HttpPut, Route("accounts/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult UpdateAccount(int id, [FromBody] CrmAccountDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
        using var db = Db.Create();
        var e = db.CrmAccounts.Find(id);
        if (e is null) return NotFound();
        e.Name = dto.Name.Trim();
        e.CustomerId = dto.CustomerId;
        e.Industry = Null(dto.Industry);
        e.Website = Null(dto.Website);
        e.BillingEmail = Null(dto.BillingEmail);
        e.OwnerUserId = dto.OwnerUserId;
        e.IsActive = dto.IsActive;
        db.SaveChanges();
        if (e.CustomerId != null)
            e.Customer = db.Customers.Find(e.CustomerId);
        return Ok(ToAccountDto(e));
    }

    [HttpPost, Route("accounts/{id:int}/link-customer"), RequirePermission("crm")]
    public IHttpActionResult LinkOrCreateCustomer(int id)
    {
        using var db = Db.Create();
        var e = db.CrmAccounts.Find(id);
        if (e is null) return NotFound();
        if (e.CustomerId != null) return Ok(ToAccountDto(e));

        var customer = new Customer { Name = e.Name, Email = e.BillingEmail, IsActive = true };
        db.Customers.Add(customer);
        db.SaveChanges();
        e.CustomerId = customer.Id;
        db.SaveChanges();
        e.Customer = customer;
        return Ok(ToAccountDto(e));
    }

    [HttpDelete, Route("accounts/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult DeleteAccount(int id)
    {
        using var db = Db.Create();
        var e = db.CrmAccounts.Find(id);
        if (e is null) return NotFound();
        e.IsActive = false;
        db.SaveChanges();
        return Ok();
    }

    // ---------- Contacts ----------
    [HttpGet, Route("contacts"), RequirePermission("crm")]
    public IHttpActionResult ListContacts([FromUri] int? accountId = null)
    {
        using var db = Db.Create();
        var q = db.CrmContacts.Include(c => c.Account).Where(c => c.IsActive);
        if (accountId != null) q = q.Where(c => c.AccountId == accountId);
        return Ok(q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).AsEnumerable().Select(ToContactDto).ToList());
    }

    [HttpPost, Route("contacts"), RequirePermission("crm")]
    public IHttpActionResult CreateContact([FromBody] CrmContactDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.FirstName)) return BadRequest("First name required");
        using var db = Db.Create();
        var e = new CrmContact
        {
            AccountId = dto.AccountId,
            LeadId = dto.LeadId,
            FirstName = dto.FirstName.Trim(),
            LastName = Null(dto.LastName),
            Email = Null(dto.Email),
            Phone = Null(dto.Phone),
            Title = Null(dto.Title),
            IsPrimary = dto.IsPrimary,
            IsActive = true
        };
        db.CrmContacts.Add(e);
        db.SaveChanges();
        if (e.AccountId != null) e.Account = db.CrmAccounts.Find(e.AccountId);
        return Ok(ToContactDto(e));
    }

    [HttpPut, Route("contacts/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult UpdateContact(int id, [FromBody] CrmContactDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.FirstName)) return BadRequest("First name required");
        using var db = Db.Create();
        var e = db.CrmContacts.Find(id);
        if (e is null) return NotFound();
        e.AccountId = dto.AccountId;
        e.LeadId = dto.LeadId;
        e.FirstName = dto.FirstName.Trim();
        e.LastName = Null(dto.LastName);
        e.Email = Null(dto.Email);
        e.Phone = Null(dto.Phone);
        e.Title = Null(dto.Title);
        e.IsPrimary = dto.IsPrimary;
        e.IsActive = dto.IsActive;
        db.SaveChanges();
        if (e.AccountId != null) e.Account = db.CrmAccounts.Find(e.AccountId);
        return Ok(ToContactDto(e));
    }

    [HttpDelete, Route("contacts/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult DeleteContact(int id)
    {
        using var db = Db.Create();
        var e = db.CrmContacts.Find(id);
        if (e is null) return NotFound();
        e.IsActive = false;
        db.SaveChanges();
        return Ok();
    }

    // ---------- Opportunities / pipeline ----------
    [HttpGet, Route("opportunities"), RequirePermission("crm")]
    public IHttpActionResult ListOpportunities([FromUri] string? stage = null)
    {
        using var db = Db.Create();
        var q = db.CrmOpportunities.Include(o => o.Account).AsQueryable();
        if (!string.IsNullOrWhiteSpace(stage))
            q = q.Where(o => o.Stage == stage);
        return Ok(q.OrderByDescending(o => o.CreatedAt).AsEnumerable().Select(ToOppDto).ToList());
    }

    [HttpPost, Route("opportunities"), RequirePermission("crm")]
    public IHttpActionResult CreateOpportunity([FromBody] CrmOpportunityDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.AccountId <= 0)
            return BadRequest("Name and account required");
        using var db = Db.Create();
        if (db.CrmAccounts.Find(dto.AccountId) is null) return BadRequest("Account not found");
        var e = new CrmOpportunity
        {
            AccountId = dto.AccountId,
            PrimaryContactId = dto.PrimaryContactId,
            Name = dto.Name.Trim(),
            Stage = string.IsNullOrWhiteSpace(dto.Stage) ? "prospecting" : dto.Stage.Trim().ToLowerInvariant(),
            Amount = dto.Amount,
            ExpectedClose = dto.ExpectedClose,
            OwnerUserId = dto.OwnerUserId ?? UserEntity?.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.CrmOpportunities.Add(e);
        db.SaveChanges();
        e.Account = db.CrmAccounts.Find(e.AccountId)!;
        return Ok(ToOppDto(e));
    }

    [HttpPut, Route("opportunities/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult UpdateOpportunity(int id, [FromBody] CrmOpportunityDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
        using var db = Db.Create();
        var e = db.CrmOpportunities.Include(o => o.Account).FirstOrDefault(o => o.Id == id);
        if (e is null) return NotFound();
        e.AccountId = dto.AccountId > 0 ? dto.AccountId : e.AccountId;
        e.PrimaryContactId = dto.PrimaryContactId;
        e.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Stage)) e.Stage = dto.Stage.Trim().ToLowerInvariant();
        e.Amount = dto.Amount;
        e.ExpectedClose = dto.ExpectedClose;
        e.OwnerUserId = dto.OwnerUserId;
        e.LostReason = Null(dto.LostReason);
        db.SaveChanges();
        return Ok(ToOppDto(e));
    }

    [HttpPost, Route("opportunities/{id:int}/win"), RequirePermission("crm")]
    public IHttpActionResult WinOpportunity(int id, [FromBody] CrmOpportunityWinDto? dto)
    {
        using var db = Db.Create();
        var e = db.CrmOpportunities.Include(o => o.Account).FirstOrDefault(o => o.Id == id);
        if (e is null) return NotFound();
        if (e.Stage == "won" && e.SalesOrderId != null) return Ok(ToOppDto(e));

        var account = e.Account;
        if (account.CustomerId == null)
        {
            var customer = new Customer { Name = account.Name, Email = account.BillingEmail, IsActive = true };
            db.Customers.Add(customer);
            db.SaveChanges();
            account.CustomerId = customer.Id;
        }

        var docType = (dto?.DocumentType ?? "quote").Trim().ToLowerInvariant();
        if (docType != "quote" && docType != "order") docType = "quote";
        var seqType = docType == "quote" ? "QT" : "SO";
        var prefix = docType == "quote" ? "QT-" : "SO-";
        var so = new SalesOrder
        {
            OrderNumber = DocumentNumbers.Next(db, seqType, prefix),
            CustomerId = account.CustomerId!.Value,
            DocumentType = docType,
            Status = docType == "quote" ? "draft" : "confirmed",
            OrderDate = DateTime.Today,
            Notes = $"Created from CRM opportunity #{e.Id}: {e.Name}",
            Total = e.Amount ?? 0,
            Subtotal = e.Amount ?? 0
        };
        db.SalesOrders.Add(so);
        db.SaveChanges();
        e.Stage = "won";
        e.SalesOrderId = so.Id;
        db.SaveChanges();
        return Ok(ToOppDto(e));
    }

    [HttpDelete, Route("opportunities/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult DeleteOpportunity(int id)
    {
        using var db = Db.Create();
        var e = db.CrmOpportunities.Find(id);
        if (e is null) return NotFound();
        db.CrmOpportunities.Remove(e);
        db.SaveChanges();
        return Ok();
    }

    // ---------- Activities ----------
    [HttpGet, Route("activities"), RequirePermission("crm")]
    public IHttpActionResult ListActivities([FromUri] string? status = null)
    {
        using var db = Db.Create();
        var q = db.CrmActivities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);
        return Ok(q.OrderBy(a => a.DueAt).ThenByDescending(a => a.Id).AsEnumerable().Select(ToActivityDto).ToList());
    }

    [HttpPost, Route("activities"), RequirePermission("crm")]
    public IHttpActionResult CreateActivity([FromBody] CrmActivityDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Subject)) return BadRequest("Subject required");
        using var db = Db.Create();
        var e = new CrmActivity
        {
            ActivityType = string.IsNullOrWhiteSpace(dto.ActivityType) ? "task" : dto.ActivityType.Trim().ToLowerInvariant(),
            Subject = dto.Subject.Trim(),
            Body = Null(dto.Body),
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "open" : dto.Status.Trim().ToLowerInvariant(),
            DueAt = dto.DueAt,
            OwnerUserId = dto.OwnerUserId ?? UserEntity?.Id,
            AccountId = dto.AccountId,
            ContactId = dto.ContactId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId
        };
        db.CrmActivities.Add(e);
        db.SaveChanges();
        return Ok(ToActivityDto(e));
    }

    [HttpPut, Route("activities/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult UpdateActivity(int id, [FromBody] CrmActivityDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Subject)) return BadRequest("Subject required");
        using var db = Db.Create();
        var e = db.CrmActivities.Find(id);
        if (e is null) return NotFound();
        e.ActivityType = string.IsNullOrWhiteSpace(dto.ActivityType) ? e.ActivityType : dto.ActivityType.Trim().ToLowerInvariant();
        e.Subject = dto.Subject.Trim();
        e.Body = Null(dto.Body);
        e.Status = string.IsNullOrWhiteSpace(dto.Status) ? e.Status : dto.Status.Trim().ToLowerInvariant();
        e.DueAt = dto.DueAt;
        e.OwnerUserId = dto.OwnerUserId;
        e.AccountId = dto.AccountId;
        e.ContactId = dto.ContactId;
        e.LeadId = dto.LeadId;
        e.OpportunityId = dto.OpportunityId;
        if (e.Status == "done" && e.CompletedAt == null) e.CompletedAt = DateTime.UtcNow;
        if (e.Status != "done") e.CompletedAt = null;
        db.SaveChanges();
        return Ok(ToActivityDto(e));
    }

    [HttpDelete, Route("activities/{id:int}"), RequirePermission("crm")]
    public IHttpActionResult DeleteActivity(int id)
    {
        using var db = Db.Create();
        var e = db.CrmActivities.Find(id);
        if (e is null) return NotFound();
        db.CrmActivities.Remove(e);
        db.SaveChanges();
        return Ok();
    }

    // ---------- Notes & communications ----------
    [HttpGet, Route("notes"), RequirePermission("crm")]
    public IHttpActionResult ListNotes([FromUri] int? accountId = null, [FromUri] int? leadId = null, [FromUri] int? opportunityId = null)
    {
        using var db = Db.Create();
        var q = db.CrmNotes.AsQueryable();
        if (accountId != null) q = q.Where(n => n.AccountId == accountId);
        if (leadId != null) q = q.Where(n => n.LeadId == leadId);
        if (opportunityId != null) q = q.Where(n => n.OpportunityId == opportunityId);
        return Ok(q.OrderByDescending(n => n.CreatedAt).AsEnumerable().Select(ToNoteDto).ToList());
    }

    [HttpPost, Route("notes"), RequirePermission("crm")]
    public IHttpActionResult CreateNote([FromBody] CrmNoteDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Body)) return BadRequest("Body required");
        using var db = Db.Create();
        var e = new CrmNote
        {
            Body = dto.Body.Trim(),
            AuthorUserId = UserEntity?.Id,
            CreatedAt = DateTime.UtcNow,
            AccountId = dto.AccountId,
            ContactId = dto.ContactId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId
        };
        db.CrmNotes.Add(e);
        db.SaveChanges();
        return Ok(ToNoteDto(e));
    }

    [HttpGet, Route("communications"), RequirePermission("crm")]
    public IHttpActionResult ListCommunications([FromUri] int? accountId = null)
    {
        using var db = Db.Create();
        var q = db.CrmCommunications.AsQueryable();
        if (accountId != null) q = q.Where(c => c.AccountId == accountId);
        return Ok(q.OrderByDescending(c => c.OccurredAt).AsEnumerable().Select(ToCommDto).ToList());
    }

    [HttpPost, Route("communications"), RequirePermission("crm")]
    public IHttpActionResult CreateCommunication([FromBody] CrmCommunicationDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Summary)) return BadRequest("Summary required");
        using var db = Db.Create();
        var e = new CrmCommunicationLog
        {
            Channel = string.IsNullOrWhiteSpace(dto.Channel) ? "other" : dto.Channel.Trim().ToLowerInvariant(),
            Direction = string.IsNullOrWhiteSpace(dto.Direction) ? "outbound" : dto.Direction.Trim().ToLowerInvariant(),
            Subject = Null(dto.Subject),
            Summary = dto.Summary.Trim(),
            OccurredAt = dto.OccurredAt == default ? DateTime.UtcNow : dto.OccurredAt,
            UserId = UserEntity?.Id,
            AccountId = dto.AccountId,
            ContactId = dto.ContactId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId
        };
        db.CrmCommunications.Add(e);
        db.SaveChanges();
        return Ok(ToCommDto(e));
    }

    private static string? Null(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static CrmLeadDto ToLeadDto(CrmLead e) => new()
    {
        Id = e.Id, Name = e.Name, CompanyName = e.CompanyName, Email = e.Email, Phone = e.Phone,
        Source = e.Source, Status = e.Status, OwnerUserId = e.OwnerUserId, CreatedAt = e.CreatedAt,
        ConvertedAccountId = e.ConvertedAccountId, ConvertedCustomerId = e.ConvertedCustomerId
    };

    private static CrmAccountDto ToAccountDto(CrmAccount e) => new()
    {
        Id = e.Id, Name = e.Name, CustomerId = e.CustomerId, CustomerName = e.Customer?.Name,
        Industry = e.Industry, Website = e.Website, BillingEmail = e.BillingEmail,
        IsActive = e.IsActive, OwnerUserId = e.OwnerUserId, CreatedAt = e.CreatedAt
    };

    private static CrmContactDto ToContactDto(CrmContact e) => new()
    {
        Id = e.Id, AccountId = e.AccountId, AccountName = e.Account?.Name, LeadId = e.LeadId,
        FirstName = e.FirstName, LastName = e.LastName, Email = e.Email, Phone = e.Phone,
        Title = e.Title, IsPrimary = e.IsPrimary, IsActive = e.IsActive
    };

    private static CrmOpportunityDto ToOppDto(CrmOpportunity e) => new()
    {
        Id = e.Id, AccountId = e.AccountId, AccountName = e.Account?.Name, PrimaryContactId = e.PrimaryContactId,
        Name = e.Name, Stage = e.Stage, Amount = e.Amount, ExpectedClose = e.ExpectedClose,
        OwnerUserId = e.OwnerUserId, SalesOrderId = e.SalesOrderId, LostReason = e.LostReason, CreatedAt = e.CreatedAt
    };

    private static CrmActivityDto ToActivityDto(CrmActivity e) => new()
    {
        Id = e.Id, ActivityType = e.ActivityType, Subject = e.Subject, Body = e.Body, Status = e.Status,
        DueAt = e.DueAt, CompletedAt = e.CompletedAt, OwnerUserId = e.OwnerUserId,
        AccountId = e.AccountId, ContactId = e.ContactId, LeadId = e.LeadId, OpportunityId = e.OpportunityId
    };

    private static CrmNoteDto ToNoteDto(CrmNote e) => new()
    {
        Id = e.Id, Body = e.Body, AuthorUserId = e.AuthorUserId, CreatedAt = e.CreatedAt,
        AccountId = e.AccountId, ContactId = e.ContactId, LeadId = e.LeadId, OpportunityId = e.OpportunityId
    };

    private static CrmCommunicationDto ToCommDto(CrmCommunicationLog e) => new()
    {
        Id = e.Id, Channel = e.Channel, Direction = e.Direction, Subject = e.Subject, Summary = e.Summary,
        OccurredAt = e.OccurredAt, UserId = e.UserId, AccountId = e.AccountId, ContactId = e.ContactId,
        LeadId = e.LeadId, OpportunityId = e.OpportunityId
    };
}
