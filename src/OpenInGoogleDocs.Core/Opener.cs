namespace OpenInGoogleDocs.Core;

/// <summary>
/// The workflow: for each file, upload it as a Google Doc, then open the resulting link.
/// It only knows about the two interfaces, not about Google or Windows,
/// which is what makes it testable with fakes.
/// </summary>
public sealed class Opener(IDocumentUploader uploader, IBrowser browser)
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
                var url = await uploader.UploadAsGoogleDocAsync(path, DocumentTitle.From(path), cancellationToken);
                browser.Open(url);
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
