using System.Drawing;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    public class StardewColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder { get { return ColorTranslator.FromHtml("#612D0F"); } }
        public override Color MenuItemBorder { get { return ColorTranslator.FromHtml("#9E5524"); } }
        public override Color MenuItemSelected { get { return ColorTranslator.FromHtml("#F4BD6D"); } }
        public override Color MenuItemSelectedGradientBegin { get { return ColorTranslator.FromHtml("#F7C97E"); } }
        public override Color MenuItemSelectedGradientEnd { get { return ColorTranslator.FromHtml("#EBA952"); } }
        public override Color ToolStripDropDownBackground { get { return ColorTranslator.FromHtml("#FCE0AC"); } }
        public override Color ImageMarginGradientBegin { get { return ColorTranslator.FromHtml("#F4CC88"); } }
        public override Color ImageMarginGradientMiddle { get { return ColorTranslator.FromHtml("#F4CC88"); } }
        public override Color ImageMarginGradientEnd { get { return ColorTranslator.FromHtml("#F4CC88"); } }
        public override Color SeparatorDark { get { return ColorTranslator.FromHtml("#B87A40"); } }
        public override Color SeparatorLight { get { return ColorTranslator.FromHtml("#FFEBC2"); } }
        public override Color CheckBackground { get { return ColorTranslator.FromHtml("#EBA952"); } }
        public override Color CheckSelectedBackground { get { return ColorTranslator.FromHtml("#D98E34"); } }
    }

    public class StardewMenuRenderer : ToolStripProfessionalRenderer
    {
        public StardewMenuRenderer() : base(new StardewColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen outer = new Pen(ColorTranslator.FromHtml("#592A0E"), 2))
            using (Pen inner = new Pen(ColorTranslator.FromHtml("#C4773B"), 1))
            {
                Rectangle r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                e.Graphics.DrawRectangle(outer, r);
                e.Graphics.DrawRectangle(inner, 2, 2, e.ToolStrip.Width - 5, e.ToolStrip.Height - 5);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = ColorTranslator.FromHtml("#4A220A");
            base.OnRenderItemText(e);
        }
    }
}
