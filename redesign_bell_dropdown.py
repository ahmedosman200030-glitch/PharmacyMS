#!/usr/bin/env python3
"""
Redesigns the header bell dropdown in MainShell.axaml: colored icon chips per row,
'Mark all as read' and 'View All Notifications' footer, matching the reference design.
Run from the PharmacyMS project root:  python3 redesign_bell_dropdown.py
"""
import sys

PATH = "src/PharmacyMS.Desktop/Views/Shell/MainShell.axaml"

OLD_FLYOUT = '''              <Button.Flyout>
                <Flyout Placement="BottomEdgeAlignedRight">
                  <StackPanel Width="320" Spacing="10">
                    <TextBlock x:Name="NotifHeaderText" Text="🔔 Notifications" FontWeight="Bold" FontSize="14"/>
                    <Separator/>

                    <StackPanel x:Name="NotifCriticalSection" Spacing="6">
                      <TextBlock Text="🔴 Critical" FontWeight="SemiBold" FontSize="12" Foreground="#EF4444"/>
                      <Button x:Name="NotifExpiredButton" Classes="notifRow" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" IsVisible="False">
                        <TextBlock x:Name="NotifExpiredText" Text="⚠ 0 medicines have expired" FontSize="12"/>
                      </Button>
                      <Button x:Name="NotifOutOfStockButton" Classes="notifRow" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" IsVisible="False">
                        <TextBlock x:Name="NotifOutOfStockText" Text="⚠ 0 medicines are out of stock" FontSize="12"/>
                      </Button>
                    </StackPanel>

                    <StackPanel x:Name="NotifWarningSection" Spacing="6">
                      <TextBlock Text="🟠 Warnings" FontWeight="SemiBold" FontSize="12" Foreground="#F59E0B"/>
                      <Button x:Name="NotifLowStockButton" Classes="notifRow" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" IsVisible="False">
                        <TextBlock x:Name="NotifLowStockText" Text="📦 0 medicines are low stock" FontSize="12"/>
                      </Button>
                      <Button x:Name="NotifExpiringButton" Classes="notifRow" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" IsVisible="False">
                        <TextBlock x:Name="NotifExpiringText" Text="⏳ 0 medicines expire within 30 days" FontSize="12"/>
                      </Button>
                    </StackPanel>

                    <StackPanel x:Name="NotifBusinessSection" Spacing="6">
                      <TextBlock Text="🟢 Business" FontWeight="SemiBold" FontSize="12" Foreground="#DC2626"/>
                      <TextBlock x:Name="NotifRevenueText" Text="💰 Today's sales: $0.00" FontSize="12" Margin="8,4"/>
                      <Button x:Name="NotifNewCustomersButton" Classes="notifRow" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" IsVisible="False">
                        <TextBlock x:Name="NotifNewCustomersText" Text="👥 0 new customers today" FontSize="12"/>
                      </Button>
                    </StackPanel>

                    <TextBlock x:Name="NotifEmptyText" Text="✅ No alerts right now" FontSize="12" Foreground="#64748B" IsVisible="False"/>
                    <Separator/>
                  </StackPanel>
                </Flyout>
              </Button.Flyout>'''

