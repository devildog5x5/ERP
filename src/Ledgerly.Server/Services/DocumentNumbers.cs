using System.Linq;
using Ledgerly.Server.Data;

namespace Ledgerly.Server.Services;

public static class DocumentNumbers
{
    public static string Next(ErpDbContext db, string documentType, string prefix)
    {
        var seq = db.NumberSequences.FirstOrDefault(s => s.DocumentType == documentType);
        if (seq is null)
        {
            seq = new NumberSequence { DocumentType = documentType, Prefix = prefix, NextValue = 1 };
            db.NumberSequences.Add(seq);
            db.SaveChanges();
        }
        var value = seq.NextValue++;
        db.SaveChanges();
        return $"{seq.Prefix}{value:000000}";
    }
}
