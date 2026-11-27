namespace WinCap.Dialogs;

/// <summary>Common look and layout helpers for the small modal dialogs.</summary>
/// <remarks>
/// Layout: one auto-sized root table with two rows: the dialog content (<see cref="Table"/>)
/// and a single-line button row. The button row has extra buttons (<see cref="ExtraButtonPanel"/>)
/// on the left and OK / Cancel on the right. Nothing is docked, so the auto-sized form
/// measures its content correctly and the rows never overlap.
/// </remarks>
public class DialogBase : Form
{
    private const int DialogPadding = 12;

    /// <summary>Content area for the derived dialog.</summary>
    protected readonly TableLayoutPanel Table;

    /// <summary>Left part of the button row, for buttons other than OK / Cancel.</summary>
    protected readonly FlowLayoutPanel ExtraButtonPanel;

    protected readonly Button OkButton;
    protected readonly Button CancelButtonControl;

    private readonly TableLayoutPanel _root;

    public DialogBase()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        BackColor = WinCap.Controls.Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(0, 0, DialogPadding, DialogPadding);

        Table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
        };

        OkButton = MakeButton("OK", DialogResult.OK);
        CancelButtonControl = MakeButton("Cancel", DialogResult.Cancel);

        ExtraButtonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Left,
        };

        var buttonRow = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            Dock = DockStyle.Fill, // fills the root column so OK / Cancel sit at the right edge
            Margin = new Padding(0, 14, 0, 0),
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.Controls.Add(ExtraButtonPanel, 0, 0);
        buttonRow.Controls.Add(OkButton, 1, 0);
        buttonRow.Controls.Add(CancelButtonControl, 2, 0);

        _root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Location = new Point(DialogPadding, DialogPadding),
            Margin = new Padding(0),
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.Controls.Add(Table, 0, 0);
        _root.Controls.Add(buttonRow, 0, 1);

        Controls.Add(_root);
        AcceptButton = OkButton;
        CancelButton = CancelButtonControl;
    }

    /// <summary>Call at the end of the derived constructor.</summary>
    protected void FinishLayout()
    {
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>Compact dialog button: standard height, at least 76 px wide, grows for longer text.</summary>
    protected static Button MakeButton(string text, DialogResult result = DialogResult.None) => new()
    {
        Text = text,
        DialogResult = result,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(76, 0),
        Padding = new Padding(6, 0, 6, 0),
        Margin = new Padding(6, 0, 0, 0),
        UseVisualStyleBackColor = true,
    };

    protected static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 6, 10, 6),
    };
}
