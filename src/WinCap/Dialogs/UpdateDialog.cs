using System.Text.RegularExpressions;
using WinCap.Controls;
using WinCap.Updates;

namespace WinCap.Dialogs;

/// <summary>
/// Shows a newer release and downloads its package. After DialogResult.OK the caller installs
/// <see cref="PackagePath"/> (and trusts <see cref="CertificatePath"/> for MSIX, if set).
/// </summary>
public sealed class UpdateDialog : DialogBase
{
    private readonly ReleaseInfo _release;
    private readonly InstallKind _kind;
    private readonly ProgressBar _progress;
    private readonly Label _lblProgress;
    private CancellationTokenSource? _cts;
    private bool _closeWhenCanceled;

    /// <summary>Downloaded .zip or .msix.</summary>
    public string? PackagePath { get; private set; }

    /// <summary>Downloaded self-signed certificate of an MSIX release (null if the release has none).</summary>
    public string? CertificatePath { get; private set; }

    internal UpdateDialog(ReleaseInfo release, InstallKind kind)
    {
        _release = release;
        _kind = kind;
        Text = "Update WinCap";
        Table.ColumnCount = 1;

        var title = new Label
        {
            Text = $"WinCap {release.Version} is available",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12F),
            Margin = new Padding(0, 0, 0, 4),
        };
        Table.Controls.Add(title);

        string date = release.PublishedAt is { } d ? $" · released {d.LocalDateTime:yyyy-MM-dd}" : string.Empty;
        Table.Controls.Add(new Label
        {
            Text = $"You have version {AppVersion.Current}{date}",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 0, 0, 8),
        });

        var notes = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Size = new Size(460, 170),
            Text = string.IsNullOrWhiteSpace(release.Notes) ? "(No release notes)" : ToPlainText(release.Notes),
            Margin = new Padding(0, 0, 0, 6),
            TabStop = false,
        };
        Table.Controls.Add(notes);

        var link = new LinkLabel
        {
            Text = "View this release on GitHub",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
            LinkColor = Theme.Accent,
            ActiveLinkColor = Theme.AccentHover,
        };
        link.LinkClicked += (_, _) => UpdateInstaller.OpenUrl(release.PageUrl);
        Table.Controls.Add(link);

        Table.Controls.Add(new Label
        {
            Text = kind switch
            {
                InstallKind.Msix => "The installer package (MSIX) will be downloaded and opened. WinCap closes so it can be updated.",
                InstallKind.Portable => "The new WinCap.exe will be downloaded and replace this one. WinCap then restarts.",
                _ => "This is a development build, which cannot update itself. The release page will be opened instead.",
            },
            AutoSize = true,
            MaximumSize = new Size(460, 0),
        });

        _progress = new ProgressBar { Size = new Size(460, 14), Margin = new Padding(0, 10, 0, 2), Visible = false, Maximum = 1000 };
        _lblProgress = new Label { AutoSize = true, ForeColor = SystemColors.GrayText, Visible = false };
        Table.Controls.Add(_progress);
        Table.Controls.Add(_lblProgress);

        OkButton.Text = kind == InstallKind.Unsupported ? "Open Release Page" : "Download and Install";
        OkButton.DialogResult = DialogResult.None; // closes itself when the download has finished
        OkButton.Click += OnOkClick;
        FormClosing += OnFormClosing;
        Shown += (_, _) => { notes.Select(0, 0); OkButton.Focus(); };
        FinishLayout();
    }

    /// <summary>Light Markdown cleanup so GitHub release notes read well in a plain text box.</summary>
    private static string ToPlainText(string markdown)
    {
        var lines = new List<string>();
        foreach (string raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw.TrimEnd();
            string trimmed = line.Trim();
            if (trimmed.Length > 1 && trimmed.Trim('|', '-', ':', ' ').Length == 0)
                continue; // table separator row: |---|---|
            if (trimmed.StartsWith('|') && trimmed.EndsWith('|'))
                line = string.Join("   ", trimmed.Trim('|').Split('|').Select(c => c.Trim())); // table row
            line = Regex.Replace(line, @"^#{1,6}\s+", string.Empty);              // headings
            line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]+\)", "$1");          // [text](url)
            line = line.Replace("**", string.Empty).Replace("`", string.Empty);   // bold, code
            lines.Add(line);
        }
        return string.Join("\r\n", lines).Trim();
    }

    private async void OnOkClick(object? sender, EventArgs e)
    {
        if (_kind == InstallKind.Unsupported)
        {
            UpdateInstaller.OpenUrl(_release.PageUrl);
            DialogResult = DialogResult.Cancel;
            return;
        }

        string suffix = _kind == InstallKind.Msix ? UpdateInstaller.MsixSuffix : UpdateInstaller.ZipSuffix;
        ReleaseAsset? package = _release.FindAsset(suffix);
        if (package == null)
        {
            MessageBox.Show(this, $"The release has no \"*{suffix}\" file. Please download it from the release page.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            UpdateInstaller.OpenUrl(_release.PageUrl);
            return;
        }
        ReleaseAsset? certificate = _kind == InstallKind.Msix ? _release.FindAsset(UpdateInstaller.CertificateSuffix) : null;

        var cts = new CancellationTokenSource();
        _cts = cts;
        OkButton.Enabled = false;
        _progress.Visible = _lblProgress.Visible = true;
        string folder = UpdateInstaller.DownloadFolder(_release);
        try
        {
            string packagePath = Path.Combine(folder, package.Name);
            var progress = new Progress<double>(f =>
            {
                _progress.Value = (int)Math.Clamp(f * 1000, 0, 1000);
                _lblProgress.Text = $"Downloading {package.Name}  {f * package.Size / 1048576:N1} / {package.Size / 1048576.0:N1} MB";
            });
            await GitHubReleases.DownloadAsync(package, packagePath, progress, cts.Token);

            string? certPath = null;
            if (certificate != null)
            {
                certPath = Path.Combine(folder, certificate.Name);
                await GitHubReleases.DownloadAsync(certificate, certPath, null, cts.Token);
            }

            PackagePath = packagePath;
            CertificatePath = certPath;
            _cts = null; // not downloading any more: let OnFormClosing allow the close
            DialogResult = DialogResult.OK;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            _lblProgress.Text = "Download canceled.";
        }
        catch (Exception ex)
        {
            _lblProgress.Text = "Download failed.";
            if (!_closeWhenCanceled)
                MessageBox.Show(this, $"The download failed:\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _cts = null;
            cts.Dispose();
            if (_closeWhenCanceled)
            {
                DialogResult = DialogResult.Cancel;
            }
            else if (DialogResult != DialogResult.OK)
            {
                OkButton.Enabled = true;
                _progress.Value = 0;
            }
        }
    }

    /// <summary>Cancel / Esc / close while downloading: stop the download first, then close.</summary>
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_cts == null) return;
        e.Cancel = true;
        _closeWhenCanceled = true;
        _cts.Cancel();
    }
}
