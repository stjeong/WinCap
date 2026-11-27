using WinCap.Settings;

namespace WinCap.Dialogs;

/// <summary>File &gt; Settings: save folder, image format and external editor.</summary>
public sealed class SettingsDialog : DialogBase
{
    private readonly TextBox _txtFolder;
    private readonly ComboBox _cboFormat;
    private readonly TextBox _txtEditor;
    private readonly CheckBox _chkScaleToDpi;
    private readonly CheckBox _chkRunAtStartup;

    public string SaveFolder => _txtFolder.Text.Trim();
    public string ImageFormat => (string)_cboFormat.SelectedItem!;
    public string EditorPath => _txtEditor.Text.Trim();
    public bool ScaleToDpi => _chkScaleToDpi.Checked;
    public bool RunAtStartup => _chkRunAtStartup.Checked;

    public SettingsDialog(AppSettings settings)
    {
        Text = "Settings";
        Table.ColumnCount = 4;
        Table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Save folder
        _txtFolder = new TextBox { Text = settings.SaveFolder, Width = 340, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var btnFolder = MakeButton("Browse...");
        btnFolder.Click += (_, _) => BrowseFolder();
        Table.Controls.Add(MakeLabel("Save folder:"), 0, 0);
        Table.Controls.Add(_txtFolder, 1, 0);
        Table.Controls.Add(btnFolder, 2, 0);

        // Image format
        _cboFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, Anchor = AnchorStyles.Left };
        _cboFormat.Items.AddRange(["PNG", "JPG", "BMP"]);
        int idx = _cboFormat.Items.IndexOf(settings.ImageFormat.ToUpperInvariant());
        _cboFormat.SelectedIndex = idx >= 0 ? idx : 0;
        Table.Controls.Add(MakeLabel("Image format:"), 0, 1);
        Table.Controls.Add(_cboFormat, 1, 1);

        // Save at 100 % scale (compensate monitor DPI scaling)
        _chkScaleToDpi = new CheckBox
        {
            Text = "Save at 100% scale (shrink by monitor DPI, e.g. 175%: 175 px -> 100 px)",
            Checked = settings.ScaleToDpi,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 6),
        };
        Table.Controls.Add(_chkScaleToDpi, 1, 2);
        Table.SetColumnSpan(_chkScaleToDpi, 3);

        // Editor
        _txtEditor = new TextBox { Text = settings.EditorPath, Width = 340, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var btnEditor = MakeButton("Browse...");
        btnEditor.Click += (_, _) => BrowseEditor();
        var btnDefault = MakeButton("Default (Paint)");
        btnDefault.Click += (_, _) => _txtEditor.Text = AppSettings.DefaultEditor;
        Table.Controls.Add(MakeLabel("Editor program:"), 0, 3);
        Table.Controls.Add(_txtEditor, 1, 3);
        Table.Controls.Add(btnEditor, 2, 3);
        Table.Controls.Add(btnDefault, 3, 3);

        var note = new Label
        {
            Text = "The editor is used by \"Preview in Editor\". Default: MS Paint (mspaint.exe).",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 4, 0, 0),
        };
        Table.Controls.Add(note, 1, 4);
        Table.SetColumnSpan(note, 3);

        // Run at Windows startup (HKCU\...\CurrentVersion\Run)
        _chkRunAtStartup = new CheckBox
        {
            Text = "Start WinCap when Windows starts",
            Checked = StartupRegistration.IsEnabled,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0),
        };
        Table.Controls.Add(_chkRunAtStartup, 1, 5);
        Table.SetColumnSpan(_chkRunAtStartup, 3);

        FormClosing += OnFormClosing;
        FinishLayout();
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select the folder where captures are saved",
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(_txtFolder.Text) ? _txtFolder.Text : string.Empty,
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtFolder.Text = dlg.SelectedPath;
    }

    private void BrowseEditor()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select editor program",
            Filter = "Programs (*.exe)|*.exe|All files (*.*)|*.*",
        };
        if (File.Exists(_txtEditor.Text))
            dlg.InitialDirectory = Path.GetDirectoryName(_txtEditor.Text);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtEditor.Text = dlg.FileName;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK) return;

        if (string.IsNullOrWhiteSpace(SaveFolder))
        {
            MessageBox.Show(this, "Please enter a save folder.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        try
        {
            Directory.CreateDirectory(SaveFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Cannot use the save folder:\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(EditorPath))
            _txtEditor.Text = AppSettings.DefaultEditor;
    }
}
