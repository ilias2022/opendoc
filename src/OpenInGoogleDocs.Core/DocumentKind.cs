namespace OpenInGoogleDocs.Core;

/// <summary>
/// One kind of file we can convert: what we send to Drive (SourceMimeType) and what
/// Drive should turn it into (GoogleMimeType). When the two differ, Drive converts on upload.
/// </summary>
public sealed record DocumentKind(string SourceMimeType, string GoogleMimeType)
{
    private const string GoogleDoc = "application/vnd.google-apps.document";
    private const string GoogleSheet = "application/vnd.google-apps.spreadsheet";
    private const string GoogleSlides = "application/vnd.google-apps.presentation";

    private const string Ooxml = "application/vnd.openxmlformats-officedocument.";
    private const string OpenDocument = "application/vnd.oasis.opendocument.";

    // Extensions Drive can import and convert, keyed case-insensitively.
    private static readonly Dictionary<string, DocumentKind> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".docx"] = new(Ooxml + "wordprocessingml.document", GoogleDoc),
        [".doc"] = new("application/msword", GoogleDoc),
        [".odt"] = new(OpenDocument + "text", GoogleDoc),
        [".rtf"] = new("application/rtf", GoogleDoc),

        [".xlsx"] = new(Ooxml + "spreadsheetml.sheet", GoogleSheet),
        [".xls"] = new("application/vnd.ms-excel", GoogleSheet),
        [".ods"] = new(OpenDocument + "spreadsheet", GoogleSheet),
        [".csv"] = new("text/csv", GoogleSheet),

        [".pptx"] = new(Ooxml + "presentationml.presentation", GoogleSlides),
        [".ppt"] = new("application/vnd.ms-powerpoint", GoogleSlides),
        [".odp"] = new(OpenDocument + "presentation", GoogleSlides),
    };

    /// <summary>The kind for this file name, or null if we can't convert it.</summary>
    public static DocumentKind? For(string fileName) =>
        ByExtension.TryGetValue(Path.GetExtension(fileName), out var kind) ? kind : null;

    /// <summary>All supported extensions, e.g. for the installer and error messages.</summary>
    public static IReadOnlyCollection<string> SupportedExtensions => ByExtension.Keys;
}
