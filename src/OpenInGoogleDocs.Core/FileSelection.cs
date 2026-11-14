namespace OpenInGoogleDocs.Core;

/// <summary>
/// The result of checking the paths Windows handed us: which ones we'll upload,
/// and which ones we won't (with a reason we can show the user).
/// </summary>
public sealed record FileSelection(IReadOnlyList<string> Accepted, IReadOnlyList<FileProblem> Rejected)
{
    private const string WordLockFilePrefix = "~$";

    /// <param name="args">The raw command-line arguments: one path per selected file.</param>
    /// <param name="fileExists">
    /// How to check a file exists. The app passes File.Exists; the tests pass a fake.
    /// Passing it in is called "dependency injection": the logic no longer depends on the real disk.
    /// </param>
    public static FileSelection From(IEnumerable<string> args, Func<string, bool> fileExists)
    {
        var accepted = new List<string>();
        var rejected = new List<FileProblem>();

        // Windows paths are case-insensitive (C:\A.docx and c:\a.DOCX are the same file),
        // so we compare them case-insensitively to spot duplicates.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            // HashSet.Add returns false if the item was already there: a cheap duplicate check.
            if (!seen.Add(arg))
                continue;

            var fileName = PathParts.FileName(arg);

            if (!fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                rejected.Add(new FileProblem(arg, "Only .docx files are supported."));
            else if (fileName.StartsWith(WordLockFilePrefix, StringComparison.Ordinal))
                rejected.Add(new FileProblem(arg, "This is a Word lock file (Word creates it while the document is open), not a real document."));
            else if (!fileExists(arg))
                rejected.Add(new FileProblem(arg, "File not found."));
            else
                accepted.Add(arg);
        }

        return new FileSelection(accepted, rejected);
    }
}
