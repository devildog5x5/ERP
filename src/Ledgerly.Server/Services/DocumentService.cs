using System.Linq;
using System.Net.Mail;
using System.Text;
using Ledgerly.Server.Data;
using Ledgerly.Shared;

namespace Ledgerly.Server.Services;

public static class DocumentService
{
    public static DocumentHtmlDto BuildSalesDocument(ErpDbContext db, SalesOrder so)
    {
        var company = db.Settings.First();
        var title = so.DocumentType switch
        {
            "quote" => "Quote",
            "invoice" => "Invoice",
            _ => "Sales Order"
        };
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'><title>").Append(title).Append(' ').Append(so.OrderNumber)
            .Append("</title><style>body{font-family:Segoe UI,Arial;margin:32px;color:#15202B}table{border-collapse:collapse;width:100%;margin-top:16px}th,td{border-bottom:1px solid #E2E8EE;padding:8px;text-align:left}h1{margin:0}.muted{color:#667888}</style></head><body>");
        sb.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(company.CompanyName)).Append("</h1>");
        sb.Append("<div class='muted'>").Append(System.Net.WebUtility.HtmlEncode(company.Address ?? "")).Append("<br/>")
            .Append(System.Net.WebUtility.HtmlEncode(company.Phone ?? "")).Append(" ")
            .Append(System.Net.WebUtility.HtmlEncode(company.Email ?? "")).Append("</div>");
        sb.Append("<h2>").Append(title).Append(' ').Append(System.Net.WebUtility.HtmlEncode(so.OrderNumber)).Append("</h2>");
        sb.Append("<p>Customer: <b>").Append(System.Net.WebUtility.HtmlEncode(so.Customer?.Name ?? "")).Append("</b><br/>Date: ")
            .Append(so.OrderDate.ToString("yyyy-MM-dd")).Append("<br/>Status: ").Append(so.Status).Append("</p>");
        sb.Append("<table><tr><th>SKU</th><th>Item</th><th>Qty</th><th>Price</th><th>Line</th></tr>");
        foreach (var line in so.Lines)
        {
            var lineTotal = line.Quantity * line.UnitPrice * (1 - line.DiscountPercent / 100m);
            sb.Append("<tr><td>").Append(System.Net.WebUtility.HtmlEncode(line.Product?.Sku ?? ""))
                .Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(line.Product?.Name ?? ""))
                .Append("</td><td>").Append(line.Quantity)
                .Append("</td><td>").Append(line.UnitPrice.ToString("C"))
                .Append("</td><td>").Append(lineTotal.ToString("C")).Append("</td></tr>");
        }
        sb.Append("</table>");
        sb.Append("<p style='text-align:right'>Subtotal: ").Append(so.Subtotal.ToString("C"))
            .Append("<br/>Discount: ").Append(so.DiscountAmount.ToString("C"))
            .Append("<br/>Tax: ").Append(so.TaxAmount.ToString("C"))
            .Append("<br/><b>Total: ").Append(so.Total.ToString("C")).Append("</b></p>");
        if (!string.IsNullOrWhiteSpace(so.TrackingNumber))
            sb.Append("<p>Shipping: ").Append(System.Net.WebUtility.HtmlEncode(so.Carrier ?? "")).Append(" ")
                .Append(System.Net.WebUtility.HtmlEncode(so.TrackingNumber)).Append("</p>");
        sb.Append("<p class='muted'>").Append(System.Net.WebUtility.HtmlEncode(company.ReceiptFooter ?? "")).Append("</p>");
        sb.Append("</body></html>");
        return new DocumentHtmlDto { Title = $"{title} {so.OrderNumber}", Html = sb.ToString() };
    }

    public static DocumentHtmlDto BuildPurchaseDocument(ErpDbContext db, PurchaseOrder po)
    {
        var company = db.Settings.First();
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'><title>PO ").Append(po.PoNumber)
            .Append("</title><style>body{font-family:Segoe UI,Arial;margin:32px}table{border-collapse:collapse;width:100%}th,td{border-bottom:1px solid #ddd;padding:8px;text-align:left}</style></head><body>");
        sb.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(company.CompanyName)).Append("</h1>");
        sb.Append("<h2>Purchase Order ").Append(System.Net.WebUtility.HtmlEncode(po.PoNumber)).Append("</h2>");
        sb.Append("<p>Supplier: <b>").Append(System.Net.WebUtility.HtmlEncode(po.Supplier?.Name ?? "")).Append("</b><br/>Expected: ")
            .Append(po.ExpectedDate?.ToString("yyyy-MM-dd") ?? "—").Append("<br/>Status: ").Append(po.Status).Append("</p>");
        sb.Append("<table><tr><th>SKU</th><th>Item</th><th>Qty</th><th>Cost</th><th>Line</th></tr>");
        foreach (var line in po.Lines)
        {
            sb.Append("<tr><td>").Append(System.Net.WebUtility.HtmlEncode(line.Product?.Sku ?? ""))
                .Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(line.Product?.Name ?? ""))
                .Append("</td><td>").Append(line.QuantityOrdered)
                .Append("</td><td>").Append(line.UnitCost.ToString("C"))
                .Append("</td><td>").Append((line.QuantityOrdered * line.UnitCost).ToString("C")).Append("</td></tr>");
        }
        sb.Append("</table><p style='text-align:right'><b>Total: ").Append(po.Total.ToString("C")).Append("</b></p></body></html>");
        return new DocumentHtmlDto { Title = $"PO {po.PoNumber}", Html = sb.ToString() };
    }

    public static string? TrySendEmail(CompanySettings settings, string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(to))
            return "SMTP not configured or recipient missing";
        try
        {
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl = settings.SmtpEnableSsl,
                Credentials = string.IsNullOrWhiteSpace(settings.SmtpUsername)
                    ? null
                    : new System.Net.NetworkCredential(settings.SmtpUsername, settings.SmtpPassword)
            };
            var from = string.IsNullOrWhiteSpace(settings.SmtpFrom) ? settings.SmtpUsername : settings.SmtpFrom;
            var msg = new MailMessage(from ?? "ledgerly@localhost", to, subject, htmlBody) { IsBodyHtml = true };
            client.Send(msg);
            return null;
        }
        catch (System.Exception ex)
        {
            return ex.Message;
        }
    }
}
