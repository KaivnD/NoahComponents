using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Noah.Components
{
	public class ImporterAttr : GH_Attributes<Importer>
	{
		private RectangleF m_button;

		private RectangleF TextBound;

		public override bool HasInputGrip => false;

		public override bool HasOutputGrip => true;

		public override bool AllowMessageBalloon => false;

		public ImporterAttr(Importer owner)
			: base(owner)
		{
		}

		protected override void Layout()
		{
            Pivot = GH_Convert.ToPoint(Pivot);
            Size size = new Size(160, 18);
            size.Width = Math.Max(size.Width, GH_FontServer.StringWidth(base.Owner.NickName, GH_FontServer.StandardAdjusted) + 20);
            int num = Convert.ToInt32((double)Pivot.X - 0.5 * (double)size.Width);
            int num2 = Convert.ToInt32((double)Pivot.X + 0.5 * (double)size.Width);
            int num3 = Convert.ToInt32((double)Pivot.Y - 0.5 * (double)size.Height);
            int num4 = Convert.ToInt32((double)Pivot.Y + 0.5 * (double)size.Height);
            Bounds = RectangleF.FromLTRB(num, num3, num2, num4);
            TextBound = RectangleF.FromLTRB(num + 60, num3, num2, num4);
            m_button = new RectangleF(Bounds.Left, Bounds.Top, 60, Bounds.Height);
        }

		public override void ExpireLayout()
		{
			base.ExpireLayout();
		}

		protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
		{
            if (channel == GH_CanvasChannel.Objects)
            {
                bool num = !string.IsNullOrEmpty(base.Owner.NickName);
                GH_Capsule gH_Capsule = (!num) ? GH_Capsule.CreateCapsule(Bounds, GH_Palette.White, 3, 0) : GH_Capsule.CreateTextCapsule(Bounds, TextBound, GH_Palette.White, base.Owner.NickName, 3, 0);

                GH_PaletteStyle impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(GH_Palette.White, Selected, base.Owner.Locked, hidden: true);
                gH_Capsule.RenderEngine.RenderOutlines(graphics, canvas.Viewport.Zoom, impliedStyle);
                GH_CapsuleRenderEngine.RenderOutputGrip(graphics, canvas.Viewport.Zoom, OutputGrip, full: true);
                if (num)
                {
                    gH_Capsule.RenderEngine.RenderText(graphics, impliedStyle.Text);
                }
                gH_Capsule.Dispose();

                GH_Capsule gH_Capsule2 = GH_Capsule.CreateCapsule(m_button, GH_Palette.Black, 3, 0);
                gH_Capsule2.TextOrientation = GH_Orientation.horizontal_center;
                gH_Capsule2.Font = GH_FontServer.ConsoleAdjusted;
                gH_Capsule2.Text = "Import";//
                gH_Capsule2.Render(graphics, Color.AliceBlue);
                gH_Capsule2.Dispose();
            }
		}
	}
}
