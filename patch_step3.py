CS = "src/PharmacyMS.Desktop/Views/Shell/ShellWindow.axaml.cs"

def replace_once(path, old, new):
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()
    count = content.count(old)
    if count != 1:
        raise SystemExit(f"ERROR in {path}: expected 1 match, found {count} for:\n{old[:120]}...")
    content = content.replace(old, new)
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"OK: patched {path}")

replace_once(CS,
'''using PharmacyMS.Desktop.Views.Onboarding;
using PharmacyMS.Desktop.Views.Splash;

namespace PharmacyMS.Desktop.Views.Shell;''',
'''using PharmacyMS.Desktop.Views.Onboarding;
using PharmacyMS.Desktop.Views.Splash;
using PharmacyMS.Desktop.Views.Startup;

namespace PharmacyMS.Desktop.Views.Shell;''')

replace_once(CS,
'''    public void ShowSplash(SplashView splash) => RootContent.Content = splash;''',
'''    public void ShowSplash(SplashView splash) => RootContent.Content = splash;

    public void ShowServerUnreachable(string reason, Func<Task> onRetry) =>
        RootContent.Content = new ServerUnreachableView(reason, onRetry);''')

print("\nAll patches applied successfully.")
