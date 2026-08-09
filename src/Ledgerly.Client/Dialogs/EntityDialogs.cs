using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
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
            MessageBox.Show("SKU and name are required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Name is required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Title is required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Add at least one supplier and one product first.", "Coalesce",
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
            MessageBox.Show("Add at least one line.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Add at least one customer and one product first.", "Coalesce",
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
            MessageBox.Show("Add at least one line.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Nothing left to receive on this PO.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
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

    /// <summary>
    /// Admin database status dialog. Returns an action key the caller should run, or null if closed.
    /// Action keys: backup, purge, grow, free-disk (informational).
    /// </summary>
    public static string? ShowDatabaseStatus(Window? owner, DatabaseStatusDto status)
    {
        string? chosen = null;
        var dialog = new Window
        {
            Title = "Database status",
            Owner = owner,
            Width = 640,
            Height = 620,
            MinWidth = 520,
            MinHeight = 480,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.CanResizeWithGrip,
            Background = Brushes.White
        };

        var levelBrush = status.CapacityLevel switch
        {
            "critical" => new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18)),
            "high" => new SolidColorBrush(Color.FromRgb(0xB5, 0x47, 0x08)),
            "watch" => new SolidColorBrush(Color.FromRgb(0xA1, 0x62, 0x07)),
            _ => new SolidColorBrush(Color.FromRgb(0x02, 0x78, 0x4A))
        };
        var levelBg = status.CapacityLevel switch
        {
            "critical" => new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2)),
            "high" => new SolidColorBrush(Color.FromRgb(0xFF, 0xF7, 0xED)),
            "watch" => new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB)),
            _ => new SolidColorBrush(Color.FromRgb(0xEC, 0xFD, 0xF5))
        };

        var root = new DockPanel { Margin = new Thickness(20) };

        var header = new Border
        {
            Background = levelBg,
            BorderBrush = levelBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 14)
        };
        header.Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = status.ProviderLabel, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x20, 0x2B)) },
                new TextBlock { Text = status.CapacityLabel, Foreground = levelBrush, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = status.Summary, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)), Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap }
            }
        };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        Button ActionBtn(string content, string key, bool primary = false)
        {
            var b = new Button
            {
                Content = content,
                Style = (Style)Application.Current.FindResource(primary ? "PrimaryButton" : "SecondaryButton"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 110,
                Tag = key
            };
            b.Click += (_, _) =>
            {
                chosen = key;
                dialog.DialogResult = true;
            };
            return b;
        }
        buttons.Children.Add(ActionBtn("Grow database…", "grow", primary: true));
        buttons.Children.Add(ActionBtn("Backup now", "backup"));
        if (status.RecommendedActions.Any(a => string.Equals(a, "purge", StringComparison.OrdinalIgnoreCase))
            || status.Suggestions.Any(s => string.Equals(s.ActionKey, "purge", StringComparison.OrdinalIgnoreCase)))
            buttons.Children.Add(ActionBtn("Purge old logs…", "purge"));
        var close = new Button
        {
            Content = "Close",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            IsCancel = true,
            MinWidth = 90
        };
        close.Click += (_, _) => dialog.DialogResult = false;
        buttons.Children.Add(close);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var body = new StackPanel();

        body.Children.Add(SectionTitle("How full"));
        body.Children.Add(BuildCapacityPiePanel(status, levelBrush));

        body.Children.Add(MetricLine("Database used", status.UsedDisplay));
        body.Children.Add(MetricLine("Free space", status.FreeDisplay));
        body.Children.Add(MetricLine("Capacity / volume", status.CapacityDisplay));
        body.Children.Add(MetricLine("Percent full", status.PercentDisplay));
        if (!string.IsNullOrWhiteSpace(status.EngineVersion))
            body.Children.Add(MetricLine("Engine version", status.EngineVersion!));
        if (!string.IsNullOrWhiteSpace(status.Location))
            body.Children.Add(MetricLine("Location", status.Location!));
        body.Children.Add(MetricLine("Multi-user ready", status.MultiUserReady ? "Yes" : "No (local SQLite)"));

        body.Children.Add(SectionTitle("Characteristics"));
        foreach (var c in status.Characteristics)
            body.Children.Add(Bullet(c));

        body.Children.Add(SectionTitle("Largest tables (row counts)"));
        var tablePie = BuildTableSharePiePanel(status.Tables);
        if (tablePie != null)
            body.Children.Add(tablePie);
        foreach (var t in status.Tables.Take(10))
            body.Children.Add(MetricLine(t.Name, t.Rows.ToString("N0")));

        body.Children.Add(SectionTitle("Suggestions"));
        foreach (var s in status.Suggestions)
        {
            var card = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xEE)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC))
            };
            card.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = $"[{s.Severity.ToUpperInvariant()}] {s.Title}",
                        FontWeight = FontWeights.SemiBold,
                        Foreground = s.Severity switch
                        {
                            "critical" => new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18)),
                            "high" => new SolidColorBrush(Color.FromRgb(0xB5, 0x47, 0x08)),
                            "watch" => new SolidColorBrush(Color.FromRgb(0xA1, 0x62, 0x07)),
                            _ => new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x48))
                        },
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = s.Detail,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
                        Margin = new Thickness(0, 4, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };
            body.Children.Add(card);
        }

        scroll.Content = body;
        root.Children.Add(scroll);
        dialog.Content = root;
        dialog.ShowDialog();
        return chosen;
    }

    private static TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        FontSize = 14,
        Margin = new Thickness(0, 8, 0, 8),
        Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x20, 0x2B))
    };

    private static TextBlock MetricLine(string label, string value) => new()
    {
        Text = $"{label}: {value}",
        Margin = new Thickness(0, 0, 0, 4),
        TextWrapping = TextWrapping.Wrap,
        Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x48))
    };

    private static TextBlock Bullet(string text) => new()
    {
        Text = "• " + text,
        Margin = new Thickness(0, 0, 0, 4),
        TextWrapping = TextWrapping.Wrap,
        Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x48))
    };

    /// <summary>Compact capacity donut for Settings summary (or any host).</summary>
    public static UIElement BuildCapacityPieChart(DatabaseStatusDto status, double size = 88)
    {
        var usedBrush = status.CapacityLevel switch
        {
            "critical" => new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18)),
            "high" => new SolidColorBrush(Color.FromRgb(0xB5, 0x47, 0x08)),
            "watch" => new SolidColorBrush(Color.FromRgb(0xCA, 0x8A, 0x04)),
            _ => new SolidColorBrush(Color.FromRgb(0x02, 0x78, 0x4A))
        };
        var freeBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xEE));
        var pct = ResolveCapacityPercent(status);
        return BuildDonutChart(
            new[]
            {
                ("Used", Math.Max(0.01, pct), (Brush)usedBrush),
                ("Free", Math.Max(0.01, 100 - pct), (Brush)freeBrush)
            },
            $"{pct:0.#}%",
            size);
    }

    private static FrameworkElement BuildCapacityPiePanel(DatabaseStatusDto status, Brush usedBrush)
    {
        var pct = ResolveCapacityPercent(status);
        var freeBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xEE));
        var pie = BuildDonutChart(
            new[]
            {
                ("Used", Math.Max(0.01, pct), usedBrush),
                ("Free", Math.Max(0.01, 100 - pct), (Brush)freeBrush)
            },
            $"{pct:0.#}%",
            168);

        var legend = new StackPanel { Margin = new Thickness(20, 8, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        legend.Children.Add(LegendRow(usedBrush, "Used / full", status.UsedDisplay + (status.PercentFull.HasValue ? $" ({status.PercentDisplay})" : $" (~{pct:0.#}%)")));
        legend.Children.Add(LegendRow(freeBrush, "Free / remaining", status.FreeDisplay));
        legend.Children.Add(new TextBlock
        {
            Text = status.CapacityLabel,
            FontWeight = FontWeights.SemiBold,
            Foreground = usedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0),
            MaxWidth = 280
        });
        legend.Children.Add(new TextBlock
        {
            Text = status.PercentFull.HasValue
                ? "Pie shows volume/capacity fullness."
                : "Pie estimates fullness from database size guidance (SQLite growth bands).",
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0),
            MaxWidth = 280
        });

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 14),
            Children = { pie, legend }
        };
    }

    private static FrameworkElement? BuildTableSharePiePanel(IList<DatabaseTableStatDto> tables)
    {
        var top = tables.Where(t => t.Rows > 0).Take(6).ToList();
        if (top.Count < 2) return null;

        var palette = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0x89)),
            new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E)),
            new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)),
            new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09)),
            new SolidColorBrush(Color.FromRgb(0xBE, 0x12, 0x3C)),
            new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B))
        };
        var slices = top.Select((t, i) => (t.Name, (double)t.Rows, palette[i % palette.Length])).ToArray();
        var pie = BuildDonutChart(slices, "Rows", 140);
        var legend = new StackPanel { Margin = new Thickness(18, 4, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        foreach (var (name, rows, brush) in slices)
            legend.Children.Add(LegendRow(brush, name, rows.ToString("N0")));

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12),
            Children = { pie, legend }
        };
    }

    private static double ResolveCapacityPercent(DatabaseStatusDto status)
    {
        if (status.PercentFull.HasValue)
            return Math.Max(0, Math.Min(100, status.PercentFull.Value));

        // Soft guidance when only file/db size is known (common for MySQL/Postgres metrics).
        if (status.UsedBytes is > 0)
        {
            const double watch = 250L * 1024 * 1024;
            const double high = 1024L * 1024 * 1024;
            const double critical = 2L * 1024 * 1024 * 1024;
            var used = (double)status.UsedBytes.Value;
            if (used >= critical) return 95;
            if (used >= high) return 70 + 20 * ((used - high) / (critical - high));
            if (used >= watch) return 40 + 30 * ((used - watch) / (high - watch));
            return Math.Max(5, 40 * (used / watch));
        }

        return status.CapacityLevel switch
        {
            "critical" => 95,
            "high" => 80,
            "watch" => 55,
            _ => 18
        };
    }

    private static FrameworkElement LegendRow(Brush color, string label, string value)
    {
        var swatch = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = color,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var text = new TextBlock
        {
            Text = $"{label}: {value}",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x48))
        };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 6),
            Children = { swatch, text }
        };
    }

    private static FrameworkElement BuildDonutChart(
        IReadOnlyList<(string Label, double Value, Brush Fill)> slices,
        string centerText,
        double size)
    {
        var total = slices.Sum(s => s.Value);
        if (total <= 0) total = 1;

        var canvas = new Canvas { Width = size, Height = size };
        var cx = size / 2;
        var cy = size / 2;
        var outer = size * 0.46;
        var inner = size * 0.28;
        var angle = 0.0;

        foreach (var slice in slices)
        {
            var sweep = 360.0 * (slice.Value / total);
            if (sweep <= 0.001) continue;
            // Full circle as ellipse (ArcSegment can't span exactly 360° reliably).
            if (sweep >= 359.9)
            {
                var outerRing = new Ellipse { Width = outer * 2, Height = outer * 2, Fill = slice.Fill };
                Canvas.SetLeft(outerRing, cx - outer);
                Canvas.SetTop(outerRing, cy - outer);
                canvas.Children.Add(outerRing);
                var hole = new Ellipse { Width = inner * 2, Height = inner * 2, Fill = Brushes.White };
                Canvas.SetLeft(hole, cx - inner);
                Canvas.SetTop(hole, cy - inner);
                canvas.Children.Add(hole);
                angle += sweep;
                continue;
            }

            var path = new Path { Fill = slice.Fill, Data = DonutSliceGeometry(cx, cy, outer, inner, angle, sweep) };
            canvas.Children.Add(path);
            angle += sweep;
        }

        var label = new TextBlock
        {
            Text = centerText,
            FontWeight = FontWeights.SemiBold,
            FontSize = size >= 140 ? 16 : 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x20, 0x2B)),
            TextAlignment = TextAlignment.Center
        };
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(label, cx - label.DesiredSize.Width / 2);
        Canvas.SetTop(label, cy - label.DesiredSize.Height / 2);
        canvas.Children.Add(label);

        return new Border
        {
            Child = canvas,
            Width = size,
            Height = size,
            HorizontalAlignment = HorizontalAlignment.Left
        };
    }

    private static Geometry DonutSliceGeometry(double cx, double cy, double outerR, double innerR, double startDeg, double sweepDeg)
    {
        var startOuter = Polar(cx, cy, outerR, startDeg);
        var endOuter = Polar(cx, cy, outerR, startDeg + sweepDeg);
        var startInner = Polar(cx, cy, innerR, startDeg);
        var endInner = Polar(cx, cy, innerR, startDeg + sweepDeg);
        var large = sweepDeg > 180;

        var fig = new PathFigure { StartPoint = startOuter, IsClosed = true };
        fig.Segments.Add(new ArcSegment
        {
            Point = endOuter,
            Size = new Size(outerR, outerR),
            IsLargeArc = large,
            SweepDirection = SweepDirection.Clockwise
        });
        fig.Segments.Add(new LineSegment { Point = endInner });
        fig.Segments.Add(new ArcSegment
        {
            Point = startInner,
            Size = new Size(innerR, innerR),
            IsLargeArc = large,
            SweepDirection = SweepDirection.Counterclockwise
        });

        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        geo.Freeze();
        return geo;
    }

    private static Point Polar(double cx, double cy, double radius, double angleDeg)
    {
        var rad = (angleDeg - 90) * Math.PI / 180.0;
        return new Point(cx + radius * Math.Cos(rad), cy + radius * Math.Sin(rad));
    }

    /// <summary>
    /// Guided grow wizard. Returns true if the database was grown successfully.
    /// </summary>
    public static async Task<bool> ShowGrowDatabaseAsync(
        Window? owner,
        Func<DatabaseConnectionTestDto, Task<DatabaseConnectionTestResultDto?>> testAsync,
        Func<DatabaseGrowDto, Task<DatabaseGrowResultDto?>> growAsync)
    {
        var dialog = new Window
        {
            Title = "Grow database",
            Owner = owner,
            Width = 560,
            Height = 640,
            MinWidth = 480,
            MinHeight = 520,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.CanResizeWithGrip,
            Background = Brushes.White
        };

        var providerBox = new ComboBox
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(8, 6, 8, 6),
            ItemsSource = new[] { "SqlServer", "MySql", "PostgreSql" },
            SelectedIndex = 0
        };
        var hostBox = Field("localhost");
        var portBox = Field("1433");
        var dbBox = Field("Coalesce");
        var userBox = Field("");
        var passBox = new PasswordBox { Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 0, 10) };
        var windowsAuth = new CheckBox
        {
            Content = "Use Windows authentication (SQL Server)",
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var copyMode = new RadioButton
        {
            Content = "Copy my data (recommended)",
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 4)
        };
        var emptyMode = new RadioButton
        {
            Content = "Start empty on the new database",
            Margin = new Thickness(0, 0, 0, 10)
        };
        var advanced = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 64,
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 8),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
            Margin = new Thickness(0, 0, 0, 8),
            MinHeight = 36
        };
        var phraseBox = new TextBox { Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 4, 0, 0) };

        void ApplyProviderDefaults()
        {
            var p = providerBox.SelectedItem as string ?? "SqlServer";
            if (p == "SqlServer")
            {
                portBox.Text = "1433";
                windowsAuth.IsEnabled = true;
                windowsAuth.IsChecked = true;
                userBox.IsEnabled = false;
                passBox.IsEnabled = false;
            }
            else
            {
                portBox.Text = p == "MySql" ? "3306" : "5432";
                windowsAuth.IsChecked = false;
                windowsAuth.IsEnabled = false;
                userBox.IsEnabled = true;
                passBox.IsEnabled = true;
                if (string.IsNullOrWhiteSpace(userBox.Text))
                    userBox.Text = "coalesce";
            }
        }
        providerBox.SelectionChanged += (_, _) => ApplyProviderDefaults();
        windowsAuth.Checked += (_, _) => { userBox.IsEnabled = false; passBox.IsEnabled = false; };
        windowsAuth.Unchecked += (_, _) =>
        {
            if ((providerBox.SelectedItem as string) == "SqlServer")
            {
                userBox.IsEnabled = true;
                passBox.IsEnabled = true;
            }
        };
        ApplyProviderDefaults();

        DatabaseConnectionTestDto BuildTestDto() => new()
        {
            Provider = providerBox.SelectedItem as string ?? "SqlServer",
            Host = hostBox.Text.Trim(),
            Port = int.TryParse(portBox.Text.Trim(), out var port) ? port : null,
            Database = dbBox.Text.Trim(),
            Username = userBox.Text.Trim(),
            Password = passBox.Password,
            UseWindowsAuth = windowsAuth.IsChecked == true,
            ConnectionString = string.IsNullOrWhiteSpace(advanced.Text) ? null : advanced.Text.Trim()
        };

        DatabaseGrowDto BuildGrowDto() => new()
        {
            Provider = BuildTestDto().Provider,
            Host = BuildTestDto().Host,
            Port = BuildTestDto().Port,
            Database = BuildTestDto().Database,
            Username = BuildTestDto().Username,
            Password = BuildTestDto().Password,
            UseWindowsAuth = BuildTestDto().UseWindowsAuth,
            ConnectionString = BuildTestDto().ConnectionString,
            Mode = copyMode.IsChecked == true ? "CopyAndSwitch" : "EmptyAndSwitch",
            Confirmation = phraseBox.Text.Trim()
        };

        var testBtn = new Button
        {
            Content = "Test connection",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 0),
            MinWidth = 120
        };
        var growBtn = new Button
        {
            Content = "Grow database",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            Margin = new Thickness(0, 0, 8, 0),
            MinWidth = 120,
            IsEnabled = false
        };
        var cancelBtn = new Button
        {
            Content = "Cancel",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            IsCancel = true,
            MinWidth = 90
        };
        phraseBox.TextChanged += (_, _) =>
            growBtn.IsEnabled = string.Equals(phraseBox.Text.Trim(), "GROW DATABASE", StringComparison.Ordinal);

        var grown = false;
        testBtn.Click += async (_, _) =>
        {
            testBtn.IsEnabled = false;
            growBtn.IsEnabled = false;
            status.Text = "Testing connection…";
            status.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88));
            try
            {
                var result = await testAsync(BuildTestDto());
                if (result?.Ok == true)
                {
                    status.Text = result.Message + (string.IsNullOrWhiteSpace(result.Summary) ? "" : "\n" + result.Summary);
                    status.Foreground = new SolidColorBrush(Color.FromRgb(0x02, 0x78, 0x4A));
                }
                else
                {
                    status.Text = result?.Message ?? "Connection failed.";
                    status.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
                }
            }
            catch (Exception ex)
            {
                status.Text = CleanError(ex.Message);
                status.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
            }
            finally
            {
                testBtn.IsEnabled = true;
                growBtn.IsEnabled = string.Equals(phraseBox.Text.Trim(), "GROW DATABASE", StringComparison.Ordinal);
            }
        };

        growBtn.Click += async (_, _) =>
        {
            if (!string.Equals(phraseBox.Text.Trim(), "GROW DATABASE", StringComparison.Ordinal))
            {
                status.Text = "Type GROW DATABASE to confirm.";
                status.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
                return;
            }

            testBtn.IsEnabled = false;
            growBtn.IsEnabled = false;
            cancelBtn.IsEnabled = false;
            status.Text = "Growing database — copying data and switching…";
            status.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88));
            try
            {
                var result = await growAsync(BuildGrowDto());
                if (result?.Success == true)
                {
                    grown = true;
                    MessageBox.Show(dialog,
                        result.Message +
                        (string.IsNullOrWhiteSpace(result.BackupPath) ? "" : "\n\nBackup: " + result.BackupPath) +
                        "\n\nYou are now on " + result.Provider + ".",
                        "Database grown", MessageBoxButton.OK, MessageBoxImage.Information);
                    dialog.DialogResult = true;
                }
                else
                {
                    status.Text = result?.Message ?? "Grow failed.";
                    status.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
                }
            }
            catch (Exception ex)
            {
                status.Text = CleanError(ex.Message);
                status.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
            }
            finally
            {
                testBtn.IsEnabled = true;
                cancelBtn.IsEnabled = true;
                growBtn.IsEnabled = string.Equals(phraseBox.Text.Trim(), "GROW DATABASE", StringComparison.Ordinal);
            }
        };
        cancelBtn.Click += (_, _) => dialog.DialogResult = false;

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock
                {
                    Text = "Move Coalesce onto a larger database. Create an empty database on the target server first, then fill in the details below.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x48))
                },
                Label("Database type"),
                providerBox,
                Label("Host"),
                hostBox,
                Label("Port"),
                portBox,
                Label("Database name"),
                dbBox,
                windowsAuth,
                Label("Username"),
                userBox,
                Label("Password"),
                passBox,
                Label("What to do"),
                copyMode,
                emptyMode,
                Label("Advanced connection string (optional override)"),
                advanced,
                status,
                new TextBlock
                {
                    Text = "Type GROW DATABASE to enable the Grow button. A backup is taken automatically.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
                    Margin = new Thickness(0, 0, 0, 4)
                },
                phraseBox,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 16, 0, 0),
                    Children = { testBtn, growBtn, cancelBtn }
                }
            }
        };
        dialog.Content = scroll;
        dialog.ShowDialog();
        return grown;
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
        Margin = new Thickness(0, 0, 0, 4)
    };

    public static bool ConfirmDelete(Window owner, string label) =>
        MessageBox.Show(owner, $"Delete {label}?\nThis cannot be undone.", "Confirm delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public static bool Confirm(Window owner, string title, string message) =>
        MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static bool ConfirmTypedPhrase(Window? owner, string title, string message, string requiredPhrase)
    {
        var dialog = new Window
        {
            Title = title,
            Owner = owner,
            Width = 480,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Background = Brushes.White
        };
        var phraseBox = new TextBox { Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 4, 0, 0) };
        var ok = new Button
        {
            Content = "Confirm",
            Style = (Style)Application.Current.FindResource("DangerButton"),
            IsDefault = true,
            MinWidth = 100,
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = false
        };
        var cancel = new Button
        {
            Content = "Cancel",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            IsCancel = true,
            MinWidth = 90
        };
        phraseBox.TextChanged += (_, _) =>
            ok.IsEnabled = string.Equals(phraseBox.Text.Trim(), requiredPhrase, StringComparison.Ordinal);
        ok.Click += (_, _) => dialog.DialogResult = true;
        cancel.Click += (_, _) => dialog.DialogResult = false;

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) },
                new TextBlock
                {
                    Text = $"Type {requiredPhrase} exactly to confirm.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x78, 0x88)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                },
                phraseBox,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 16, 0, 0),
                    Children = { ok, cancel }
                }
            }
        };
        return dialog.ShowDialog() == true;
    }

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

    public static CrmLeadDto? EditCrmLead(Window owner, CrmLeadDto? existing)
    {
        var name = Field(existing?.Name ?? "");
        var company = Field(existing?.CompanyName ?? "");
        var email = Field(existing?.Email ?? "");
        var phone = Field(existing?.Phone ?? "");
        var source = Field(existing?.Source ?? "");
        var statusItems = new[] { "new", "working", "qualified", "disqualified", "converted" }
            .Select((s, i) => new ComboItem(i + 1, s)).ToList();
        var statusIdx = Math.Max(0, Array.IndexOf(new[] { "new", "working", "qualified", "disqualified", "converted" }, existing?.Status ?? "new"));
        var status = Combo(statusItems, statusIdx + 1);

        if (!Show(owner, existing is null ? "Add lead" : "Edit lead", 440,
                Row("Name", name), Row("Company", company), Row("Email", email),
                Row("Phone", phone), Row("Source", source), Row("Status", status)))
            return null;
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show("Name is required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        return new CrmLeadDto
        {
            Id = existing?.Id ?? 0,
            Name = name.Text.Trim(),
            CompanyName = NullIfEmpty(company.Text),
            Email = NullIfEmpty(email.Text),
            Phone = NullIfEmpty(phone.Text),
            Source = NullIfEmpty(source.Text),
            Status = SelectedLabel(status) ?? "new",
            OwnerUserId = existing?.OwnerUserId
        };
    }

    public static CrmAccountDto? EditCrmAccount(Window owner, CrmAccountDto? existing, IList<PartnerDto> customers)
    {
        var name = Field(existing?.Name ?? "");
        var industry = Field(existing?.Industry ?? "");
        var website = Field(existing?.Website ?? "");
        var email = Field(existing?.BillingEmail ?? "");
        var custItems = new List<ComboItem> { new ComboItem(0, "(none — link later)") };
        custItems.AddRange(customers.Select(c => new ComboItem(c.Id, c.Name)));
        var customer = Combo(custItems, existing?.CustomerId ?? 0);

        if (!Show(owner, existing is null ? "Add CRM account" : "Edit CRM account", 460,
                Row("Account name", name), Row("Industry", industry), Row("Website", website),
                Row("Billing email", email), Row("ERP customer", customer)))
            return null;
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show("Name is required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        return new CrmAccountDto
        {
            Id = existing?.Id ?? 0,
            Name = name.Text.Trim(),
            Industry = NullIfEmpty(industry.Text),
            Website = NullIfEmpty(website.Text),
            BillingEmail = NullIfEmpty(email.Text),
            CustomerId = SelectedId(customer) is int cid && cid > 0 ? cid : null,
            IsActive = existing?.IsActive ?? true,
            OwnerUserId = existing?.OwnerUserId
        };
    }

    public static CrmContactDto? EditCrmContact(Window owner, CrmContactDto? existing, IList<CrmAccountDto> accounts)
    {
        var first = Field(existing?.FirstName ?? "");
        var last = Field(existing?.LastName ?? "");
        var email = Field(existing?.Email ?? "");
        var phone = Field(existing?.Phone ?? "");
        var title = Field(existing?.Title ?? "");
        var accItems = new List<ComboItem> { new ComboItem(0, "(none)") };
        accItems.AddRange(accounts.Select(a => new ComboItem(a.Id, a.Name)));
        var account = Combo(accItems, existing?.AccountId ?? 0);
        var primary = new CheckBox { Content = "Primary contact", IsChecked = existing?.IsPrimary == true, Margin = new Thickness(0, 6, 0, 0) };

        if (!Show(owner, existing is null ? "Add contact" : "Edit contact", 440,
                Row("First name", first), Row("Last name", last), Row("Title", title),
                Row("Email", email), Row("Phone", phone), Row("Account", account), primary))
            return null;
        if (string.IsNullOrWhiteSpace(first.Text))
        {
            MessageBox.Show("First name is required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        return new CrmContactDto
        {
            Id = existing?.Id ?? 0,
            FirstName = first.Text.Trim(),
            LastName = NullIfEmpty(last.Text),
            Title = NullIfEmpty(title.Text),
            Email = NullIfEmpty(email.Text),
            Phone = NullIfEmpty(phone.Text),
            AccountId = SelectedId(account) is int aid && aid > 0 ? aid : null,
            IsPrimary = primary.IsChecked == true,
            IsActive = existing?.IsActive ?? true
        };
    }

    public static CrmOpportunityDto? EditCrmOpportunity(Window owner, CrmOpportunityDto? existing, IList<CrmAccountDto> accounts)
    {
        if (accounts.Count == 0)
        {
            MessageBox.Show("Create a CRM account first.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        var name = Field(existing?.Name ?? "");
        var amount = Field((existing?.Amount ?? 0).ToString(CultureInfo.InvariantCulture));
        var close = Field(existing?.ExpectedClose?.ToString("yyyy-MM-dd") ?? "");
        var stages = new[] { "prospecting", "qualified", "proposal", "negotiation", "won", "lost" };
        var stageItems = stages.Select((s, i) => new ComboItem(i + 1, s)).ToList();
        var stageIdx = Math.Max(0, Array.IndexOf(stages, existing?.Stage ?? "prospecting"));
        var stage = Combo(stageItems, stageIdx + 1);
        var accItems = accounts.Select(a => new ComboItem(a.Id, a.Name)).ToList();
        var account = Combo(accItems, existing?.AccountId > 0 ? existing.AccountId : accounts[0].Id);

        if (!Show(owner, existing is null ? "Add opportunity" : "Edit opportunity", 460,
                Row("Name", name), Row("Account", account), Row("Stage", stage),
                Row("Amount", amount), Row("Expected close (yyyy-MM-dd)", close)))
            return null;
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show("Name is required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        DateTime? expected = null;
        if (!string.IsNullOrWhiteSpace(close.Text) &&
            DateTime.TryParse(close.Text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            expected = d.Date;

        return new CrmOpportunityDto
        {
            Id = existing?.Id ?? 0,
            Name = name.Text.Trim(),
            AccountId = SelectedId(account) ?? accounts[0].Id,
            Stage = SelectedLabel(stage) ?? "prospecting",
            Amount = Dec(amount.Text),
            ExpectedClose = expected,
            OwnerUserId = existing?.OwnerUserId,
            SalesOrderId = existing?.SalesOrderId,
            LostReason = existing?.LostReason
        };
    }

    public static CrmActivityDto? EditCrmActivity(Window owner, CrmActivityDto? existing)
    {
        var subject = Field(existing?.Subject ?? "");
        var body = Field(existing?.Body ?? "");
        var due = Field(existing?.DueAt?.ToString("yyyy-MM-dd") ?? "");
        var types = new[] { "task", "call", "meeting", "email" };
        var typeItems = types.Select((s, i) => new ComboItem(i + 1, s)).ToList();
        var typeIdx = Math.Max(0, Array.IndexOf(types, existing?.ActivityType ?? "task"));
        var type = Combo(typeItems, typeIdx + 1);
        var statuses = new[] { "open", "done", "cancelled" };
        var statusItems = statuses.Select((s, i) => new ComboItem(i + 1, s)).ToList();
        var statusIdx = Math.Max(0, Array.IndexOf(statuses, existing?.Status ?? "open"));
        var status = Combo(statusItems, statusIdx + 1);

        if (!Show(owner, existing is null ? "Add activity" : "Edit activity", 440,
                Row("Type", type), Row("Subject", subject), Row("Details", body),
                Row("Due (yyyy-MM-dd)", due), Row("Status", status)))
            return null;
        if (string.IsNullOrWhiteSpace(subject.Text))
        {
            MessageBox.Show("Subject is required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        DateTime? dueAt = null;
        if (!string.IsNullOrWhiteSpace(due.Text) &&
            DateTime.TryParse(due.Text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            dueAt = d.Date;

        return new CrmActivityDto
        {
            Id = existing?.Id ?? 0,
            ActivityType = SelectedLabel(type) ?? "task",
            Subject = subject.Text.Trim(),
            Body = NullIfEmpty(body.Text),
            Status = SelectedLabel(status) ?? "open",
            DueAt = dueAt,
            AccountId = existing?.AccountId,
            ContactId = existing?.ContactId,
            LeadId = existing?.LeadId,
            OpportunityId = existing?.OpportunityId,
            OwnerUserId = existing?.OwnerUserId
        };
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
            MessageBox.Show("No roles are defined.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Username and password are required.", "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        MessageBox.Show(CleanError(ex.Message), "Coalesce", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private static string? SelectedLabel(ComboBox box) =>
        (box.SelectedItem as ComboItem)?.Label;

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
