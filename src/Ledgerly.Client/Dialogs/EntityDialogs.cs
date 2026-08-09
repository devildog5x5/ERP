using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ledgerly.Shared;

namespace Ledgerly.Client.Dialogs;

public static class EntityDialogs
{
    public static ProductCreateDto? EditProduct(Window owner, ProductDto? existing, IList<PartnerDto> suppliers)
    {
        var sku = Field(existing?.Sku ?? "");
        var upc = Field(existing?.Upc ?? "");
        var name = Field(existing?.Name ?? "");
        var category = Field(existing?.Category ?? "");
        var unit = Field(existing?.Unit ?? "ea");
        var qty = Field((existing?.QuantityOnHand ?? 0).ToString(CultureInfo.InvariantCulture));
        var reorder = Field((existing?.ReorderPoint ?? 10).ToString(CultureInfo.InvariantCulture));
        var buyQty = Field((existing?.ReorderQuantity ?? 25).ToString(CultureInfo.InvariantCulture));
        var cost = Field((existing?.UnitCost ?? 0).ToString(CultureInfo.InvariantCulture));
        var price = Field((existing?.SellPrice ?? 0).ToString(CultureInfo.InvariantCulture));
        var supplierItems = new List<ComboItem> { new ComboItem(0, "(none)") };
        supplierItems.AddRange(suppliers.Select(s => new ComboItem(s.Id, s.Name)));
        var supplier = Combo(supplierItems, existing?.SupplierId ?? 0);

        if (!Show(owner, existing is null ? "Add product" : "Edit product", 460,
                Row("SKU", sku),
                Row("UPC / barcode (scan here)", upc),
                Row("Name", name), Row("Category", category), Row("Unit", unit),
                Row("On hand", qty), Row("Reorder point", reorder), Row("Buy qty", buyQty),
                Row("Unit cost", cost), Row("Sell price", price), Row("Supplier", supplier)))
            return null;

        if (string.IsNullOrWhiteSpace(sku.Text) || string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show("SKU and name are required.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return new ProductCreateDto
        {
            Sku = sku.Text.Trim(),
            Upc = NullIfEmpty(upc.Text),
            Name = name.Text.Trim(),
            Category = NullIfEmpty(category.Text),
            Unit = string.IsNullOrWhiteSpace(unit.Text) ? "ea" : unit.Text.Trim(),
            QuantityOnHand = Dec(qty.Text),
            ReorderPoint = Dec(reorder.Text),
            ReorderQuantity = Dec(buyQty.Text),
            UnitCost = Dec(cost.Text),
            SellPrice = Dec(price.Text),
            SupplierId = SelectedId(supplier) is int sid && sid > 0 ? sid : null
        };
    }

    public static PartnerCreateDto? EditPartner(Window owner, string kind, PartnerDto? existing)
    {
        var name = Field(existing?.Name ?? "");
        var email = Field(existing?.Email ?? "");
        var phone = Field(existing?.Phone ?? "");
        var address = Field(existing?.Address ?? "");

        if (!Show(owner, existing is null ? $"Add {kind}" : $"Edit {kind}", 420,
                Row("Name", name), Row("Email", email), Row("Phone", phone), Row("Address", address)))
            return null;

        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show("Name is required.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return new PartnerCreateDto
        {
            Name = name.Text.Trim(),
            Email = NullIfEmpty(email.Text),
            Phone = NullIfEmpty(phone.Text),
            Address = NullIfEmpty(address.Text)
        };
    }

    public static ReminderCreateDto? EditReminder(Window owner, ReminderDto? existing)
    {
        var type = Field(existing?.ReminderType ?? "manual");
        var severity = Combo(new List<ComboItem>
        {
            new ComboItem(0, "info"), new ComboItem(1, "warning"), new ComboItem(2, "critical")
        }, SeverityIndex(existing?.Severity));
        var title = Field(existing?.Title ?? "");
        var message = Field(existing?.Message ?? "", multiline: true);

        if (!Show(owner, existing is null ? "Add reminder" : "Edit reminder", 440,
                Row("Type", type), Row("Severity", severity), Row("Title", title), Row("Message", message)))
            return null;

        if (string.IsNullOrWhiteSpace(title.Text))
        {
            MessageBox.Show("Title is required.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return new ReminderCreateDto
        {
            ReminderType = string.IsNullOrWhiteSpace(type.Text) ? "manual" : type.Text.Trim(),
            Severity = ((ComboItem)severity.SelectedItem).Label,
            Title = title.Text.Trim(),
            Message = message.Text.Trim()
        };
    }

    public static PurchaseOrderCreateDto? EditPurchaseOrder(
        Window owner, PurchaseOrderDto? existing, IList<PartnerDto> suppliers, IList<ProductDto> products)
    {
        if (suppliers.Count == 0 || products.Count == 0)
        {
            MessageBox.Show("Add at least one supplier and one product first.", "Ledgerly",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        var supplier = Combo(suppliers.Select(s => new ComboItem(s.Id, s.Name)).ToList(), existing?.SupplierId ?? suppliers[0].Id);
        var expected = Field(existing?.ExpectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"));
        var notes = Field("", multiline: true);
        var lines = new List<LineDraft>();
        if (existing != null)
        {
            foreach (var l in existing.Lines)
                lines.Add(new LineDraft(l.ProductId, l.QuantityOrdered, l.UnitCost, LabelFor(products, l.ProductId)));
        }

        var linesBox = new ListBox { Height = 120, Margin = new Thickness(0, 0, 0, 8) };
        void RefreshLines() => linesBox.ItemsSource = lines.Select(l => l.Display).ToList();
        RefreshLines();

        var product = Combo(products.Select(p => new ComboItem(p.Id, $"{p.Sku} — {p.Name}")).ToList(), products[0].Id);
        var qty = Field("1");
        var addLine = new Button { Content = "Add line", Style = (Style)Application.Current.FindResource("SecondaryButton"), Margin = new Thickness(8, 0, 0, 0) };
        addLine.Click += (_, _) =>
        {
            var pid = SelectedId(product) ?? 0;
            var q = Dec(qty.Text);
            if (pid <= 0 || q <= 0) return;
            var p = products.First(x => x.Id == pid);
            lines.Add(new LineDraft(pid, q, p.UnitCost, $"{p.Sku} — {p.Name}"));
            RefreshLines();
        };
        var removeLine = new Button { Content = "Remove selected", Style = (Style)Application.Current.FindResource("SecondaryButton"), Margin = new Thickness(8, 0, 0, 0) };
        removeLine.Click += (_, _) =>
        {
            if (linesBox.SelectedIndex < 0 || linesBox.SelectedIndex >= lines.Count) return;
            lines.RemoveAt(linesBox.SelectedIndex);
            RefreshLines();
        };

        var lineTools = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(removeLine, Dock.Right);
        DockPanel.SetDock(addLine, Dock.Right);
        lineTools.Children.Add(removeLine);
        lineTools.Children.Add(addLine);
        lineTools.Children.Add(product);

        var rows = new List<UIElement>
        {
            Row("Supplier", supplier),
            Row("Expected (yyyy-MM-dd)", expected),
            Row("Notes", notes),
            new TextBlock { Text = "Lines", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) },
            lineTools,
            Row("Qty", qty),
            linesBox
        };

        if (!Show(owner, existing is null ? "Add purchase order" : "Edit purchase order", 520, rows))
            return null;

        if (lines.Count == 0)
        {
            MessageBox.Show("Add at least one line.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        DateTime? expectedDate = null;
        if (DateTime.TryParse(expected.Text, out var d)) expectedDate = d.Date;

        return new PurchaseOrderCreateDto
        {
            SupplierId = SelectedId(supplier) ?? 0,
            ExpectedDate = expectedDate,
            Notes = NullIfEmpty(notes.Text),
            Lines = lines.Select(l => new PurchaseOrderLineCreateDto
            {
                ProductId = l.ProductId,
                QuantityOrdered = l.Quantity,
                UnitCost = l.UnitCost
            }).ToList()
        };
    }

    public static SalesOrderCreateDto? EditSalesOrder(
        Window owner, SalesOrderDto? existing, IList<PartnerDto> customers, IList<ProductDto> products)
    {
        if (customers.Count == 0 || products.Count == 0)
        {
            MessageBox.Show("Add at least one customer and one product first.", "Ledgerly",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        var customer = Combo(customers.Select(c => new ComboItem(c.Id, c.Name)).ToList(), existing?.CustomerId ?? customers[0].Id);
        var docType = Combo(new List<ComboItem>
        {
            new ComboItem(0, "order — deducts inventory"),
            new ComboItem(1, "quote — no stock deduction")
        }, string.Equals(existing?.DocumentType, "quote", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        var notes = Field(existing?.Notes ?? "", multiline: true);
        notes.MinHeight = 72;
        var lines = new List<LineDraft>();
        if (existing != null)
        {
            foreach (var l in existing.Lines)
                lines.Add(new LineDraft(l.ProductId, l.Quantity, l.UnitPrice, LabelFor(products, l.ProductId)));
        }

        var linesBox = new ListBox { Height = 160, Margin = new Thickness(0, 0, 0, 8), MinWidth = 480 };
        void RefreshLines() => linesBox.ItemsSource = lines.Select(l => l.Display).ToList();
        RefreshLines();

        bool IsQuote() => (SelectedId(docType) ?? 0) == 1;

        var product = Combo(products.Select(p => new ComboItem(p.Id,
            $"{p.Sku} — {p.Name} (avail {p.QuantityOnHand:0.##})")).ToList(), products[0].Id);
        var qty = Field("1");
        var addLine = new Button { Content = "Add line", Style = (Style)Application.Current.FindResource("SecondaryButton"), Margin = new Thickness(8, 0, 0, 0) };
        addLine.Click += (_, _) =>
        {
            var pid = SelectedId(product) ?? 0;
            var q = Dec(qty.Text);
            if (pid <= 0 || q <= 0) return;
            var p = products.First(x => x.Id == pid);
            var already = lines.Where(l => l.ProductId == pid).Sum(l => l.Quantity);
            if (!IsQuote() && already + q > p.QuantityOnHand)
            {
                MessageBox.Show(
                    $"Insufficient stock for {p.Sku}.\nRequested {already + q}, available {p.QuantityOnHand}.\n\n" +
                    "Lower the qty, receive stock first, or switch Document type to Quote.",
                    "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            lines.Add(new LineDraft(pid, q, p.SellPrice, $"{p.Sku} — {p.Name} × {q:0.##}"));
            RefreshLines();
        };
        var removeLine = new Button { Content = "Remove selected", Style = (Style)Application.Current.FindResource("SecondaryButton"), Margin = new Thickness(8, 0, 0, 0) };
        removeLine.Click += (_, _) =>
        {
            if (linesBox.SelectedIndex < 0 || linesBox.SelectedIndex >= lines.Count) return;
            lines.RemoveAt(linesBox.SelectedIndex);
            RefreshLines();
        };

        var lineTools = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(removeLine, Dock.Right);
        DockPanel.SetDock(addLine, Dock.Right);
        lineTools.Children.Add(removeLine);
        lineTools.Children.Add(addLine);
        lineTools.Children.Add(product);

        var rows = new List<UIElement>
        {
            Row("Customer", customer),
            Row("Document type", docType),
            Row("Notes", notes),
            new TextBlock { Text = "Lines", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) },
            lineTools,
            Row("Qty", qty),
            linesBox
        };

        if (!Show(owner, existing is null ? "Add sales order" : "Edit sales order", 580, rows, canResize: true))
            return null;

        if (lines.Count == 0)
        {
            MessageBox.Show("Add at least one line.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var asQuote = IsQuote();
        if (!asQuote)
        {
            foreach (var group in lines.GroupBy(l => l.ProductId))
            {
                var p = products.First(x => x.Id == group.Key);
                var totalQty = group.Sum(l => l.Quantity);
                if (totalQty > p.QuantityOnHand)
                {
                    MessageBox.Show(
                        $"Insufficient stock for {p.Sku}.\nRequested {totalQty}, available {p.QuantityOnHand}.",
                        "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
            }
        }

        return new SalesOrderCreateDto
        {
            CustomerId = SelectedId(customer) ?? 0,
            DocumentType = asQuote ? "quote" : "order",
            Notes = NullIfEmpty(notes.Text),
            Lines = lines.Select(l => new SalesOrderLineCreateDto
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitCost
            }).ToList()
        };
    }

    public static ReceivePurchaseOrderDto? ReceivePurchaseOrder(Window owner, PurchaseOrderDto po)
    {
        var remaining = po.Lines.Where(l => l.QuantityOrdered > l.QuantityReceived).ToList();
        if (remaining.Count == 0)
        {
            MessageBox.Show("Nothing left to receive on this PO.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        var fields = new List<(PurchaseOrderLineDto Line, TextBox Qty)>();
        var rows = new List<UIElement>
        {
            new TextBlock
            {
                Text = $"Receive remaining quantities for {po.PoNumber}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            }
        };
        foreach (var line in remaining)
        {
            var left = line.QuantityOrdered - line.QuantityReceived;
            var qty = Field(left.ToString(CultureInfo.InvariantCulture));
            fields.Add((line, qty));
            rows.Add(Row($"{line.ProductSku} (remain {left})", qty));
        }

        if (!Show(owner, "Receive purchase order", 440, rows))
            return null;

        return new ReceivePurchaseOrderDto
        {
            Lines = fields.Select(f => new ReceiveLineDto
            {
                LineId = f.Line.Id,
                QuantityReceived = Dec(f.Qty.Text)
            }).Where(x => x.QuantityReceived > 0).ToList()
        };
    }

    public static bool ConfirmDelete(Window owner, string label) =>
        MessageBox.Show(owner, $"Delete {label}?\nThis cannot be undone.", "Confirm delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public static bool Confirm(Window owner, string title, string message) =>
        MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    /// <summary>
    /// Triple confirmation for database refresh: two explicit checks + typed phrase.
    /// Returns true only when all three are satisfied and the user clicks Refresh.
    /// </summary>
    public static bool ConfirmDatabaseRefresh(Window? owner)
    {
        var dialog = new Window
        {
            Title = "Confirm database refresh — 3 steps required",
            Owner = owner,
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Background = Brushes.White,
            Topmost = true,
            ShowInTaskbar = false
        };

        const string requiredPhrase = "REFRESH DATABASE";
        var check1 = new CheckBox
        {
            Content = "Step 1 of 3 — I understand this will ERASE all products, orders, partners, finance data, and users.",
            Margin = new Thickness(0, 0, 0, 12)
        };
        var check2 = new CheckBox
        {
            Content = "Step 2 of 3 — I understand this cannot be undone in the app (only by restoring a backup).",
            Margin = new Thickness(0, 0, 0, 12)
        };
        var phraseBox = new TextBox { Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 4, 0, 0) };
        var phraseHint = new TextBlock
        {
            Text = $"Step 3 of 3 — Type {requiredPhrase} exactly (case-sensitive).",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var proceed = new Button
        {
            Content = "Refresh database",
            Style = (Style)Application.Current.FindResource("DangerButton"),
            IsEnabled = false,
            MinWidth = 140,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var cancel = new Button
        {
            Content = "Cancel",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            IsCancel = true,
            MinWidth = 90
        };

        void UpdateProceed()
        {
            proceed.IsEnabled =
                check1.IsChecked == true &&
                check2.IsChecked == true &&
                string.Equals(phraseBox.Text.Trim(), requiredPhrase, StringComparison.Ordinal);
        }

        check1.Checked += (_, _) => UpdateProceed();
        check1.Unchecked += (_, _) => UpdateProceed();
        check2.Checked += (_, _) => UpdateProceed();
        check2.Unchecked += (_, _) => UpdateProceed();
        phraseBox.TextChanged += (_, _) => UpdateProceed();

        proceed.Click += (_, _) =>
        {
            if (!proceed.IsEnabled) return;
            dialog.DialogResult = true;
            dialog.Close();
        };
        cancel.Click += (_, _) =>
        {
            dialog.DialogResult = false;
            dialog.Close();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        buttons.Children.Add(proceed);
        buttons.Children.Add(cancel);

        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock
        {
            Text = "Triple confirmation required before the database is wiped.",
            FontWeight = FontWeights.SemiBold,
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "A backup is created first. Afterward you get a clean company start (no demo customers/suppliers/products). Sign in again as admin / admin.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
            Margin = new Thickness(0, 0, 0, 16)
        });
        panel.Children.Add(check1);
        panel.Children.Add(check2);
        panel.Children.Add(phraseHint);
        panel.Children.Add(phraseBox);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        return dialog.ShowDialog() == true;
    }

    public static decimal? PromptDecimal(Window owner, string title, string message, string defaultValue = "1")
    {
        var box = Field(defaultValue);
        if (!Show(owner, title, 360, new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) }, Row("Amount", box)))
            return null;
        return Dec(box.Text);
    }

    public static string? FieldPrompt(Window owner, string label, string defaultValue = "")
    {
        var box = Field(defaultValue);
        if (!Show(owner, label, 360, Row(label, box))) return null;
        return box.Text.Trim();
    }

    public static UserCreateDto? EditUser(Window owner, IList<RoleDto> roles)
    {
        if (roles.Count == 0)
        {
            MessageBox.Show("No roles are defined.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var userName = Field("");
        var displayName = Field("");
        var password = Field("changeme");
        var roleItems = roles
            .Select(r => new ComboItem(r.Id, $"{r.Name}  ·  {SummarizePermissions(r.Permissions)}"))
            .ToList();
        var role = Combo(roleItems, roles[0].Id);
        role.ToolTip = "Role permissions control which screens and actions this user can access.";

        if (!Show(owner, "Add user", 480,
                Row("Username", userName),
                Row("Display name", displayName),
                Row("Password", password),
                Row("Role (element access)", role)))
            return null;

        if (string.IsNullOrWhiteSpace(userName.Text) || string.IsNullOrWhiteSpace(password.Text))
        {
            MessageBox.Show("Username and password are required.", "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var roleId = SelectedId(role) ?? roles[0].Id;
        return new UserCreateDto
        {
            UserName = userName.Text.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName.Text) ? userName.Text.Trim() : displayName.Text.Trim(),
            Password = password.Text,
            RoleId = roleId
        };
    }

    private static string SummarizePermissions(string? permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions)) return "no permissions";
        var parts = permissions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        if (parts.Count == 0) return "no permissions";
        if (parts.Count <= 4) return string.Join(", ", parts);
        return string.Join(", ", parts.Take(4)) + $", +{parts.Count - 4} more";
    }

    public static void ShowError(Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            App.PromptRelogin(ex.Message);
            return;
        }
        MessageBox.Show(CleanError(ex.Message), "Ledgerly", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static string CleanError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Unexpected error.";
        var text = message.Trim();
        // Strip typical Web API wrappers: 400 Bad Request: "..."
        var colon = text.IndexOf(':');
        if (colon > 0 && text.Length > colon + 1 &&
            (text.StartsWith("400", StringComparison.Ordinal) ||
             text.StartsWith("403", StringComparison.Ordinal) ||
             text.StartsWith("404", StringComparison.Ordinal) ||
             text.StartsWith("500", StringComparison.Ordinal)))
            text = text.Substring(colon + 1).Trim();
        text = text.Trim().Trim('"');
        if (text.StartsWith("{\"message\":", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("{\"Message\":", StringComparison.OrdinalIgnoreCase))
        {
            var start = text.IndexOf(':');
            var end = text.LastIndexOf('"');
            if (start > 0 && end > start + 2)
                text = text.Substring(start + 1, end - start - 1).Trim().Trim('"');
        }
        return text;
    }

    private static bool Show(Window owner, string title, double width, params UIElement[] rows) =>
        Show(owner, title, width, (IEnumerable<UIElement>)rows, canResize: false);

    private static bool Show(Window owner, string title, double width, IEnumerable<UIElement> rows, bool canResize = false)
    {
        var dialog = new Window
        {
            Title = title,
            Owner = owner,
            Width = width,
            MinWidth = Math.Min(width, 420),
            SizeToContent = canResize ? SizeToContent.Manual : SizeToContent.Height,
            Height = canResize ? 560 : double.NaN,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = canResize ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize,
            Background = Brushes.White
        };

        var panel = new StackPanel { Margin = new Thickness(20) };
        foreach (var row in rows) panel.Children.Add(row);

        var ok = new Button
        {
            Content = "Save",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            IsDefault = true,
            Margin = new Thickness(0, 0, 8, 0),
            MinWidth = 90
        };
        var cancel = new Button
        {
            Content = "Cancel",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            IsCancel = true,
            MinWidth = 90
        };
        ok.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
        cancel.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        dialog.Content = canResize
            ? new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            }
            : panel;
        return dialog.ShowDialog() == true;
    }

    private static UIElement Row(string label, UIElement control)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
            Margin = new Thickness(0, 0, 0, 4)
        });
        stack.Children.Add(control);
        return stack;
    }

    private static TextBox Field(string value, bool multiline = false) => new()
    {
        Text = value,
        Padding = new Thickness(8, 6, 8, 6),
        AcceptsReturn = multiline,
        TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
        Height = multiline ? 72 : double.NaN,
        VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden
    };

    private static ComboBox Combo(IList<ComboItem> items, int selectedId)
    {
        return new ComboBox
        {
            ItemsSource = items,
            DisplayMemberPath = nameof(ComboItem.Label),
            SelectedValuePath = nameof(ComboItem.Id),
            SelectedValue = items.Any(i => i.Id == selectedId) ? selectedId : items.FirstOrDefault()?.Id,
            Padding = new Thickness(6)
        };
    }

    private static int? SelectedId(ComboBox box) =>
        box.SelectedValue as int? ?? (box.SelectedItem as ComboItem)?.Id;

    private static decimal Dec(string text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
        || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out d)
            ? d : 0;

    private static string? NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static string LabelFor(IList<ProductDto> products, int id)
    {
        var p = products.FirstOrDefault(x => x.Id == id);
        return p is null ? $"Product #{id}" : $"{p.Sku} — {p.Name}";
    }

    private static int SeverityIndex(string? severity) =>
        severity?.ToLowerInvariant() switch
        {
            "warning" => 1,
            "critical" => 2,
            _ => 0
        };

    private sealed class ComboItem
    {
        public ComboItem(int id, string label) { Id = id; Label = label; }
        public int Id { get; }
        public string Label { get; }
    }

    private sealed class LineDraft
    {
        public LineDraft(int productId, decimal quantity, decimal unitCost, string label)
        {
            ProductId = productId;
            Quantity = quantity;
            UnitCost = unitCost;
            Label = label;
        }

        public int ProductId { get; }
        public decimal Quantity { get; }
        public decimal UnitCost { get; }
        public string Label { get; }
        public string Display => $"{Label}  × {Quantity} @ {UnitCost:C}";
    }
}
