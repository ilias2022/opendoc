using System.Runtime.InteropServices;
using OpenInGoogleDocs.Core;

namespace OpenInGoogleDocs;

/// <summary>
/// The app has no console window, so this is the only way it can tell you anything:
/// a standard Windows message box, plus a log file with the full details.
/// </summary>
internal sealed class ErrorReporter(string logPath)
{
    private const string Caption = "Open in Google Docs";
    private const long MaxLogBytes = 256 * 1024;

    // Icon codes for MessageBox, from the Windows API documentation.
    private const uint IconError = 0x10;
    private const uint IconWarning = 0x30;
    private const uint IconInformation = 0x40;

    public void ShowInfo(string message) => MessageBox(IntPtr.Zero, message, Caption, IconInformation);

    public void ShowError(string message)
    {
        Log(message);
        MessageBox(IntPtr.Zero, message, Caption, IconError);
    }

    public void ShowProblems(IReadOnlyList<FileProblem> problems)
    {
        var lines = problems.Select(p => $"• {Path.GetFileName(p.Path)}: {p.Reason}");
        var message = "Some files couldn't be opened:\n\n" + string.Join("\n", lines);
        Log(message);
        MessageBox(IntPtr.Zero, message, Caption, IconWarning);
    }

    public void ShowCrash(Exception ex)
    {
        Log(ex.ToString()); // full stack trace goes to the log, not the dialog
        MessageBox(IntPtr.Zero, $"Something went wrong: {ex.Message}\n\nDetails: {logPath}", Caption, IconError);
    }

    private void Log(string text)
    {
        try
        {
            // Keep the log from growing forever: past 256 KB, start a fresh one (keeping one old copy).
            if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxLogBytes)
                File.Move(logPath, logPath + ".old", overwrite: true);

            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // If we can't write the log, still show the dialog rather than crash while reporting a crash.
        }
    }

    // P/Invoke: calling a function built into Windows (user32.dll) directly from C#.
    // This gives us a message box without pulling in all of Windows Forms just for one dialog.
    // CharSet.Unicode picks MessageBoxW, the version that handles any characters in file names.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
