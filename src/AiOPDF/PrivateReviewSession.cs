using System.Security.AccessControl;
using System.Security.Principal;
namespace AiOPDF;
public sealed class PrivateReviewSession : IDisposable
{
    readonly FileStream lease;
    public string DirectoryPath { get; }
    public PrivateReviewSession(string? root = null)
    {
        root ??= Path.Combine(Path.GetTempPath(), "AiOPDF-private-" + Environment.UserName);
        root = Path.GetFullPath(root);
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(root);
        else Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new IOException("La carpeta temporal no puede ser un enlace.");
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectorySecurity(); security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(root).SetAccessControl(security);
        }
        else File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Recover(root);
        DirectoryPath = Path.Combine(root, "AiOPDF-review-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(DirectoryPath);
        lease = new FileStream(Path.Combine(DirectoryPath, "session.lease"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    }
    public static void Recover(string root)
    {
        foreach (var directory in Directory.EnumerateDirectories(root, "AiOPDF-review-*"))
        {
            if (!Guid.TryParseExact(Path.GetFileName(directory)["AiOPDF-review-".Length..], "N", out _) || !SafeTree(directory)) continue;
            var marker = Path.Combine(directory, "session.lease"); if (!File.Exists(marker)) continue;
            try { using (new FileStream(marker, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } Directory.Delete(directory, true); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    static bool SafeTree(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return false;
        foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0 || (Directory.Exists(entry) && !SafeTree(entry))) return false;
        return true;
    }
    public void Dispose()
    {
        lease.Dispose();
        try { if (Directory.Exists(DirectoryPath) && SafeTree(DirectoryPath)) Directory.Delete(DirectoryPath, true); }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
