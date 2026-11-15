using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenInGoogleDocs.ShellExtension;

// The Windows 11 right-click menu asks this class what to show (GetTitle, GetState)
// and what to do when clicked (Invoke). Windows finds it through the CLSID below,
// which must match the one in AppxManifest.xml.
[ComVisible(true)]
[Guid(ClassId)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class OpenInGoogleDocsCommand : IExplorerCommand
{
    public const string ClassId = "6B1D4F0E-3C52-4A8B-9E47-2D5A7C81F3B9";

    // The real app sits in the same folder as this DLL. Don't use AppContext.BaseDirectory here:
    // inside a COM host that's the folder of the host process (dllhost.exe), not ours.
    private static string ExePath =>
        Path.Combine(
            Path.GetDirectoryName(typeof(OpenInGoogleDocsCommand).Assembly.Location)!,
            "OpenInGoogleDocs.exe");

    private static void Log(string message)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenInGoogleDocs");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "shellext-log.txt"), $"{DateTime.Now:s} {message}{Environment.NewLine}");
        }
        catch { /* logging must never break the menu */ }
    }

    public void GetTitle(IShellItemArray? items, out string name) => name = "Open in Google Docs";

    public void GetIcon(IShellItemArray? items, out string icon) => icon = ExePath + ",0";

    public void GetToolTip(IShellItemArray? items, out string tip) =>
        tip = "Upload to Google Drive and open as a Google Doc, Sheet or Slides file";

    public void GetCanonicalName(out Guid guid) => guid = new Guid(ClassId);

    public void GetState(IShellItemArray? items, bool okToBeSlow, out uint state) => state = 0; // ECS_ENABLED

    public void Invoke(IShellItemArray? items, IntPtr bindContext)
    {
        try
        {
            if (items is null) { Log("Invoke: no items"); return; }

            if (!File.Exists(ExePath))
            {
                Log($"Invoke: {ExePath} not found");
                ShowError("Open in Google Docs can't find its program file. Try reinstalling it.");
                return;
            }

            var startInfo = new ProcessStartInfo(ExePath) { UseShellExecute = false };

            items.GetCount(out var count);
            for (uint i = 0; i < count; i++)
            {
                items.GetItemAt(i, out var item);
                item.GetDisplayName(SIGDN_FILESYSPATH, out var path);
                startInfo.ArgumentList.Add(path);
            }

            Log($"Invoke: starting {ExePath} with {startInfo.ArgumentList.Count} file(s)");
            if (startInfo.ArgumentList.Count > 0)
                Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            // Never let a managed exception escape into Explorer: log it and tell the user instead.
            Log($"Invoke failed: {ex}");
            ShowError($"Couldn't start Open in Google Docs: {ex.Message}");
        }
    }

    private static void ShowError(string message)
    {
        try { MessageBox(IntPtr.Zero, message, "Open in Google Docs", 0x10 /* MB_ICONERROR */); }
        catch { /* nothing more we can do */ }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public void GetFlags(out uint flags) => flags = 0; // ECF_DEFAULT

    // We have no sub-commands (GetFlags says so). Never throw into Explorer: hand back "nothing".
    public void EnumSubCommands(out IntPtr enumerator) => enumerator = IntPtr.Zero;

    private const uint SIGDN_FILESYSPATH = 0x80058000;
}

// ---- Minimal declarations of the Windows shell interfaces we talk to. ----
// Method ORDER matters: COM calls methods by position, so unused ones are kept as placeholders.

[ComImport, Guid("A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommand
{
    void GetTitle(IShellItemArray? items, [MarshalAs(UnmanagedType.LPWStr)] out string name);
    void GetIcon(IShellItemArray? items, [MarshalAs(UnmanagedType.LPWStr)] out string icon);
    void GetToolTip(IShellItemArray? items, [MarshalAs(UnmanagedType.LPWStr)] out string tip);
    void GetCanonicalName(out Guid guid);
    void GetState(IShellItemArray? items, [MarshalAs(UnmanagedType.Bool)] bool okToBeSlow, out uint state);
    void Invoke(IShellItemArray? items, IntPtr bindContext);
    void GetFlags(out uint flags);
    void EnumSubCommands(out IntPtr enumerator);
}

[ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItemArray
{
    void BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid riid, out IntPtr result);
    void GetPropertyStore(int flags, ref Guid riid, out IntPtr result);
    void GetPropertyDescriptionList(IntPtr key, ref Guid riid, out IntPtr result);
    void GetAttributes(int flags, uint mask, out uint attributes);
    void GetCount(out uint count);
    void GetItemAt(uint index, out IShellItem item);
    void EnumItems(out IntPtr enumerator);
}

[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItem
{
    void BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid riid, out IntPtr result);
    void GetParent(out IShellItem parent);
    void GetDisplayName(uint sigdn, [MarshalAs(UnmanagedType.LPWStr)] out string name);
    void GetAttributes(uint mask, out uint attributes);
    void Compare(IShellItem other, uint hint, out int order);
}
