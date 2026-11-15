using OpenInGoogleDocs;
using OpenInGoogleDocs.Core;

// Entry point. This file uses "top-level statements": there's no Main method written out,
// and `args` (the command-line arguments) is available automatically.
// When you use Send to, Windows passes the selected files' full paths as `args`.

// Our own folder for the sign-in token and the log:
// %LOCALAPPDATA%\OpenInGoogleDocs, e.g. C:\Users\Ilias\AppData\Local\OpenInGoogleDocs
var appDataFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "OpenInGoogleDocs");
Directory.CreateDirectory(appDataFolder); // does nothing if it already exists

var reporter = new ErrorReporter(Path.Combine(appDataFolder, "log.txt"));

if (args.Length == 0)
{
    reporter.ShowInfo("Right-click a Word, Excel or PowerPoint file and choose Open in Google Docs.");
    return 0;
}

// Safety net: there's no window to close, so if sign-in waits forever
// (e.g. you close the Google sign-in tab without finishing), give up after 5 minutes
// instead of leaving an invisible process running. Uploads have their own per-file limit below.
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var selection = FileSelection.From(args, File.Exists);
    var problems = new List<FileProblem>(selection.Rejected);

    // Only sign in to Google if there's actually something to upload.
    if (selection.Accepted.Count > 0)
    {
        var uploader = await GoogleDriveUploader.CreateAsync(
            credentialsPath: Path.Combine(AppContext.BaseDirectory, "credentials.json"),
            tokenFolder: Path.Combine(appDataFolder, "token"),
            timeout.Token);

        var opener = new Opener(uploader, new ShellBrowser(), perFileTimeout: TimeSpan.FromMinutes(3));
        problems.AddRange(await opener.OpenAllAsync(selection.Accepted, CancellationToken.None));
    }

    if (problems.Count > 0)
        reporter.ShowProblems(problems);

    // Exit code: 0 means success, anything else means failure (a universal convention).
    return problems.Count == 0 ? 0 : 1;
}
catch (OperationCanceledException)
{
    reporter.ShowError("Gave up after 5 minutes. Did the Google sign-in page get closed before finishing?");
    return 1;
}
catch (Exception ex)
{
    // Anything unexpected (missing credentials.json, no internet, ...).
    reporter.ShowCrash(ex);
    return 1;
}
