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

		public override bool HasInputGrip => true;

		public override bool HasOutputGrip => true;

		public override bool AllowMessageBalloon => false;

		public ImporterAttr(Importer owner)
			: base(owner)
		{
		}

		protected override void Layout()
		{
            Pivot = GH_Convert.ToPoint(Pivot);
            Size size = new Size(180, 20);
            size.Width = Math.Max(size.Width, GH_FontServer.StringWidth(base.Owner.NickName, GH_FontServer.StandardAdjusted) + 20);
            int num = Convert.ToInt32((double)Pivot.X - 0.5 * (double)size.Width);
            int num2 = Convert.ToInt32((double)Pivot.X + 0.5 * (double)size.Width);
            int num3 = Convert.ToInt32((double)Pivot.Y - 0.5 * (double)size.Height);
            int num4 = Convert.ToInt32((double)Pivot.Y + 0.5 * (double)size.Height);
            Bounds = RectangleF.FromLTRB(num, num3, num2, num4);
            TextBound = RectangleF.FromLTRB(num + 30, num3, num2, num4);
            m_button = new RectangleF(Bounds.Left, Bounds.Top, 30, Bounds.Height);
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
                gH_Capsule2.Text = "Import";
                gH_Capsule2.Render(graphics, Color.AliceBlue);
                gH_Capsule2.Dispose();
            }
            else if (channel ==GH_CanvasChannel.Wires && Owner.SourceCount == 1)
			{
				Color color = Utility.TernaryIf<Color>(Selected, GH_Skin.wire_selected_a, GH_Skin.wire_default);
				if (base.Owner.Locked)
				{
					color = Color.FromArgb(50, color);
				}
				Rectangle r = GH_Convert.ToRectangle(base.Owner.Attributes.Bounds);
				RenderTimerConnection(graphics, base.Owner.Sources[0].Attributes.OutputGrip, r, color);
			}
		}

		public static void RenderTimerConnection(Graphics g, PointF anchor, RectangleF box, Color col)
		{
			if (box.Contains(anchor))
			{
				return;
			}
			Pen pen = new Pen(col, 3f);
			pen.StartCap = LineCap.Round;
			pen.DashCap = DashCap.Round;
			pen.EndCap = LineCap.Round;
			pen.DashPattern = new float[2]
			{
				1f,
				0.8f
			};
			PointF pt = GH_GraphicsUtil.BoxClosestPoint(anchor + new SizeF(20f, 0f), box);
			if ((double)Math.Abs(box.Y - anchor.Y) < 1.0)
			{
				g.DrawLine(pen, pt, anchor);
			}
			else
			{
				List<PointF> list = new List<PointF>();
				list.Add(new PointF(anchor.X, anchor.Y));
				if (pt.X < anchor.X + 20f)
				{
					list.Add(new PointF(anchor.X + 20f, anchor.Y));
					list.Add(new PointF(anchor.X + 20f, pt.Y));
					list.Add(new PointF(pt.X, pt.Y));
				}
				else
				{
					list.Add(new PointF(pt.X, anchor.Y));
					list.Add(new PointF(pt.X, pt.Y));
				}
				list.Reverse();
				GraphicsPath graphicsPath = GH_GDI_Util.FilletPolyline(list.ToArray(), 20f);
				g.DrawPath(pen, graphicsPath);
				graphicsPath.Dispose();
			}
			pen.Dispose();
		}
	}
}
