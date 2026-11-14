namespace OpenInGoogleDocs.Core;

/// <summary>A file we couldn't handle, and a human-readable reason why.</summary>
public sealed record FileProblem(string Path, string Reason);

/// <summary>
/// Anything that can take a local file and give back a link to it as a native Google
/// Doc, Sheet or Slides file (depending on its kind).
/// The real version talks to Google Drive; the tests use a fake.
/// </summary>
public interface IDocumentUploader
{
    Task<Uri> UploadAsync(string localPath, string title, DocumentKind kind, CancellationToken cancellationToken);
}

/// <summary>Anything that can show a URL to the user. The real version opens the default browser.</summary>
public interface IBrowser
{
    void Open(Uri url);
}
