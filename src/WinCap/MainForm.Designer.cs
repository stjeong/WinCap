using WinCap.Controls;

namespace WinCap
{
    partial class MainForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pnlTitle = new Panel();
            lblLogo = new Label();
            lblTitle = new Label();
            segMode = new SegmentedControl();
            btnSettings = new Button();
            btnMore = new Button();
            btnMinimize = new Button();
            btnClose = new Button();
            mnuMain = new ContextMenuStrip(components);
            mnuFileCapture = new ToolStripMenuItem();
            mnuFileOpenFolder = new ToolStripMenuItem();
            mnuEditCopyLast = new ToolStripMenuItem();
            mnuEditOpenLast = new ToolStripMenuItem();
            mnuSep1 = new ToolStripSeparator();
            mnuEditHotkey = new ToolStripMenuItem();
            mnuFileSettings = new ToolStripMenuItem();
            mnuViewAlwaysOnTop = new ToolStripMenuItem();
            mnuSep2 = new ToolStripSeparator();
            mnuHelpAbout = new ToolStripMenuItem();
            mnuFileExit = new ToolStripMenuItem();
            pnlBody = new Panel();
            lblAreaCaption = new Label();
            segSelection = new SegmentedControl();
            chkTrimMargins = new CheckBox();
            lblEffects = new Label();
            segEffects = new SegmentedControl();
            btnEffectsSettings = new Button();
            pnlSeparatorV = new Panel();
            lblOptionsCaption = new Label();
            btnReset = new Button();
            lblPreview = new Label();
            tglPreview = new ToggleSwitch();
            lblClipboard = new Label();
            tglClipboard = new ToggleSwitch();
            lblCursor = new Label();
            tglCursor = new ToggleSwitch();
            lblDelay = new Label();
            btnDelayOptions = new Button();
            tglDelay = new ToggleSwitch();
            pnlSeparatorV2 = new Panel();
            btnCapture = new AccentButton();
            lnkHotkey = new LinkLabel();
            pnlStatus = new Panel();
            lblStatus = new Label();
            btnOpenFolder = new Button();
            pnlStatusLine = new Panel();
            toolTip = new ToolTip(components);
            pnlTitle.SuspendLayout();
            mnuMain.SuspendLayout();
            pnlBody.SuspendLayout();
            pnlStatus.SuspendLayout();
            SuspendLayout();
            //
            // pnlTitle
            //
            pnlTitle.BackColor = Theme.Header;
            pnlTitle.Controls.Add(lblLogo);
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(segMode);
            pnlTitle.Controls.Add(btnSettings);
            pnlTitle.Controls.Add(btnMore);
            pnlTitle.Controls.Add(btnMinimize);
            pnlTitle.Controls.Add(btnClose);
            pnlTitle.Dock = DockStyle.Top;
            pnlTitle.Name = "pnlTitle";
            pnlTitle.Size = new Size(658, 44);
            pnlTitle.TabIndex = 0;
            pnlTitle.MouseDown += TitleBar_MouseDown;
            //
            // lblLogo
            //
            lblLogo.BackColor = Color.Transparent;
            lblLogo.ImageAlign = ContentAlignment.MiddleCenter;
            lblLogo.Location = new Point(14, 12);
            lblLogo.Name = "lblLogo";
            lblLogo.Size = new Size(20, 20);
            lblLogo.Text = "";
            lblLogo.MouseDown += TitleBar_MouseDown;
            //
            // lblTitle
            //
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI Semibold", 10.5F);
            lblTitle.ForeColor = Theme.Text;
            lblTitle.Location = new Point(40, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "WinCap";
            lblTitle.MouseDown += TitleBar_MouseDown;
            //
            // segMode
            //
            segMode.Font = new Font("Segoe UI", 9F);
            segMode.Glyphs = new string[] { "", "" };
            segMode.Items = new string[] { "Image", "Video" };
            segMode.Location = new Point(124, 8);
            segMode.Name = "segMode";
            segMode.Size = new Size(168, 28);
            segMode.TabIndex = 0;
            segMode.SelectedIndexChanged += segMode_SelectedIndexChanged;
            //
            // btnSettings
            //
            ConfigureTitleButton(btnSettings, "", 482);
            btnSettings.Name = "btnSettings";
            btnSettings.Click += mnuFileSettings_Click;
            //
            // btnMore
            //
            ConfigureTitleButton(btnMore, "", 526);
            btnMore.Name = "btnMore";
            btnMore.AccessibleName = "Menu";
            btnMore.Click += btnMore_Click;
            //
            // btnMinimize
            //
            ConfigureTitleButton(btnMinimize, "", 570);
            btnMinimize.Font = new Font(Theme.IconFont, 8F);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Click += btnMinimize_Click;
            //
            // btnClose
            //
            ConfigureTitleButton(btnClose, "", 614);
            btnClose.Font = new Font(Theme.IconFont, 8F);
            btnClose.FlatAppearance.MouseOverBackColor = Theme.CloseHover;
            btnClose.Name = "btnClose";
            btnClose.Click += btnClose_Click;
            //
            // mnuMain
            //
            mnuMain.Font = new Font("Segoe UI", 9.75F);
            mnuMain.Items.AddRange(new ToolStripItem[] { mnuFileCapture, mnuFileOpenFolder, mnuEditCopyLast, mnuEditOpenLast, mnuSep1, mnuEditHotkey, mnuFileSettings, mnuViewAlwaysOnTop, mnuSep2, mnuHelpAbout, mnuFileExit });
            mnuMain.Name = "mnuMain";
            mnuMain.ShowImageMargin = false;
            mnuMain.ShowCheckMargin = true;
            //
            // menu items
            //
            mnuFileCapture.Name = "mnuFileCapture";
            mnuFileCapture.Text = "&Capture";
            mnuFileCapture.Click += btnCapture_Click;
            mnuFileOpenFolder.Name = "mnuFileOpenFolder";
            mnuFileOpenFolder.Text = "&Open Capture Folder";
            mnuFileOpenFolder.Click += mnuFileOpenFolder_Click;
            mnuEditCopyLast.Name = "mnuEditCopyLast";
            mnuEditCopyLast.Text = "Copy &Last Capture";
            mnuEditCopyLast.Click += mnuEditCopyLast_Click;
            mnuEditOpenLast.Name = "mnuEditOpenLast";
            mnuEditOpenLast.Text = "Open Last Capture in &Editor";
            mnuEditOpenLast.Click += mnuEditOpenLast_Click;
            mnuSep1.Name = "mnuSep1";
            mnuEditHotkey.Name = "mnuEditHotkey";
            mnuEditHotkey.Text = "&Hotkey...";
            mnuEditHotkey.Click += lnkHotkey_LinkClicked;
            mnuFileSettings.Name = "mnuFileSettings";
            mnuFileSettings.Text = "&Settings...";
            mnuFileSettings.Click += mnuFileSettings_Click;
            mnuViewAlwaysOnTop.CheckOnClick = true;
            mnuViewAlwaysOnTop.Name = "mnuViewAlwaysOnTop";
            mnuViewAlwaysOnTop.Text = "&Always on Top";
            mnuViewAlwaysOnTop.CheckedChanged += mnuViewAlwaysOnTop_CheckedChanged;
            mnuSep2.Name = "mnuSep2";
            mnuHelpAbout.Name = "mnuHelpAbout";
            mnuHelpAbout.Text = "A&bout WinCap";
            mnuHelpAbout.Click += mnuHelpAbout_Click;
            mnuFileExit.Name = "mnuFileExit";
            mnuFileExit.Text = "E&xit";
            mnuFileExit.Click += mnuFileExit_Click;
            //
            // pnlBody
            //
            pnlBody.BackColor = Theme.Surface;
            pnlBody.Controls.Add(lblAreaCaption);
            pnlBody.Controls.Add(segSelection);
            pnlBody.Controls.Add(chkTrimMargins);
            pnlBody.Controls.Add(lblEffects);
            pnlBody.Controls.Add(segEffects);
            pnlBody.Controls.Add(btnEffectsSettings);
            pnlBody.Controls.Add(pnlSeparatorV);
            pnlBody.Controls.Add(lblOptionsCaption);
            pnlBody.Controls.Add(btnReset);
            pnlBody.Controls.Add(lblPreview);
            pnlBody.Controls.Add(tglPreview);
            pnlBody.Controls.Add(lblClipboard);
            pnlBody.Controls.Add(tglClipboard);
            pnlBody.Controls.Add(lblCursor);
            pnlBody.Controls.Add(tglCursor);
            pnlBody.Controls.Add(lblDelay);
            pnlBody.Controls.Add(btnDelayOptions);
            pnlBody.Controls.Add(tglDelay);
            pnlBody.Controls.Add(pnlSeparatorV2);
            pnlBody.Controls.Add(btnCapture);
            pnlBody.Controls.Add(lnkHotkey);
            pnlBody.Dock = DockStyle.Fill;
            pnlBody.ForeColor = Theme.Text;
            pnlBody.Name = "pnlBody";
            pnlBody.Size = new Size(658, 176);
            pnlBody.TabIndex = 1;
            //
            // lblAreaCaption
            //
            ConfigureCaption(lblAreaCaption, "CAPTURE AREA", 20);
            lblAreaCaption.Name = "lblAreaCaption";
            //
            // segSelection
            //
            segSelection.Font = new Font("Segoe UI", 9F);
            segSelection.Glyphs = new string[] { "", "", "" };
            segSelection.Items = new string[] { "Region", "Window", "Fullscreen" };
            segSelection.Location = new Point(20, 36);
            segSelection.Name = "segSelection";
            segSelection.Size = new Size(258, 64);
            segSelection.TabIndex = 1;
            segSelection.Tiles = true;
            segSelection.SelectedIndexChanged += segSelection_SelectedIndexChanged;
            //
            // chkTrimMargins
            //
            chkTrimMargins.AutoSize = true;
            chkTrimMargins.Checked = true;
            chkTrimMargins.CheckState = CheckState.Checked;
            chkTrimMargins.FlatAppearance.BorderColor = Theme.TextDim;
            chkTrimMargins.FlatAppearance.CheckedBackColor = Theme.Raised;
            chkTrimMargins.FlatAppearance.MouseOverBackColor = Theme.RaisedHover;
            chkTrimMargins.FlatStyle = FlatStyle.Flat;
            chkTrimMargins.Font = new Font("Segoe UI", 9F);
            chkTrimMargins.ForeColor = Theme.TextMuted;
            chkTrimMargins.Location = new Point(21, 108);
            chkTrimMargins.Name = "chkTrimMargins";
            chkTrimMargins.TabIndex = 2;
            chkTrimMargins.Text = "Trim transparent window margins";
            chkTrimMargins.UseVisualStyleBackColor = false;
            chkTrimMargins.CheckedChanged += chkTrimMargins_CheckedChanged;
            //
            // lblEffects
            //
            lblEffects.AutoSize = true;
            lblEffects.Font = new Font("Segoe UI", 9.75F);
            lblEffects.ForeColor = Theme.TextMuted;
            lblEffects.Location = new Point(18, 141);
            lblEffects.Name = "lblEffects";
            lblEffects.Text = "Effect";
            //
            // segEffects
            //
            segEffects.Font = new Font("Segoe UI", 9F);
            segEffects.Items = new string[] { "None", "Border" };
            segEffects.Location = new Point(74, 136);
            segEffects.Name = "segEffects";
            segEffects.Size = new Size(150, 28);
            segEffects.TabIndex = 3;
            segEffects.SelectedIndexChanged += segEffects_SelectedIndexChanged;
            //
            // btnEffectsSettings
            //
            ConfigureIconButton(btnEffectsSettings, "", new Point(230, 136), new Size(28, 28), 10F);
            btnEffectsSettings.Name = "btnEffectsSettings";
            btnEffectsSettings.TabIndex = 4;
            btnEffectsSettings.Click += btnEffectsSettings_Click;
            //
            // pnlSeparatorV
            //
            pnlSeparatorV.BackColor = Theme.Divider;
            pnlSeparatorV.Location = new Point(298, 16);
            pnlSeparatorV.Name = "pnlSeparatorV";
            pnlSeparatorV.Size = new Size(1, 146);
            //
            // lblOptionsCaption
            //
            ConfigureCaption(lblOptionsCaption, "OPTIONS", 318);
            lblOptionsCaption.Name = "lblOptionsCaption";
            //
            // btnReset
            //
            ConfigureIconButton(btnReset, "", new Point(478, 10), new Size(26, 24), 9F);
            btnReset.Name = "btnReset";
            btnReset.TabIndex = 5;
            btnReset.Click += btnReset_Click;
            //
            // option rows
            //
            ConfigureOptionRow(lblPreview, tglPreview, "Preview in editor", 38, 6);
            lblPreview.Name = "lblPreview";
            tglPreview.Name = "tglPreview";
            tglPreview.CheckedChanged += tglPreview_CheckedChanged;
            ConfigureOptionRow(lblClipboard, tglClipboard, "Copy to clipboard", 70, 7);
            lblClipboard.Name = "lblClipboard";
            tglClipboard.Name = "tglClipboard";
            tglClipboard.CheckedChanged += tglClipboard_CheckedChanged;
            ConfigureOptionRow(lblCursor, tglCursor, "Capture cursor", 102, 8);
            lblCursor.Name = "lblCursor";
            tglCursor.Name = "tglCursor";
            tglCursor.CheckedChanged += tglCursor_CheckedChanged;
            ConfigureOptionRow(lblDelay, tglDelay, "Time delay", 134, 10);
            lblDelay.Name = "lblDelay";
            tglDelay.Name = "tglDelay";
            tglDelay.CheckedChanged += tglDelay_CheckedChanged;
            //
            // btnDelayOptions
            //
            ConfigureIconButton(btnDelayOptions, "", new Point(436, 132), new Size(24, 24), 8.25F);
            btnDelayOptions.Name = "btnDelayOptions";
            btnDelayOptions.TabIndex = 9;
            btnDelayOptions.Click += btnDelayOptions_Click;
            //
            // pnlSeparatorV2
            //
            pnlSeparatorV2.BackColor = Theme.Divider;
            pnlSeparatorV2.Location = new Point(522, 16);
            pnlSeparatorV2.Name = "pnlSeparatorV2";
            pnlSeparatorV2.Size = new Size(1, 146);
            //
            // btnCapture
            //
            btnCapture.Font = new Font("Segoe UI Semibold", 10F);
            btnCapture.Glyph = "";
            btnCapture.Location = new Point(538, 16);
            btnCapture.Name = "btnCapture";
            btnCapture.Size = new Size(106, 106);
            btnCapture.TabIndex = 11;
            btnCapture.Text = "Capture";
            btnCapture.UseVisualStyleBackColor = false;
            btnCapture.Click += btnCapture_Click;
            //
            // lnkHotkey
            //
            lnkHotkey.ActiveLinkColor = Theme.Accent;
            lnkHotkey.AutoEllipsis = true;
            lnkHotkey.Font = new Font("Segoe UI", 8.25F);
            lnkHotkey.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkHotkey.LinkColor = Theme.TextMuted;
            lnkHotkey.Location = new Point(526, 130);
            lnkHotkey.Name = "lnkHotkey";
            lnkHotkey.Size = new Size(130, 30);
            lnkHotkey.TabIndex = 12;
            lnkHotkey.TabStop = true;
            lnkHotkey.Text = "Add Hotkey";
            lnkHotkey.TextAlign = ContentAlignment.MiddleCenter;
            lnkHotkey.LinkClicked += lnkHotkey_LinkClicked;
            //
            // pnlStatus
            //
            pnlStatus.BackColor = Theme.Header;
            pnlStatus.Controls.Add(lblStatus);
            pnlStatus.Controls.Add(btnOpenFolder);
            pnlStatus.Controls.Add(pnlStatusLine);
            pnlStatus.Dock = DockStyle.Bottom;
            pnlStatus.Name = "pnlStatus";
            pnlStatus.Size = new Size(658, 30);
            pnlStatus.TabIndex = 2;
            //
            // lblStatus
            //
            lblStatus.AutoEllipsis = true;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Theme.TextMuted;
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(14, 0, 10, 0);
            lblStatus.Text = "Ready";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.DoubleClick += lblStatus_DoubleClick;
            //
            // btnOpenFolder
            //
            btnOpenFolder.AccessibleName = "Open Folder";
            btnOpenFolder.Dock = DockStyle.Right;
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.FlatAppearance.MouseDownBackColor = Theme.RaisedSelected;
            btnOpenFolder.FlatAppearance.MouseOverBackColor = Theme.RaisedHover;
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.Font = new Font("Segoe UI", 9F);
            btnOpenFolder.ForeColor = Theme.Text;
            btnOpenFolder.Name = "btnOpenFolder";
            btnOpenFolder.Size = new Size(124, 29);
            btnOpenFolder.TabIndex = 0;
            btnOpenFolder.TabStop = false;
            btnOpenFolder.Text = ""; // glyph + caption drawn in btnOpenFolder_Paint
            btnOpenFolder.UseVisualStyleBackColor = false;
            btnOpenFolder.Click += btnOpenFolder_Click;
            btnOpenFolder.Paint += btnOpenFolder_Paint;
            //
            // pnlStatusLine
            //
            pnlStatusLine.BackColor = Theme.Divider;
            pnlStatusLine.Dock = DockStyle.Top;
            pnlStatusLine.Name = "pnlStatusLine";
            pnlStatusLine.Size = new Size(658, 1);
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Theme.Border;
            ClientSize = new Size(660, 252);
            Controls.Add(pnlBody);
            Controls.Add(pnlStatus);
            Controls.Add(pnlTitle);
            Font = new Font("Segoe UI", 9.75F);
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            Name = "MainForm";
            Padding = new Padding(1);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WinCap";
            pnlTitle.ResumeLayout(false);
            pnlTitle.PerformLayout();
            mnuMain.ResumeLayout(false);
            pnlBody.ResumeLayout(false);
            pnlBody.PerformLayout();
            pnlStatus.ResumeLayout(false);
            ResumeLayout(false);
        }

