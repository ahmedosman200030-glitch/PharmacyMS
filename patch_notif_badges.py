#!/usr/bin/env python3
"""
Adds a colored count badge to each notification dropdown row
(Expired, Out of Stock, Low Stock, Expiring, New Customers) in
MainShell.axaml, and wires the counts through from code-behind.

Run from the repo root:
    python3 patch_notif_badges.py
"""
import re
import sys
from pathlib import Path

XAML_PATH = Path("src/PharmacyMS.Desktop/Views/Shell/MainShell.axaml")
CS_PATH = Path("src/PharmacyMS.Desktop/Views/Shell/MainShell.axaml.cs")

# (button x:Name, badge x:Name, badge background color)
ROWS = [
    ("NotifExpiredButton", "NotifExpiredCountBadge", "#EF4444"),
    ("NotifOutOfStockButton", "NotifOutOfStockCountBadge", "#EF4444"),
    ("NotifLowStockButton", "NotifLowStockCountBadge", "#F59E0B"),
    ("NotifExpiringButton", "NotifExpiringCountBadge", "#F59E0B"),
    ("NotifNewCustomersButton", "NotifNewCustomersCountBadge", "#8B5CF6"),
]

BADGE_TEMPLATE = """                                <Border Grid.Column="2" Background="{color}" CornerRadius="10"
                                        MinWidth="20" Height="20" Padding="5,0" VerticalAlignment="Top">
                                  <TextBlock x:Name="{badge_name}" Text="0" FontSize="10" FontWeight="Bold"
                                             Foreground="White" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>
"""


def patch_xaml(text: str) -> str:
    for button_name, badge_name, color in ROWS:
        pattern = re.compile(
            rf'(<Button x:Name="{button_name}".*?)(</Grid>\s*</Button>)',
            re.DOTALL,
        )
        match = pattern.search(text)
        if not match:
            print(f"ERROR: could not find block for {button_name} in {XAML_PATH}", file=sys.stderr)
            sys.exit(1)

        block = match.group(1)
        closing = match.group(2)

        if f'x:Name="{badge_name}"' in block:
            print(f"SKIP: {badge_name} already present, leaving {button_name} untouched")
            continue

        old_columns = 'ColumnDefinitions="Auto,*" ColumnSpacing="12"'
        new_columns = 'ColumnDefinitions="Auto,*,Auto" ColumnSpacing="10"'
        if old_columns not in block:
            print(f"ERROR: expected column definition not found in {button_name} block", file=sys.stderr)
            sys.exit(1)
        block = block.replace(old_columns, new_columns, 1)

        badge_xml = BADGE_TEMPLATE.format(color=color, badge_name=badge_name)
        new_block = block + badge_xml + "                              "

        text = text[: match.start()] + new_block + closing + text[match.end():]

    return text


# (existing IsVisible line to anchor after, badge name, code expression for the count)
CS_INSERTS = [
    ("NotifExpiredButton.IsVisible = expiredCount > 0;", "NotifExpiredCountBadge", "expiredCount"),
    ("NotifOutOfStockButton.IsVisible = outOfStockCount > 0;", "NotifOutOfStockCountBadge", "outOfStockCount"),
    ("NotifLowStockButton.IsVisible = lowStock > 0;", "NotifLowStockCountBadge", "lowStock"),
    ("NotifExpiringButton.IsVisible = expiring > 0;", "NotifExpiringCountBadge", "expiring"),
    ("NotifNewCustomersButton.IsVisible = newCustomersToday > 0;", "NotifNewCustomersCountBadge", "newCustomersToday"),
]


def patch_codebehind(text: str) -> str:
    for anchor, badge_name, count_expr in CS_INSERTS:
        if f"{badge_name}.Text" in text:
            print(f"SKIP: {badge_name}.Text already set, leaving code-behind untouched for this row")
            continue
        if anchor not in text:
            print(f"ERROR: could not find anchor line in {CS_PATH}:\n  {anchor}", file=sys.stderr)
            sys.exit(1)
        replacement = f"{anchor}\n            {badge_name}.Text = {count_expr}.ToString();"
        text = text.replace(anchor, replacement, 1)
    return text


def main():
    if not XAML_PATH.exists():
        print(f"ERROR: {XAML_PATH} not found. Run this script from the repo root.", file=sys.stderr)
        sys.exit(1)
    if not CS_PATH.exists():
        print(f"ERROR: {CS_PATH} not found. Run this script from the repo root.", file=sys.stderr)
        sys.exit(1)

    xaml_text = XAML_PATH.read_text(encoding="utf-8")
    xaml_text = patch_xaml(xaml_text)
    XAML_PATH.write_text(xaml_text, encoding="utf-8")
    print(f"Patched {XAML_PATH}")

    cs_text = CS_PATH.read_text(encoding="utf-8")
    cs_text = patch_codebehind(cs_text)
    CS_PATH.write_text(cs_text, encoding="utf-8")
    print(f"Patched {CS_PATH}")

    print("\nDone. Now run: dotnet build")


if __name__ == "__main__":
    main()