NEW_FLYOUT = '''              <Button.Flyout>
                <Flyout Placement="BottomEdgeAlignedRight">
                  <Border Width="340" Background="White" CornerRadius="10" Padding="0">
                    <StackPanel Spacing="0">

                      <!-- Header -->
                      <Grid ColumnDefinitions="*,Auto" Margin="16,14,16,10">
                        <TextBlock x:Name="NotifHeaderText" Grid.Column="0" Text="Notifications"
                                   FontWeight="Bold" FontSize="15" Foreground="#0F172A" VerticalAlignment="Center"/>
                        <TextBlock Grid.Column="1" Text="Mark all as read" FontSize="11.5"
                                   Foreground="#DC2626" VerticalAlignment="Center" Cursor="Hand"/>
                      </Grid>
                      <Separator Background="#F1F5F9"/>

                      <StackPanel Margin="10,10,10,4" Spacing="4" MaxHeight="380">
                        <ScrollViewer MaxHeight="380">
                          <StackPanel Spacing="2">

                            <Button x:Name="NotifExpiredButton" Classes="notifRowModern" HorizontalAlignment="Stretch" IsVisible="False">
                              <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                <Border Grid.Column="0" Width="36" Height="36" CornerRadius="8" Background="#FEE2E2" VerticalAlignment="Top">
                                  <TextBlock Text="⚠️" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>
                                <StackPanel Grid.Column="1" Spacing="1">
                                  <TextBlock Text="Expired Medicines" FontSize="13" FontWeight="SemiBold" Foreground="#0F172A"/>
                                  <TextBlock x:Name="NotifExpiredText" Text="0 medicines have expired" FontSize="11.5" Foreground="#64748B" TextWrapping="Wrap"/>
                                </StackPanel>
                              </Grid>
                            </Button>

                            <Button x:Name="NotifOutOfStockButton" Classes="notifRowModern" HorizontalAlignment="Stretch" IsVisible="False">
                              <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                <Border Grid.Column="0" Width="36" Height="36" CornerRadius="8" Background="#FEE2E2" VerticalAlignment="Top">
                                  <TextBlock Text="🚫" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>
                                <StackPanel Grid.Column="1" Spacing="1">
                                  <TextBlock Text="Out of Stock" FontSize="13" FontWeight="SemiBold" Foreground="#0F172A"/>
                                  <TextBlock x:Name="NotifOutOfStockText" Text="0 medicines are out of stock" FontSize="11.5" Foreground="#64748B" TextWrapping="Wrap"/>
                                </StackPanel>
                              </Grid>
                            </Button>

                            <Button x:Name="NotifLowStockButton" Classes="notifRowModern" HorizontalAlignment="Stretch" IsVisible="False">
                              <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                <Border Grid.Column="0" Width="36" Height="36" CornerRadius="8" Background="#FEF3C7" VerticalAlignment="Top">
                                  <TextBlock Text="📦" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>
                                <StackPanel Grid.Column="1" Spacing="1">
                                  <TextBlock Text="Low Stock Alert" FontSize="13" FontWeight="SemiBold" Foreground="#0F172A"/>
                                  <TextBlock x:Name="NotifLowStockText" Text="0 medicines are low stock" FontSize="11.5" Foreground="#64748B" TextWrapping="Wrap"/>
                                </StackPanel>
                              </Grid>
                            </Button>

                            <Button x:Name="NotifExpiringButton" Classes="notifRowModern" HorizontalAlignment="Stretch" IsVisible="False">
                              <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                <Border Grid.Column="0" Width="36" Height="36" CornerRadius="8" Background="#FEF3C7" VerticalAlignment="Top">
                                  <TextBlock Text="⏳" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>
                                <StackPanel Grid.Column="1" Spacing="1">
                                  <TextBlock Text="Expiry Alert" FontSize="13" FontWeight="SemiBold" Foreground="#0F172A"/>
                                  <TextBlock x:Name="NotifExpiringText" Text="0 medicines expire within 30 days" FontSize="11.5" Foreground="#64748B" TextWrapping="Wrap"/>
                                </StackPanel>
                              </Grid>
                            </Button>

                            <Border x:Name="NotifRevenueRow" Padding="10,10" CornerRadius="8">
                              <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                <Border Grid.Column="0" Width="36" Height="36" CornerRadius="8" Background="#DCFCE7" VerticalAlignment="Top">
                                  <TextBlock Text="💰" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>
                                <StackPanel Grid.Column="1" Spacing="1" VerticalAlignment="Center">
                                  <TextBlock Text="Today's Sales" FontSize="13" FontWeight="SemiBold" Foreground="#0F172A"/>
                                  <TextBlock x:Name="NotifRevenueText" Text="$0.00" FontSize="11.5" Foreground="#64748B"/>
                                </StackPanel>
                              </Grid>
                            </Border>

                            <Button x:Name="NotifNewCustomersButton" Classes="notifRowModern" HorizontalAlignment="Stretch" IsVisible="False">
                              <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                                <Border Grid.Column="0" Width="36" Height="36" CornerRadius="8" Background="#EDE9FE" VerticalAlignment="Top">
                                  <TextBlock Text="👥" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>
                                <StackPanel Grid.Column="1" Spacing="1">
                                  <TextBlock Text="New Customers" FontSize="13" FontWeight="SemiBold" Foreground="#0F172A"/>
                                  <TextBlock x:Name="NotifNewCustomersText" Text="0 new customers today" FontSize="11.5" Foreground="#64748B" TextWrapping="Wrap"/>
                                </StackPanel>
                              </Grid>
                            </Button>

                            <TextBlock x:Name="NotifEmptyText" Text="✅ No alerts right now" FontSize="12.5"
                                       Foreground="#64748B" IsVisible="False" Margin="10,20" HorizontalAlignment="Center"/>

                          </StackPanel>
                        </ScrollViewer>
                      </StackPanel>

                      <Separator Background="#F1F5F9"/>
                      <Button x:Name="ViewAllNotificationsButton" Content="View All Notifications" HorizontalAlignment="Stretch"
                              HorizontalContentAlignment="Center" Background="Transparent" Foreground="#DC2626"
                              FontSize="12.5" FontWeight="SemiBold" Padding="0,12" Cursor="Hand"/>

                    </StackPanel>
                  </Border>
                </Flyout>
              </Button.Flyout>'''

STYLE_ANCHOR = '''    <Style Selector="Button.navitem">'''
STYLE_INSERT = '''    <Style Selector="Button.notifRowModern">
      <Setter Property="Background" Value="Transparent"/>
      <Setter Property="BorderThickness" Value="0"/>
      <Setter Property="CornerRadius" Value="8"/>
      <Setter Property="Padding" Value="10,10"/>
      <Setter Property="HorizontalContentAlignment" Value="Left"/>
    </Style>
    <Style Selector="Button.notifRowModern:pointerover">
      <Setter Property="Background" Value="#F8FAFC"/>
    </Style>
    <Style Selector="Button.navitem">'''

def main():
    with open(PATH, "r", encoding="utf-8") as f:
        content = f.read()

    if OLD_FLYOUT not in content:
        print("ERROR: could not find the exact old flyout block. It may already be edited, or whitespace differs.")
        print("Searching for a shorter unique fragment instead...")
        fragment = 'x:Name="NotifCriticalSection"'
        if fragment in content:
            idx = content.index(fragment)
            print("Found fragment at char index", idx, "- context around it:")
            print(content[max(0, idx-200):idx+200])
        else:
            print("Fragment not found either - the flyout may already be redesigned.")
        sys.exit(1)

    content = content.replace(OLD_FLYOUT, NEW_FLYOUT, 1)

    if STYLE_ANCHOR not in content:
        print("WARNING: could not find style anchor - notifRowModern style not inserted. Rows will have no hover effect but should still render.")
    else:
        content = content.replace(STYLE_ANCHOR, STYLE_INSERT, 1)

    with open(PATH, "w", encoding="utf-8") as f:
        f.write(content)
    print("Patched:", PATH)

if __name__ == "__main__":
    main()
