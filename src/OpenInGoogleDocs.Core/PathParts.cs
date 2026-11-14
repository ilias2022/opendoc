namespace OpenInGoogleDocs.Core;

/// <summary>
/// Tiny path helpers that treat BOTH '\' and '/' as folder separators.
///
/// Why not System.IO.Path.GetFileName? Its behaviour depends on the OS it runs on:
/// on Linux (including WSL), '\' is a legal character in a file name, not a separator,
/// so Path.GetFileName(@"C:\Docs\a.docx") returns the whole string. Our input is always
/// a Windows path, but the tests should pass wherever they run.
/// </summary>
internal static class PathParts
{
    public static string FileName(string path)
    {
        var lastSeparator = path.LastIndexOfAny(['\\', '/']);
        return path[(lastSeparator + 1)..];
    }
}