        private static void ConfigureTitleButton(Button b, string glyph, int x)
        {
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.RaisedHover;
            b.FlatAppearance.MouseDownBackColor = Theme.RaisedSelected;
            b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font(Theme.IconFont, 10F);
            b.ForeColor = Theme.TextMuted;
            b.Location = new Point(x, 0);
            b.Size = new Size(44, 44);
            b.TabStop = false;
            b.Text = glyph;
            b.UseVisualStyleBackColor = false;
        }

        private static void ConfigureIconButton(Button b, string glyph, Point location, Size size, float fontSize)
        {
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.RaisedHover;
            b.FlatAppearance.MouseDownBackColor = Theme.RaisedSelected;
            b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font(Theme.IconFont, fontSize);
            b.ForeColor = Theme.TextMuted;
            b.Location = location;
            b.Size = size;
            b.Text = glyph;
            b.UseVisualStyleBackColor = false;
        }

        private static void ConfigureCaption(Label l, string text, int x)
        {
            l.AutoSize = true;
            l.Font = new Font("Segoe UI Semibold", 7.5F);
            l.ForeColor = Theme.TextDim;
            l.Location = new Point(x - 2, 14);
            l.Text = text;
        }

        private static void ConfigureOptionRow(Label label, ToggleSwitch toggle, string text, int y, int tabIndex)
        {
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9.75F);
            label.ForeColor = Theme.Text;
            label.Location = new Point(316, y + 1);
            label.Text = text;
            toggle.Location = new Point(468, y);
            toggle.Size = new Size(36, 20);
            toggle.TabIndex = tabIndex;
        }

        #endregion

        private Panel pnlTitle;
        private Label lblLogo;
        private Label lblTitle;
        private SegmentedControl segMode;
        private Button btnSettings;
        private Button btnMore;
        private Button btnMinimize;
        private Button btnClose;
        private ContextMenuStrip mnuMain;
        private ToolStripMenuItem mnuFileCapture;
        private ToolStripMenuItem mnuFileOpenFolder;
        private ToolStripMenuItem mnuEditCopyLast;
        private ToolStripMenuItem mnuEditOpenLast;
        private ToolStripSeparator mnuSep1;
        private ToolStripMenuItem mnuEditHotkey;
        private ToolStripMenuItem mnuFileSettings;
        private ToolStripMenuItem mnuViewAlwaysOnTop;
        private ToolStripSeparator mnuSep2;
        private ToolStripMenuItem mnuHelpAbout;
        private ToolStripMenuItem mnuFileExit;
        private Panel pnlBody;
        private Label lblAreaCaption;
        private SegmentedControl segSelection;
        private CheckBox chkTrimMargins;
        private Label lblEffects;
        private SegmentedControl segEffects;
        private Button btnEffectsSettings;
        private Panel pnlSeparatorV;
        private Label lblOptionsCaption;
        private Button btnReset;
        private Label lblPreview;
        private ToggleSwitch tglPreview;
        private Label lblClipboard;
        private ToggleSwitch tglClipboard;
        private Label lblCursor;
        private ToggleSwitch tglCursor;
        private Label lblDelay;
        private Button btnDelayOptions;
        private ToggleSwitch tglDelay;
        private Panel pnlSeparatorV2;
        private AccentButton btnCapture;
        private LinkLabel lnkHotkey;
        private Panel pnlStatus;
        private Label lblStatus;
        private Button btnOpenFolder;
        private Panel pnlStatusLine;
        private ToolTip toolTip;
    }
}
