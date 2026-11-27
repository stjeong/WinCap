using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace WinCap.Updates;

/// <summary>How the running copy of WinCap was installed, which decides how it is updated.</summary>
internal enum InstallKind
{
    /// <summary>Installed from the MSIX package: open the new .msix with App Installer.</summary>
    Msix,

    /// <summary>Portable single-file WinCap.exe from the zip: replace the exe and restart.</summary>
    Portable,

    /// <summary>Developer build (WinCap.dll next to the exe): only the release page is opened.</summary>
    Unsupported,
}

internal static class UpdateInstaller
{
    /// <summary>Command-line switch of the restarted exe: wait for the old process to exit.</summary>
    public const string AfterUpdateArgument = "--after-update";

    public const string ZipSuffix = "-win-x64.zip";
    public const string MsixSuffix = "-x64.msix";
    public const string CertificateSuffix = "-selfsigned.cer";

    private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

    /// <summary>Backup name of the replaced exe; deleted on the next start.</summary>
    public static string OldExePath => ExePath + ".old";

    public static InstallKind DetectInstallKind()
    {
        if (IsPackaged()) return InstallKind.Msix;
        string dir = Path.GetDirectoryName(ExePath) ?? string.Empty;
        // A framework-dependent / debug build has WinCap.dll beside the exe; release builds are single-file.
        return File.Exists(Path.Combine(dir, "WinCap.dll")) ? InstallKind.Unsupported : InstallKind.Portable;
    }

    /// <summary>Folder for downloaded update files.</summary>
    public static string DownloadFolder(ReleaseInfo release) =>
        Path.Combine(Path.GetTempPath(), "WinCap-update", release.Tag);

    // ------------------------------------------------------------------
    //  Portable
    // ------------------------------------------------------------------

    /// <summary>
    /// Replaces the running WinCap.exe with the one in <paramref name="zipPath"/> and starts it.
    /// The caller must exit right after this returns.
    /// </summary>
    /// <remarks>
    /// Windows allows renaming a running exe, so the current file is moved to WinCap.exe.old
    /// and the new one is copied into its place. The new process waits for this one to exit
    /// (single-instance mutex) and deletes the .old file.
    /// </remarks>
    public static void ApplyPortable(string zipPath)
    {
        string exe = ExePath;
        string extractDir = Path.Combine(Path.GetDirectoryName(zipPath)!, "extracted");
        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true);
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        string newExe = Path.Combine(extractDir, "WinCap.exe");
        if (!File.Exists(newExe))
            throw new InvalidOperationException("The downloaded zip does not contain WinCap.exe.");

        string old = OldExePath;
        if (File.Exists(old)) File.Delete(old);
        File.Move(exe, old);
        try
        {
            File.Copy(newExe, exe);
        }
        catch
        {
            File.Move(old, exe); // put the running version back
            throw;
        }

        Process.Start(new ProcessStartInfo(exe, AfterUpdateArgument)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exe)!,
        });
    }

    /// <summary>Removes the exe left over from a portable update (no-op if none).</summary>
    public static void DeleteOldExe()
    {
        try
        {
            if (File.Exists(OldExePath)) File.Delete(OldExePath);
        }
        catch
        {
            // Still locked or not permitted: try again next time.
        }
    }

    // ------------------------------------------------------------------
    //  MSIX
    // ------------------------------------------------------------------

    /// <summary>True when the certificate is already trusted for installing MSIX packages.</summary>
    public static bool IsCertificateTrusted(string cerPath)
    {
        using var cert = X509CertificateLoader.LoadCertificateFromFile(cerPath);
        foreach (var (name, location) in new[]
                 {
                     (StoreName.TrustedPeople, StoreLocation.LocalMachine),
                     (StoreName.Root, StoreLocation.LocalMachine),
                 })
        {
            using var store = new X509Store(name, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            if (store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false).Count > 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Adds the certificate to Local Machine \ Trusted People (UAC prompt).
    /// Returns false when the user declined the elevation.
    /// </summary>
    public static bool TrustCertificate(string cerPath)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("certutil.exe", $"-addstore TrustedPeople \"{cerPath}\"")
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            })!;
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"certutil failed with exit code {p.ExitCode}.");
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED: UAC declined
        {
            return false;
        }
    }

    /// <summary>Opens the .msix with App Installer. The caller should exit so the app can be replaced.</summary>
    public static void LaunchMsix(string msixPath) =>
        Process.Start(new ProcessStartInfo(msixPath) { UseShellExecute = true });

    // ------------------------------------------------------------------

    public static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, char[]? packageFullName);

    private static bool IsPackaged()
    {
        try
        {
            int length = 0;
            return GetCurrentPackageFullName(ref length, null) != APPMODEL_ERROR_NO_PACKAGE;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
