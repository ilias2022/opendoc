namespace OpenInGoogleDocs.Core;

/// <summary>Works out the name the new Google Doc should have.</summary>
public static class DocumentTitle
{
    /// <summary>"C:\Docs\Q3 report.docx" becomes "Q3 report".</summary>
    public static string From(string path)
    {
        var fileName = PathParts.FileName(path);
        var lastDot = fileName.LastIndexOf('.');

        // lastDot > 0 (rather than >= 0) keeps names like ".gitignore" intact:
        // a leading dot is part of the name, not an extension.
        return lastDot > 0 ? fileName[..lastDot] : fileName;
    }
}
