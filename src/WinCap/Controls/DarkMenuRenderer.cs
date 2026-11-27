namespace WinCap.Controls;

/// <summary>Dark renderer for the main "⋯" drop-down menu.</summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Text : Theme.TextDim;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Theme.TextMuted;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // Accent check mark instead of the default boxed check image.
        using var font = new Font(Theme.IconFont, e.Item.Font.Size, GraphicsUnit.Point);
        TextRenderer.DrawText(e.Graphics, "", font, e.ImageRectangle, Theme.Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Raised;
        public override Color ImageMarginGradientBegin => Theme.Raised;
        public override Color ImageMarginGradientMiddle => Theme.Raised;
        public override Color ImageMarginGradientEnd => Theme.Raised;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Theme.RaisedSelected;
        public override Color MenuItemSelected => Theme.RaisedSelected;
        public override Color MenuItemSelectedGradientBegin => Theme.RaisedSelected;
        public override Color MenuItemSelectedGradientEnd => Theme.RaisedSelected;
        public override Color MenuItemPressedGradientBegin => Theme.RaisedSelected;
        public override Color MenuItemPressedGradientEnd => Theme.RaisedSelected;
        public override Color CheckBackground => Theme.Raised;
        public override Color CheckSelectedBackground => Theme.RaisedSelected;
        public override Color CheckPressedBackground => Theme.RaisedSelected;
        public override Color SeparatorDark => Theme.Border;
        public override Color SeparatorLight => Theme.Raised;
    }
}
