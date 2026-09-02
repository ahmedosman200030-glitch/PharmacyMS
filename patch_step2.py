import re

AXAML = "src/PharmacyMS.Desktop/Views/Settings/SettingsView.axaml"
CS = "src/PharmacyMS.Desktop/Views/Settings/SettingsView.axaml.cs"

def replace_once(path, old, new):
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()
    count = content.count(old)
    if count != 1:
        raise SystemExit(f"ERROR in {path}: expected 1 match, found {count} for:\n{old[:120]}...")
    content = content.replace(old, new)
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"OK: patched {path} ({old[:50]!r}...)")

# --- AXAML edit A: nav item description ---
replace_once(AXAML,
'''                <TextBlock Text="Cloud Sync" FontSize="13" FontWeight="SemiBold"/>
                <TextBlock Text="Connect to Supabase/Postgres" FontSize="10.5" Foreground="#94A3B8"/>''',
'''                <TextBlock Text="Database Mode" FontSize="13" FontWeight="SemiBold"/>
                <TextBlock Text="Offline, Local Network, or Cloud" FontSize="10.5" Foreground="#94A3B8"/>''')

# --- AXAML edit B: checkbox -> mode combo, grid rename ---
replace_once(AXAML,
'''              <TextBlock Text="Cloud Sync" FontSize="16" FontWeight="Bold" Foreground="#0F172A"/>
              <TextBlock Text="Connect this PC to a shared Supabase/Postgres database instead of the local SQLite file." FontSize="12" Foreground="#64748B" TextWrapping="Wrap"/>

              <CheckBox x:Name="UseCloudDbCheck" Content="Use Cloud Database (Postgres)" FontSize="13"/>

              <Grid ColumnDefinitions="*,*" ColumnSpacing="16" RowDefinitions="Auto,Auto,Auto" RowSpacing="14">''',
'''              <TextBlock Text="Database Mode" FontSize="16" FontWeight="Bold" Foreground="#0F172A"/>
              <TextBlock Text="Choose how this PC connects to your pharmacy data: stay fully offline, share data with other PCs over your local network, or sync to the cloud." FontSize="12" Foreground="#64748B" TextWrapping="Wrap"/>

              <StackPanel Spacing="6">
                <TextBlock Text="Mode" FontSize="12" Foreground="#64748B"/>
                <ComboBox x:Name="DbModeCombo" Height="38" HorizontalAlignment="Stretch"/>
                <TextBlock x:Name="DbModeHintText" FontSize="11" Foreground="#94A3B8" TextWrapping="Wrap"/>
              </StackPanel>

              <Grid x:Name="PostgresFieldsGrid" ColumnDefinitions="*,*" ColumnSpacing="16" RowDefinitions="Auto,Auto,Auto" RowSpacing="14">''')

# --- CS edit C: wire up DbModeCombo in constructor ---
replace_once(CS,
'''        CloudSslModeCombo.ItemsSource = new[] { "Require", "Disable", "Prefer" };
        CloudSslModeCombo.SelectedIndex = 0;''',
'''        CloudSslModeCombo.ItemsSource = new[] { "Require", "Disable", "Prefer" };
        CloudSslModeCombo.SelectedIndex = 0;

        DbModeCombo.ItemsSource = new[] { "Offline (this PC only)", "Local Network (share with other PCs)", "Cloud (Supabase/Postgres)" };
        DbModeCombo.SelectionChanged += (_, _) => UpdateDbModeUi();''')

