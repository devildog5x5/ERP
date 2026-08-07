using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class DashboardView : UserControl
{
    public DashboardView() => InitializeComponent();

    public static async Task<DashboardView> CreateAsync()
    {
        var view = new DashboardView();
        var data = await App.Api.GetDashboardAsync()
                   ?? throw new InvalidOperationException("Empty dashboard response");
        view.Bind(data);
        return view;
    }

    private void Bind(DashboardDto data)
    {
        KpiProducts.Text = data.ProductCount.ToString();
        KpiLow.Text = data.LowStockCount.ToString();
        KpiPos.Text = data.OpenPoCount.ToString();
        KpiValue.Text = data.InventoryValue.ToString("C");
        LowStockGrid.ItemsSource = data.LowStockProducts;
        ReminderList.ItemsSource = data.RecentReminders;
    }
}
