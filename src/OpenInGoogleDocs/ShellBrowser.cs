using System.Diagnostics;
using OpenInGoogleDocs.Core;

namespace OpenInGoogleDocs;

/// <summary>Opens a URL in the user's default browser.</summary>
internal sealed class ShellBrowser : IBrowser
{
    public void Open(Uri url) =>
        // UseShellExecute = true means "let Windows decide how to open this", like double-clicking it.
        // Its default is false in modern .NET, and with false, starting a URL fails
        // because a URL isn't an executable.
        Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
}