# --- CS edit D: LoadCloudSyncFields uses DbModeCombo + calls UpdateDbModeUi ---
replace_once(CS,
'''    private void LoadCloudSyncFields()
    {
        var cfg = new DbConfigService().Load();
        UseCloudDbCheck.IsChecked = cfg.Provider == DbProvider.Postgres;

        if (!string.IsNullOrWhiteSpace(cfg.PostgresConnectionString))
        {
            try
            {
                var b = new NpgsqlConnectionStringBuilder(cfg.PostgresConnectionString);
                CloudHostBox.Text = b.Host;
                CloudPortBox.Text = b.Port.ToString();
                CloudDatabaseBox.Text = b.Database;
                CloudUsernameBox.Text = b.Username;
                CloudPasswordBox.Text = b.Password;
                CloudSslModeCombo.SelectedItem = b.SslMode.ToString();
            }
            catch
            {
                // malformed/legacy connection string — leave fields blank for the user to re-enter
            }
        }
    }''',
'''    private void LoadCloudSyncFields()
    {
        var cfg = new DbConfigService().Load();
        DbModeCombo.SelectedIndex = cfg.NetworkMode switch
        {
            DbNetworkMode.LocalNetwork => 1,
            DbNetworkMode.Cloud => 2,
            _ => 0
        };

        if (!string.IsNullOrWhiteSpace(cfg.PostgresConnectionString))
        {
            try
            {
                var b = new NpgsqlConnectionStringBuilder(cfg.PostgresConnectionString);
                CloudHostBox.Text = b.Host;
                CloudPortBox.Text = b.Port.ToString();
                CloudDatabaseBox.Text = b.Database;
                CloudUsernameBox.Text = b.Username;
                CloudPasswordBox.Text = b.Password;
                CloudSslModeCombo.SelectedItem = b.SslMode.ToString();
            }
            catch
            {
                // malformed/legacy connection string — leave fields blank for the user to re-enter
            }
        }

        UpdateDbModeUi();
    }

    private void UpdateDbModeUi()
    {
        var mode = GetSelectedNetworkMode();
        var showPostgresFields = mode != DbNetworkMode.Offline;
        PostgresFieldsGrid.IsVisible = showPostgresFields;
        TestCloudConnectionButton.IsVisible = showPostgresFields;
        MigrateToCloudButton.IsVisible = showPostgresFields;

        DbModeHintText.Text = mode switch
        {
            DbNetworkMode.Offline => "This PC keeps its own local database. No other PC can see this data.",
            DbNetworkMode.LocalNetwork => "Connect to a PostgreSQL server on your local network (e.g. the pharmacy's main PC). Use its LAN IP address as Host, and set SSL Mode to Disable unless you have configured SSL yourself.",
            DbNetworkMode.Cloud => "Connect to a hosted Supabase/Postgres database over the internet.",
            _ => ""
        };
    }

    private DbNetworkMode GetSelectedNetworkMode() => DbModeCombo.SelectedIndex switch
    {
        1 => DbNetworkMode.LocalNetwork,
        2 => DbNetworkMode.Cloud,
        _ => DbNetworkMode.Offline
    };''')

# --- CS edit E: TestCloudConnectionAsync skips Supabase-only checks outside Cloud mode ---
replace_once(CS,
'''        var host = CloudHostBox.Text?.Trim() ?? "";
        if (host.StartsWith("db.", StringComparison.OrdinalIgnoreCase) && host.Contains(".supabase.co"))
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = "This looks like the direct-connection host, which usually won't resolve. " +
                "Use the pooler host instead, e.g. aws-0-<region>.pooler.supabase.com " +
                "(Supabase dashboard \\u2192 Project Settings \\u2192 Database \\u2192 Connection pooling). " +
                "Also set Username to postgres.<project-ref>, not just postgres.";
            TestCloudConnectionButton.IsEnabled = true;
            return;
        }

        var username = CloudUsernameBox.Text?.Trim() ?? "";
        if (username == "postgres" && host.Contains(".pooler.supabase.com"))
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = "When using the pooler host, Username must be postgres.<project-ref>, not just postgres.";
            TestCloudConnectionButton.IsEnabled = true;
            return;
        }''',
'''        var host = CloudHostBox.Text?.Trim() ?? "";
        var isCloudMode = GetSelectedNetworkMode() == DbNetworkMode.Cloud;

        if (isCloudMode && host.StartsWith("db.", StringComparison.OrdinalIgnoreCase) && host.Contains(".supabase.co"))
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = "This looks like the direct-connection host, which usually won't resolve. " +
                "Use the pooler host instead, e.g. aws-0-<region>.pooler.supabase.com " +
                "(Supabase dashboard \\u2192 Project Settings \\u2192 Database \\u2192 Connection pooling). " +
                "Also set Username to postgres.<project-ref>, not just postgres.";
            TestCloudConnectionButton.IsEnabled = true;
            return;
        }

        var username = CloudUsernameBox.Text?.Trim() ?? "";
        if (isCloudMode && username == "postgres" && host.Contains(".pooler.supabase.com"))
        {
            CloudSyncStatusText.Foreground = Avalonia.Media.Brush.Parse("#DC2626");
            CloudSyncStatusText.Text = "When using the pooler host, Username must be postgres.<project-ref>, not just postgres.";
            TestCloudConnectionButton.IsEnabled = true;
            return;
        }''')

# --- CS edit F: SaveCloudSyncAsync uses mode-derived Provider + NetworkMode ---
replace_once(CS,
'''            var cfg = new DbConfig
            {
                Provider = UseCloudDbCheck.IsChecked == true ? DbProvider.Postgres : DbProvider.Sqlite,
                PostgresConnectionString = BuildCloudConnectionString()
            };''',
'''            var mode = GetSelectedNetworkMode();
            var cfg = new DbConfig
            {
                Provider = mode == DbNetworkMode.Offline ? DbProvider.Sqlite : DbProvider.Postgres,
                NetworkMode = mode,
                PostgresConnectionString = mode == DbNetworkMode.Offline ? null : BuildCloudConnectionString()
            };''')

print("\nAll patches applied successfully.")
