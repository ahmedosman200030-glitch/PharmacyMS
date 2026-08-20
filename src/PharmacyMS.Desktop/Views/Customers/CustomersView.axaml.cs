using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClosedXML.Excel;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Customers;

public partial class CustomersView : UserControl
{
    private readonly CustomersViewModel _vm;
    public CustomersView() { InitializeComponent(); }
    public CustomersView(CustomersViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Grid.ItemsSource = _vm.Customers;
        Grid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        AttachedToVisualTree += async (_, _) => await _vm.LoadAsync();

        AddButton.Click += async (_, _) =>
        {
            var form = new CustomerFormWindow();
            var result = await form.ShowDialog<Customer?>(TopLevel.GetTopLevel(this) as Avalonia.Controls.Window);
            if (result != null) await _vm.AddAsync(result);
        };

        EditButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is not Customer c) return;
            var form = new CustomerFormWindow(c);
            var result = await form.ShowDialog<Customer?>(TopLevel.GetTopLevel(this) as Avalonia.Controls.Window);
            if (result != null) await _vm.UpdateAsync(result);
        };

        DeleteButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is Customer c && await ConfirmDeleteAsync(c.Name))
                await _vm.DeleteAsync(c);
        };


        ImportExcelButton.Click += async (_, _) => await ImportExcelAsync();
        ExportExcelButton.Click += async (_, _) => await ExportExcelAsync();
    }

    private async Task<bool> ConfirmDeleteAsync(string itemName)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dialog = new Window
        {
            Title = "Confirm Delete",
            Width = 380,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = $"Delete \"{itemName}\"? This cannot be undone.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 10,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children =
                        {
                            new Button { Content = "Cancel", Padding = new Avalonia.Thickness(14,6) },
                            new Button { Content = "Delete", Padding = new Avalonia.Thickness(14,6), Background = Avalonia.Media.Brushes.Crimson, Foreground = Avalonia.Media.Brushes.White }
                        }
                    }
                }
            }
        };

        var panel = (StackPanel)dialog.Content!;
        var buttonRow = (StackPanel)panel.Children[1];
        var cancelBtn = (Button)buttonRow.Children[0];
        var deleteBtn = (Button)buttonRow.Children[1];

        cancelBtn.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        deleteBtn.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        await dialog.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        return await tcs.Task;
    }


    private static int LevenshteinDistance(string a, string b)
    {
        a = a.ToLowerInvariant(); b = b.ToLowerInvariant();
        var dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                                     dp[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return dp[a.Length, b.Length];
    }

    private static bool IsSimilarName(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return false; // handled as exact match elsewhere
        int dist = LevenshteinDistance(a, b);
        int threshold = Math.Max(1, Math.Min(a.Length, b.Length) / 6); // roughly 1 edit per 6 chars, min 1
        return dist <= threshold;
    }

    private async Task ImportExcelAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Customers from Excel",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Excel Files") { Patterns = new[] { "*.xlsx", "*.xls" } } }
        });
        if (files == null || files.Count == 0) return;

        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in ws.Row(1).CellsUsed())
            colMap[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        int Col(params string[] names) { foreach (var n in names) if (colMap.TryGetValue(n, out var c)) return c; return -1; }

        var nameCol = Col("Name");
        var phoneCol = Col("Phone");
        var emailCol = Col("Email");
        var addressCol = Col("Address");
        if (nameCol == -1)
        {
            await ShowMessage("Import Result", "No 'Name' column found in the file — cannot import.");
            return;
        }

        int added = 0, skipped = 0;
        var flagged = new List<string>();
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(nameCol).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            await _vm.AddAsync(new Customer
            {
                Name = name,
                Phone = phoneCol != -1 ? row.Cell(phoneCol).GetString().Trim() : null,
                Email = emailCol != -1 ? row.Cell(emailCol).GetString().Trim() : null,
                Address = addressCol != -1 ? row.Cell(addressCol).GetString().Trim() : null
            });
            if (_vm.Customers.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                skipped++;
                continue;
            }
            var similar = _vm.Customers.FirstOrDefault(x => IsSimilarName(x.Name, name));
            if (similar != null)
            {
                flagged.Add($"\"{name}\" (similar to existing \"{similar.Name}\")");
                continue;
            }
            added++;
        }

        var summary = $"Import complete!\nAdded: {added}\nSkipped (exact duplicate): {skipped}\nFlagged (similar name, not imported): {flagged.Count}";
        if (flagged.Count > 0)
            summary += "\n\n" + string.Join("\n", flagged);
        await ShowMessage("Import Result", summary);
    }

    private async Task ExportExcelAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Customers",
            SuggestedFileName = $"customers-{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExtension = "xlsx",
            FileTypeChoices = new[] { new FilePickerFileType("Excel File") { Patterns = new[] { "*.xlsx" } } }
        });
        if (file == null) return;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Customers");
        ws.Cell(1, 1).Value = "Name"; ws.Cell(1, 2).Value = "Phone"; ws.Cell(1, 3).Value = "Email"; ws.Cell(1, 4).Value = "Address";
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#8B0000");
        ws.Row(1).Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var c in _vm.Customers.OrderBy(x => x.Name))
        {
            ws.Cell(row, 1).Value = c.Name;
            ws.Cell(row, 2).Value = c.Phone;
            ws.Cell(row, 3).Value = c.Email;
            ws.Cell(row, 4).Value = c.Address;
            row++;
        }
        ws.Columns().AdjustToContents();

        using var stream = await file.OpenWriteAsync();
        wb.SaveAs(stream);
    }

    private async Task ShowMessage(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock { Text = message, Margin = new Avalonia.Thickness(20), TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        };
        await dialog.ShowDialog(TopLevel.GetTopLevel(this) as Window);
    }
    public async void ApproveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: PharmacyMS.Domain.Entities.Customer customer })
            await _vm.ApproveAsync(customer);
    }

    public async void RejectButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: PharmacyMS.Domain.Entities.Customer customer })
            await _vm.RejectAsync(customer);
    }
}