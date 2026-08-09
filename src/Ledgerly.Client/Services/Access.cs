using System;
using System.Windows;
using System.Windows.Media;

namespace Ledgerly.Client.Services;

/// <summary>
/// Attached property to show/hide UI elements by the signed-in user's role permissions.
/// Usage: local:Access.Permission="inventory"
/// </summary>
public static class Access
{
    public static readonly DependencyProperty PermissionProperty =
        DependencyProperty.RegisterAttached(
            "Permission",
            typeof(string),
            typeof(Access),
            new PropertyMetadata(null, OnPermissionChanged));

    public static void SetPermission(DependencyObject element, string? value) =>
        element.SetValue(PermissionProperty, value);

    public static string? GetPermission(DependencyObject element) =>
        (string?)element.GetValue(PermissionProperty);

    private static void OnPermissionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement el)
            Apply(el, e.NewValue as string);
    }

    public static void Apply(UIElement element, string? permission)
    {
        if (string.IsNullOrWhiteSpace(permission)) return;
        var allowed = Session.CanAny(permission!);
        element.Visibility = allowed ? Visibility.Visible : Visibility.Collapsed;
        element.IsEnabled = allowed;
    }

    /// <summary>Re-evaluate every element in the tree that has Access.Permission set.</summary>
    public static void ApplyTree(DependencyObject? root)
    {
        if (root == null) return;

        if (root is UIElement el)
        {
            var perm = GetPermission(root);
            if (!string.IsNullOrWhiteSpace(perm))
                Apply(el, perm);
        }

        // VisualTreeHelper only accepts Visual / Visual3D — not ColumnDefinition, etc.
        if (root is Visual || root is System.Windows.Media.Media3D.Visual3D)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
                ApplyTree(VisualTreeHelper.GetChild(root, i));
        }

        // Logical walk for content not yet in the visual tree; skip non-visual defs (Grid columns/rows).
        if (root is FrameworkElement fe)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(fe))
            {
                if (child is Visual || child is FrameworkElement)
                    ApplyTree((DependencyObject)child);
            }
        }
    }

    public static bool Ensure(string permission, string? actionLabel = null)
    {
        if (Session.Can(permission)) return true;
        var detail = permission is "users" or "admin"
            ? "Only an Administrator can manage users and assign access levels."
            : $"You do not have permission to {(actionLabel ?? permission)}.\nRequired: {permission}";
        MessageBox.Show(detail, "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    /// <summary>Map a MainWindow nav tag to the permission that gates it.</summary>
    public static string PermissionForPage(string page) => page switch
    {
        "suppliers" or "customers" => "partners",
        "password" => "", // any signed-in user
        "users" => "users",
        _ => page
    };
}
