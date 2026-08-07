using System;
using System.Linq;
using Ledgerly.Server.Data;

namespace Ledgerly.Server.Services;

public static class GlPostingService
{
    public static void EnsurePeriodOpen(ErpDbContext db, DateTime date)
    {
        var closed = db.FiscalPeriods.Any(p => p.IsClosed && p.StartDate <= date && p.EndDate >= date);
        if (closed)
            throw new InvalidOperationException("Fiscal period is closed for this date.");
    }

    public static void PostBalanced(ErpDbContext db, string memo, string? sourceType, int? sourceId,
        (string AccountNumber, decimal Debit, decimal Credit, string? LineMemo)[] lines)
    {
        EnsurePeriodOpen(db, DateTime.Today);
        if (lines.Sum(l => l.Debit) != lines.Sum(l => l.Credit))
            throw new InvalidOperationException("Journal entry is not balanced.");

        var entry = new JournalEntry
        {
            EntryNumber = DocumentNumbers.Next(db, "JE", "JE-"),
            EntryDate = DateTime.Today,
            Memo = memo,
            SourceType = sourceType,
            SourceId = sourceId,
            IsPosted = true
        };
        foreach (var line in lines)
        {
            var acct = db.GlAccounts.FirstOrDefault(a => a.AccountNumber == line.AccountNumber)
                       ?? throw new InvalidOperationException($"GL account {line.AccountNumber} missing");
            entry.Lines.Add(new JournalLine
            {
                GlAccountId = acct.Id,
                Debit = line.Debit,
                Credit = line.Credit,
                Memo = line.LineMemo
            });
        }
        db.JournalEntries.Add(entry);
    }

    public static void PostSale(ErpDbContext db, SalesOrder so)
    {
        PostBalanced(db, $"Sale {so.OrderNumber}", "sales-order", so.Id,
            new[]
            {
                ("1200", so.Total, 0m, "AR"),
                ("4000", 0m, so.Subtotal - so.DiscountAmount, "Revenue"),
                ("2200", 0m, so.TaxAmount, "Tax payable"),
            }.Where(x => x.Item2 != 0 || x.Item3 != 0).ToArray());
    }

    public static void PostCogs(ErpDbContext db, SalesOrder so, decimal cogs)
    {
        if (cogs <= 0) return;
        PostBalanced(db, $"COGS {so.OrderNumber}", "sales-order-cogs", so.Id,
            new[]
            {
                ("5000", cogs, 0m, "COGS"),
                ("1400", 0m, cogs, "Inventory"),
            });
    }
}
