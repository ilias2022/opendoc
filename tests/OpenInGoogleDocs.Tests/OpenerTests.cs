using OpenInGoogleDocs.Core;

namespace OpenInGoogleDocs.Tests;

public class OpenerTests
{
    // Hand-written fakes instead of a mocking library: they are short, and you can
    // read exactly what they do. They record what happened so the tests can check it.
    private sealed class FakeUploader : IDocumentUploader
    {
        public List<(string Path, string Title, string Google)> Uploads { get; } = new();
        public HashSet<string> FailFor { get; } = new();

        public Task<Uri> UploadAsync(string localPath, string title, DocumentKind kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Uploads.Add((localPath, title, kind.GoogleMimeType));

            if (FailFor.Contains(localPath))
                throw new IOException("Disk on fire");

            return Task.FromResult(new Uri($"https://docs.google.com/document/d/doc-{Uploads.Count}/edit"));
        }
    }

    private sealed class FakeBrowser : IBrowser
    {
        public List<Uri> Opened { get; } = new();
        public void Open(Uri url) => Opened.Add(url);
    }

    [Fact]
    public async Task Uploads_each_file_with_its_title_and_opens_the_returned_link()
    {
        var uploader = new FakeUploader();
        var browser = new FakeBrowser();

        var problems = await new Opener(uploader, browser).OpenAllAsync(
            new[] { @"C:\Docs\one.docx", @"C:\Docs\two.xlsx" },
            CancellationToken.None);

        Assert.Empty(problems);
        Assert.Equal(
            new[]
            {
                (@"C:\Docs\one.docx", "one", "application/vnd.google-apps.document"),
                (@"C:\Docs\two.xlsx", "two", "application/vnd.google-apps.spreadsheet"),
            },
            uploader.Uploads);
        Assert.Equal(
            new[]
            {
                new Uri("https://docs.google.com/document/d/doc-1/edit"),
                new Uri("https://docs.google.com/document/d/doc-2/edit"),
            },
            browser.Opened);
    }

    [Fact]
    public async Task Keeps_going_when_one_upload_fails_and_reports_why()
    {
        var uploader = new FakeUploader();
        uploader.FailFor.Add(@"C:\Docs\bad.docx");
        var browser = new FakeBrowser();

        var problems = await new Opener(uploader, browser).OpenAllAsync(
            new[] { @"C:\Docs\bad.docx", @"C:\Docs\good.docx" },
            CancellationToken.None);

        var problem = Assert.Single(problems);
        Assert.Equal(@"C:\Docs\bad.docx", problem.Path);
        Assert.Equal("Disk on fire", problem.Reason);
        Assert.Single(browser.Opened); // only the good one was opened
    }

    [Fact]
    public async Task Does_not_swallow_cancellation()
    {
        var opener = new Opener(new FakeUploader(), new FakeBrowser());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            opener.OpenAllAsync(new[] { @"C:\Docs\one.docx" }, cancelled.Token));
    }
}
