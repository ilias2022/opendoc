using OpenInGoogleDocs.Core;

namespace OpenInGoogleDocs.Tests;

public class DocumentTitleTests
{
    [Theory]
    [InlineData(@"C:\Docs\Q3 report.docx", "Q3 report")]
    [InlineData("C:/Docs/Q3 report.docx", "Q3 report")]
    [InlineData(@"C:\Docs\archive.v2.docx", "archive.v2")] // only the LAST extension is removed
    [InlineData(@"C:\Docs\README", "README")]              // no extension: name unchanged
    public void Uses_the_file_name_without_its_extension(string path, string expected)
    {
        Assert.Equal(expected, DocumentTitle.From(path));
    }
}
