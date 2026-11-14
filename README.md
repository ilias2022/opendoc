# Open in Google Docs

Right-click a Word, Excel or PowerPoint file (.docx, .doc, .odt, .rtf, .xlsx, .xls, .ods, .csv, .pptx, .ppt, .odp) → **Send to → Open in Google Docs**. It uploads the file to your Google Drive as a native Google Doc, Sheet or Slides file and opens it in your browser.

## Layout

| Project | What it is | Tested? |
|---|---|---|
| `src/OpenInGoogleDocs.Core` | All the decisions: which files to accept, what to name them, the upload-then-open loop. No Google, no Windows. | Yes, fully |
| `src/OpenInGoogleDocs` | The Windows app: entry point, Google Drive adapter, browser launcher, error dialog. | No (thin wrappers around other libraries) |
| `tests/OpenInGoogleDocs.Tests` | xUnit tests for Core. | n/a |

## 1. One-time Google Cloud setup

Do this at <https://console.cloud.google.com> (menu names occasionally move around).

1. Create a new project, e.g. "Open in Google Docs".
2. **APIs & Services → Library** → search for **Google Drive API** → **Enable**.
3. **Google Auth Platform**:
   - **Branding**: set an app name and your email.
   - **Audience**: choose *External*, then **Publish app** so the status is *In production*.
     Do this **before** you first sign in. Apps left in *Testing* get sign-in tokens that expire after 7 days, which means signing in again every week. If you already signed in while in Testing, delete `%LOCALAPPDATA%\OpenInGoogleDocs\token` afterwards to get a fresh, long-lived token.
   - **Clients → Create client** → application type **Desktop app** → download the JSON.
4. Rename the download to `credentials.json` and put it in `src/OpenInGoogleDocs/`. It's in `.gitignore`.

## 2. Add the Google package and run the tests

Run these from **Windows PowerShell** in the repo root. You need the .NET 10 SDK: `winget install Microsoft.DotNet.SDK.10`.

```powershell
dotnet add src/OpenInGoogleDocs package Google.Apis.Drive.v3
dotnet test
```

The first command fetches the latest version and records it in the `.csproj`. The tests only touch Core, so `dotnet test` also works from WSL.

## 3. Build and install

```powershell
dotnet publish src/OpenInGoogleDocs -c Release -r win-x64 --self-contained false -o C:\Tools\OpenInGoogleDocs

$s = (New-Object -ComObject WScript.Shell).CreateShortcut("$env:APPDATA\Microsoft\Windows\SendTo\Open in Google Docs.lnk")
$s.TargetPath = "C:\Tools\OpenInGoogleDocs\OpenInGoogleDocs.exe"
$s.Save()
```

On Windows 11, **Send to** lives under **Show more options** in the right-click menu. Shift + right-click opens that full menu directly.

### Optional: top-level right-click entry (Windows 11)

To get **Open in Google Docs** in the main right-click menu for these files, instead of under Send to:

1. Turn on **Developer Mode** (Settings → System → For developers). The package is unsigned, so Windows needs this to register it.
2. From the repo root in Windows PowerShell: `.\install-context-menu.ps1`

It publishes the app and `src/OpenInGoogleDocs.ShellExtension` into `C:\Tools\OpenInGoogleDocs` and registers them as a sparse package. If the entry doesn't appear, run `Stop-Process -Name explorer`. To remove it: `Get-AppxPackage OpenInGoogleDocs.ShellExtension | Remove-AppxPackage`.

## First run

Your browser opens to sign in to Google and approve access. You may see "Google hasn't verified this app". It's your own app, so choose **Advanced → Go to (app name)**. After that, the token is saved in `%LOCALAPPDATA%\OpenInGoogleDocs\token` and later runs go straight to the document.

The app only asks for the `drive.file` permission, so it can see files it created and nothing else in your Drive.

Problems show up as a Windows dialog, and full details go to `%LOCALAPPDATA%\OpenInGoogleDocs\log.txt`.
