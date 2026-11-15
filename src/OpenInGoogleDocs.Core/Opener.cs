namespace OpenInGoogleDocs.Core;

/// <summary>
/// The workflow: for each file, upload it as a Google Doc, then open the resulting link.
/// It only knows about the two interfaces, not about Google or Windows,
/// which is what makes it testable with fakes.
/// </summary>
public sealed class Opener(IDocumentUploader uploader, IBrowser browser, TimeSpan? perFileTimeout = null)
{
    /// <returns>The files that failed, with reasons. An empty list means everything worked.</returns>
    public async Task<IReadOnlyList<FileProblem>> OpenAllAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken)
    {
        var problems = new List<FileProblem>();

        // One at a time, on purpose. On the very first run, the uploader has to sign you in
        // through the browser; doing several uploads in parallel would trigger several
        // sign-in prompts racing each other. A few seconds per file is fine for a right-click tool.
        foreach (var path in paths)
        {
            try
            {
                var kind = DocumentKind.For(PathParts.FileName(path))
                    ?? throw new InvalidOperationException("Unsupported file type.");

                // Each file gets its own time limit, so one stuck upload can't sink the whole batch.
                using var fileCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (perFileTimeout is { } limit)
                    fileCts.CancelAfter(limit);

                var url = await uploader.UploadAsync(path, DocumentTitle.From(path), kind, fileCts.Token);
                browser.Open(url);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Only this file's own time limit fired (the caller didn't cancel): a per-file problem.
                problems.Add(new FileProblem(path, "Timed out while uploading to Google Drive."));
            }
            catch (OperationCanceledException)
            {
                // Cancellation means "stop everything", not "this one file failed",
                // so let it escape instead of recording it as a problem.
                throw;
            }
            catch (Exception ex)
            {
                // Any other failure affects only this file: note it and move on to the next.
                problems.Add(new FileProblem(path, ex.Message));
            }
        }

        return problems;
    }
}
