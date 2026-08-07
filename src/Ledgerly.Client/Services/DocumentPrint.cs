using System.Diagnostics;
using System.IO;
using System.Windows;
using Ledgerly.Shared;

namespace Ledgerly.Client.Services;

public static class DocumentPrint
{
    public static void OpenHtml(DocumentHtmlDto? doc)
    {
        if (doc == null || string.IsNullOrWhiteSpace(doc.Html))
        {
            MessageBox.Show("Document not available.", "Ledgerly");
            return;
        }
        var path = Path.Combine(Path.GetTempPath(), $"ledgerly-{System.Guid.NewGuid():N}.html");
        File.WriteAllText(path, doc.Html);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
