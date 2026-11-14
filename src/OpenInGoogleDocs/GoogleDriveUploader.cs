using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using OpenInGoogleDocs.Core;
// Google's library has its own class called "File", which clashes with System.IO.File.
// This alias lets us call Google's one DriveFile and keep File meaning System.IO.File.
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace OpenInGoogleDocs;

/// <summary>
/// The real IDocumentUploader: uploads an Office file to Google Drive and asks Drive
/// to convert it into a native Google Doc, Sheet or Slides file on the way in.
/// This class is a thin "adapter" around Google's library and is deliberately not
/// unit-tested: there's almost no logic of ours in it, and the only meaningful test is a real
/// upload. All the decisions live in Core, which is tested.
/// </summary>
internal sealed class GoogleDriveUploader : IDocumentUploader
{
    private readonly DriveService _drive;

    // Private constructor + public static CreateAsync: constructors can't be async,
    // and signing in is async, so a "factory method" builds the object for us.
    private GoogleDriveUploader(DriveService drive) => _drive = drive;

    public static async Task<GoogleDriveUploader> CreateAsync(
        string credentialsPath, string tokenFolder, CancellationToken cancellationToken)
    {
        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException(
                "credentials.json is missing. Download your OAuth client file from Google Cloud and put it next to the .exe.",
                credentialsPath);

        UserCredential credential;
        await using (var stream = File.OpenRead(credentialsPath))
        {
            // First run: opens your browser to sign in and approve access, then saves a token
            // in tokenFolder. Later runs reuse (and silently refresh) that token: no browser.
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                // drive.file = access ONLY to files this app creates or you open with it.
                // Much safer than full Drive access, and Google treats it as a low-risk scope.
                new[] { DriveService.Scope.DriveFile },
                "user", // a key for the saved token; only matters if you supported several accounts
                cancellationToken,
                new FileDataStore(tokenFolder, true)); // true = tokenFolder is a full path, not a name
        }

        var drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "OpenInGoogleDocs",
        });

        return new GoogleDriveUploader(drive);
    }

    public async Task<Uri> UploadAsync(string localPath, string title, DocumentKind kind, CancellationToken cancellationToken)
    {
        // The key trick: we SEND the original content (SourceMimeType) but tell Drive the file
        // should BE a Google Doc/Sheet/Slides (GoogleMimeType). When those differ, Drive converts it.
        var metadata = new DriveFile { Name = title, MimeType = kind.GoogleMimeType };

        await using var content = File.OpenRead(localPath);
        var request = _drive.Files.Create(metadata, content, kind.SourceMimeType);

        // By default Drive returns very few fields. Ask for the link we want to open.
        request.Fields = "id, webViewLink";

        var progress = await request.UploadAsync(cancellationToken);

        // Careful: UploadAsync doesn't throw on failure; it reports it in the status.
        // Turn that into an exception so Opener records it as a problem for this file.
        if (progress.Status != UploadStatus.Completed)
            throw new InvalidOperationException(
                $"Upload to Google Drive failed: {progress.Exception?.Message ?? "unknown error"}",
                progress.Exception);

        return new Uri(request.ResponseBody.WebViewLink);
    }
}
