using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ledgerly.Client.Views;

public partial class ReportsView : UserControl
{
    public ReportsView() => InitializeComponent();

    public static async Task<ReportsView> CreateAsync()
    {
        var v = new ReportsView();
        var r = await App.Api.GetReportSummaryAsync();
        if (r != null)
        {
            v.InvValue.Text = r.InventoryValue.ToString("C");
            v.MarginPct.Text = r.GrossMarginPercent.ToString("0.##") + "%";
            v.ArTotal.Text = r.ArTotal.ToString("C");
            v.ApTotal.Text = r.ApTotal.ToString("C");
            v.ArGrid.ItemsSource = r.ArAging;
            v.ApGrid.ItemsSource = r.ApAging;
            v.DeadGrid.ItemsSource = r.DeadStock;
        }
        return v;
    }
}
