using OpenInGoogleDocs.Core;

namespace OpenInGoogleDocs.Tests;

public class FileSelectionTests
{
    // A fake file system: only the paths passed in "exist".
    // This keeps the tests fast and lets them run anywhere (Windows or WSL)
    // without creating real files on disk.
    private static Func<string, bool> Existing(params string[] paths) =>
        path => paths.Contains(path);

    [Fact]
    public void Accepts_an_existing_docx_file()
    {
        var selection = FileSelection.From(
            new[] { @"C:\Docs\report.docx" },
            Existing(@"C:\Docs\report.docx"));

        Assert.Equal(new[] { @"C:\Docs\report.docx" }, selection.Accepted);
        Assert.Empty(selection.Rejected);
    }

    [Theory]
    [InlineData(@"C:\Docsudget.xlsx")]
    [InlineData(@"C:\Docs\data.csv")]
    [InlineData(@"C:\Docs\deck.pptx")]
    [InlineData(@"C:\Docs\old.ppt")]
    public void Accepts_spreadsheets_and_presentations(string path)
    {
        var selection = FileSelection.From(new[] { path }, Existing(path));

        Assert.Equal(new[] { path }, selection.Accepted);
        Assert.Empty(selection.Rejected);
    }

    [Fact]
    public void Treats_the_extension_case_insensitively()
    {
        var selection = FileSelection.From(
            new[] { @"C:\Docs\SHOUTY.DOCX" },
            Existing(@"C:\Docs\SHOUTY.DOCX"));

        Assert.Equal(new[] { @"C:\Docs\SHOUTY.DOCX" }, selection.Accepted);
    }

    [Fact]
    public void Rejects_unsupported_file_types()
    {
        var selection = FileSelection.From(
            new[] { @"C:\Docs\photo.jpg" },
            Existing(@"C:\Docs\photo.jpg"));

        Assert.Empty(selection.Accepted);
        var problem = Assert.Single(selection.Rejected);
        Assert.Equal(@"C:\Docs\photo.jpg", problem.Path);
        Assert.Contains("Unsupported", problem.Reason);
    }

    [Fact]
    public void Rejects_files_that_do_not_exist()
    {
        var selection = FileSelection.From(
            new[] { @"C:\Docs\gone.docx" },
            Existing());

        Assert.Empty(selection.Accepted);
        var problem = Assert.Single(selection.Rejected);
        Assert.Contains("not found", problem.Reason);
    }

    // Word creates a hidden "~$report.docx" owner file while report.docx is open.
    // It has the right extension but isn't a real document.
    // Both slash styles are tested because the path-splitting code must not
    // depend on the operating system the tests happen to run on.
    [Theory]
    [InlineData(@"C:\Docs\~$report.docx")]
    [InlineData("C:/Docs/~$report.docx")]
    public void Rejects_Office_lock_files(string lockFile)
    {
        var selection = FileSelection.From(new[] { lockFile }, Existing(lockFile));

        Assert.Empty(selection.Accepted);
        var problem = Assert.Single(selection.Rejected);
        Assert.Contains("lock file", problem.Reason);
    }

    [Fact]
    public void Ignores_the_same_file_selected_twice_regardless_of_case()
    {
        var selection = FileSelection.From(
            new[] { @"C:\Docs\report.docx", @"c:\docs\REPORT.docx" },
            path => true);

        Assert.Equal(new[] { @"C:\Docs\report.docx" }, selection.Accepted);
        Assert.Empty(selection.Rejected);
    }

    [Fact]
    public void Ignores_blank_arguments()
    {
        var selection = FileSelection.From(new[] { "", "   " }, path => true);

        Assert.Empty(selection.Accepted);
        Assert.Empty(selection.Rejected);
    }
}
