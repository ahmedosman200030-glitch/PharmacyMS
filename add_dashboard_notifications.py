#!/usr/bin/env python3
"""
Adds a 'Dashboard Notifications' panel (row 6) to DashboardView.axaml + .axaml.cs.
Run from the PharmacyMS project root:  python3 add_dashboard_notifications.py
"""
import re, sys

AXAML_PATH = "src/PharmacyMS.Desktop/Views/Dashboard/DashboardView.axaml"
CS_PATH = "src/PharmacyMS.Desktop/Views/Dashboard/DashboardView.axaml.cs"

def patch_axaml():
    with open(AXAML_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    old_rows = 'RowDefinitions="Auto,Auto,1.15*,*,Auto"'
    new_rows = 'RowDefinitions="Auto,Auto,1.15*,*,Auto,Auto"'
    if old_rows not in content:
        print("ERROR: could not find RowDefinitions anchor in .axaml - aborting.")
        sys.exit(1)
    content = content.replace(old_rows, new_rows, 1)

    anchor = """                        </StackPanel>
                    </Panel>
                </Border>
            </Grid>

        </Grid>
    </Grid>

</UserControl>"""
    if anchor not in content:
        print("ERROR: could not find insertion anchor in .axaml - aborting.")
        sys.exit(1)

    new_row5_and_close = """                        </StackPanel>
                    </Panel>
                </Border>
            </Grid>

            <!-- Dashboard Notifications -->
            <Border Grid.Row="5" Classes="card" Padding="0" Margin="0,10,0,0">
                <Grid RowDefinitions="Auto,*">
                    <Border Grid.Row="0" Background="{StaticResource RoseHeaderGradient}" CornerRadius="20,20,0,0" Padding="14,10">
                        <Grid ColumnDefinitions="*,Auto">
                            <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="8">
                                <TextBlock Text="&#128276;" FontSize="16" VerticalAlignment="Center"/>
                                <TextBlock Classes="header-title" Text="Dashboard Notifications"/>
                            </StackPanel>
                            <TextBlock Grid.Column="1" x:Name="ViewAllDashNotifText" Classes="header-view-all" Text="View all" Cursor="Hand"/>
                        </Grid>
                    </Border>
                    <ItemsControl Grid.Row="1" x:Name="DashboardNotifList" Margin="6,6">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate x:DataType="vm:DashNotifRow">
                                <Grid ColumnDefinitions="Auto,*,Auto" Margin="8,6">
                                    <Border Grid.Column="0" Width="38" Height="38" CornerRadius="10"
                                            Background="{Binding IconBg}" Margin="0,0,12,0">
                                        <TextBlock Text="{Binding Icon}" FontSize="16"
                                                   HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                    </Border>
                                    <StackPanel Grid.Column="1" VerticalAlignment="Center" Spacing="1">
                                        <TextBlock Text="{Binding Title}" FontSize="13" FontWeight="SemiBold" Foreground="#111827"/>
                                        <TextBlock Text="{Binding Subtitle}" FontSize="11" Foreground="#9CA3AF" TextWrapping="Wrap"/>
                                    </StackPanel>
                                    <Border Grid.Column="2" Background="{Binding BadgeBg}" CornerRadius="8"
                                            Padding="8,3" VerticalAlignment="Center" IsVisible="{Binding HasBadge}">
                                        <TextBlock Text="{Binding BadgeText}" FontSize="10" FontWeight="Bold"
                                                   Foreground="{Binding BadgeFg}"/>
                                    </Border>
                                </Grid>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    <TextBlock x:Name="DashNotifEmptyText" Grid.Row="1" Text="No alerts right now"
                               FontSize="12.5" Foreground="#9CA3AF" HorizontalAlignment="Center"
                               Margin="0,20" IsVisible="False"/>
                </Grid>
            </Border>

        </Grid>
    </Grid>

</UserControl>"""

    content = content.replace(anchor, new_row5_and_close, 1)

    # Wrap the whole thing in a ScrollViewer so the extra row doesn't get clipped off-screen.
    old_outer = '    <Grid Background="{StaticResource PageGradient}">\n        <Grid Margin="14,12,14,14" RowDefinitions="Auto,Auto,1.15*,*,Auto,Auto">'
    new_outer = '    <ScrollViewer Background="{StaticResource PageGradient}">\n      <Grid>\n        <Grid Margin="14,12,14,14" RowDefinitions="Auto,Auto,1.15*,*,Auto,Auto">'
    if old_outer in content:
        content = content.replace(old_outer, new_outer, 1)
        # close the extra wrappers right before the final closing tags
        content = content.replace(
            "        </Grid>\n    </Grid>\n\n</UserControl>",
            "        </Grid>\n      </Grid>\n    </ScrollViewer>\n\n</UserControl>",
            1
        )
    else:
        print("WARNING: outer Grid wrapper anchor not found - skipping ScrollViewer wrap (row 6 may be clipped). You can add scrolling manually if needed.")

    with open(AXAML_PATH, "w", encoding="utf-8") as f:
        f.write(content)
    print("Patched:", AXAML_PATH)


def patch_cs():
    with open(CS_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # Add missing usings
    if "using Microsoft.Extensions.DependencyInjection;" not in content:
        content = content.replace(
            "using PharmacyMS.Desktop.ViewModels;",
            "using Microsoft.Extensions.DependencyInjection;\nusing PharmacyMS.Application.Interfaces.Repositories;\nusing PharmacyMS.Desktop.ViewModels;",
            1
        )

    # Add DashNotifRow model class + LoadDashboardNotificationsAsync method,
    # and call it from LoadAsync().
    old_load_call = "        DrawSalesChart();\n        DrawPaymentDonut();"
    new_load_call = "        DrawSalesChart();\n        DrawPaymentDonut();\n        await LoadDashboardNotificationsAsync();"
    if old_load_call not in content:
        print("ERROR: could not find LoadAsync anchor in .axaml.cs - aborting.")
        sys.exit(1)
    content = content.replace(old_load_call, new_load_call, 1)

    # Insert the new method + model class right before the final closing brace of the class.
    insertion = '''
    private async Task LoadDashboardNotificationsAsync()
    {
        try
        {
            var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
            var allMedicines = (await medicineRepo.GetAllAsync()).ToList();

            var expiredCount = allMedicines.Count(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date < DateTime.Today);
            var outOfStockCount = allMedicines.Count(m => m.QuantityInStock == 0);
            var lowStockCount = allMedicines.Count(m => m.QuantityInStock > 0 && m.QuantityInStock <= m.ReorderLevel);
            var expiringCount = allMedicines.Count(m =>
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value.Date >= DateTime.Today &&
                m.ExpiryDate.Value.Date <= DateTime.Today.AddDays(30));

            var rows = new List<DashNotifRow>();

            if (lowStockCount > 0)
                rows.Add(new DashNotifRow
                {
                    Icon = "\\U0001F4E6", IconBg = "#FEF3C7",
                    Title = "Low Stock Alert",
                    Subtitle = $"{lowStockCount} product{(lowStockCount == 1 ? "" : "s")} running low on stock.",
                    BadgeText = lowStockCount.ToString(), BadgeBg = "#FEF3C7", BadgeFg = "#D97706", HasBadge = true
                });

            if (expiringCount > 0)
                rows.Add(new DashNotifRow
                {
                    Icon = "\\u23F3", IconBg = "#FEF3C7",
                    Title = "Expiry Alert",
                    Subtitle = $"{expiringCount} product{(expiringCount == 1 ? "" : "s")} will expire within 30 days.",
                    BadgeText = expiringCount.ToString(), BadgeBg = "#FEF3C7", BadgeFg = "#D97706", HasBadge = true
                });

            if (expiredCount > 0)
                rows.Add(new DashNotifRow
                {
                    Icon = "\\u26A0", IconBg = "#FEE2E2",
                    Title = "Expired Medicines",
                    Subtitle = $"{expiredCount} product{(expiredCount == 1 ? "" : "s")} have already expired.",
                    BadgeText = expiredCount.ToString(), BadgeBg = "#FEE2E2", BadgeFg = "#DC2626", HasBadge = true
                });

            if (outOfStockCount > 0)
                rows.Add(new DashNotifRow
                {
                    Icon = "\\uD83D\\uDEAB", IconBg = "#FEE2E2",
                    Title = "Out of Stock",
                    Subtitle = $"{outOfStockCount} product{(outOfStockCount == 1 ? "" : "s")} are out of stock.",
                    BadgeText = outOfStockCount.ToString(), BadgeBg = "#FEE2E2", BadgeFg = "#DC2626", HasBadge = true
                });

            if (_vm.NewCustomersToday > 0)
                rows.Add(new DashNotifRow
                {
                    Icon = "\\uD83D\\uDC65", IconBg = "#EDE9FE",
                    Title = "New Customer Registered",
                    Subtitle = $"{_vm.NewCustomersToday} new customer{(_vm.NewCustomersToday == 1 ? "" : "s")} today.",
                    BadgeText = _vm.NewCustomersToday.ToString(), BadgeBg = "#EDE9FE", BadgeFg = "#7C3AED", HasBadge = true
                });

            DashboardNotifList.ItemsSource = rows;
            DashNotifEmptyText.IsVisible = rows.Count == 0;
        }
        catch
        {
            // dashboard notifications are non-critical; fail silently
        }
    }
}

public class DashNotifRow
{
    public string Icon { get; set; } = "";
    public string IconBg { get; set; } = "#F1F5F9";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string BadgeText { get; set; } = "";
    public string BadgeBg { get; set; } = "#F1F5F9";
    public string BadgeFg { get; set; } = "#334155";
    public bool HasBadge { get; set; }
}'''

    # Replace the final "}" of the class (the very last closing brace in the file) with our insertion.
    idx = content.rstrip().rfind("}")
    if idx == -1:
        print("ERROR: could not find final closing brace in .axaml.cs - aborting.")
        sys.exit(1)
    content = content.rstrip()[:idx] + insertion + "\n"

    with open(CS_PATH, "w", encoding="utf-8") as f:
        f.write(content)
    print("Patched:", CS_PATH)


if __name__ == "__main__":
    patch_axaml()
    patch_cs()
    print("\nDone. Now run: dotnet run --project src/PharmacyMS.Desktop/PharmacyMS.Desktop.csproj")
