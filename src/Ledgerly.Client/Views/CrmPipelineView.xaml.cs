using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ledgerly.Client.Dialogs;
using Ledgerly.Shared;

namespace Ledgerly.Client.Views;

public partial class CrmPipelineView : UserControl
{
    private List<CrmAccountDto> _accounts = new();

    public CrmPipelineView() => InitializeComponent();

    public static async Task<CrmPipelineView> CreateAsync()
    {
        var view = new CrmPipelineView();
        await view.LoadAsync();
        return view;
    }

    private Window OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dto = EntityDialogs.EditCrmOpportunity(OwnerWindow, null, _accounts);
            if (dto is null) return;
            await App.Api.CreateCrmOpportunityAsync(dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmOpportunityDto selected)
        {
            MessageBox.Show("Select an opportunity first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dto = EntityDialogs.EditCrmOpportunity(OwnerWindow, selected, _accounts);
            if (dto is null) return;
            await App.Api.UpdateCrmOpportunityAsync(selected.Id, dto);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Win_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmOpportunityDto selected) return;
        if (MessageBox.Show(
                $"Mark \"{selected.Name}\" as won and create a sales quote?",
                "Coalesce", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await App.Api.WinCrmOpportunityAsync(selected.Id, "quote");
            MessageBox.Show("Opportunity won. A sales quote was created — open Sales to review it.",
                "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not CrmOpportunityDto selected) return;
        if (!EntityDialogs.ConfirmDelete(OwnerWindow, selected.Name)) return;
        try
        {
            await App.Api.DeleteCrmOpportunityAsync(selected.Id);
            await LoadAsync();
        }
        catch (Exception ex) { EntityDialogs.ShowError(ex); }
    }

    private async Task LoadAsync()
    {
        _accounts = await App.Api.GetCrmAccountsAsync() ?? new List<CrmAccountDto>();
        Grid.ItemsSource = await App.Api.GetCrmOpportunitiesAsync();
    }
}
