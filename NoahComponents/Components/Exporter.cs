using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Noah.Components
{
    public class Exporter : GH_Component
    {
        private enum ExportMode
        {
            None,
            Rhino,
            DWG,
            Excel,
            Data
        }
        protected override Bitmap Icon => Properties.Resources.setvar;
        private ExportMode m_mode;
        public Exporter()
            : base("Exporter", "Exporter", "出口", "Noah", "Utils")
        {
            m_mode = ExportMode.Rhino;
        }

        public override Guid ComponentGuid => new Guid("56CABC33-DA6D-48CB-9EBB-D092174BAA70");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "E", "需要出口的内容", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Index", "X", "对应Noah包输出的序号，从0起", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0)
            {
                switch (m_mode)
                {
                    case ExportMode.None:
                        Message = null;
                        break;
                    case ExportMode.Rhino:
                        Message = "Rhino";
                        break;
                    case ExportMode.DWG:
                        Message = "DWG";
                        break;
                    case ExportMode.Excel:
                        Message = "Excel";
                        break;
                }
            }
        }

        private void writeRhino3dm (string filePath, GeometryBase G, ObjectAttributes att)
        {
            Rhino.FileIO.File3dm f = new Rhino.FileIO.File3dm();
            switch (G.ObjectType)
            {
                case ObjectType.Brep:
                    f.Objects.AddBrep(G as Brep, att);
                    break;
                case ObjectType.Curve:
                    f.Objects.AddCurve(G as Curve, att);
                    break;
                case ObjectType.Point:
                    f.Objects.AddPoint((G as Rhino.Geometry.Point).Location, att);
                    break;
                case ObjectType.Surface:
                    f.Objects.AddSurface(G as Surface, att);
                    break;
                case ObjectType.Mesh:
                    f.Objects.AddMesh(G as Mesh, att);
                    break;
                case ObjectType.PointSet:
                    f.Objects.AddPointCloud(G as PointCloud, att); //This is a speculative entry
                    break;
                default:
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "不能识别的物体: " + G.GetType().FullName);
                    break;
            }

            f.Write(filePath, 5);
            f.Dispose();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            ToolStripMenuItem toolStripMenuItem = Menu_AppendItem(
                menu, "Rhino", (object sender, EventArgs e) =>
                {
                    if (m_mode != ExportMode.Rhino)
                    {
                        RecordUndoEvent("Rhino");
                        m_mode = ExportMode.Rhino;
                        ExpireSolution(recompute: true);
                    }
                }, true, m_mode == ExportMode.Rhino);
            toolStripMenuItem.ToolTipText = 
                "将O端输入的内容写入3DM文件" + Environment.NewLine + "请确保输入的内容为可写入3DM文件的类型";
            ToolStripMenuItem toolStripMenuItem2 = Menu_AppendItem(
            menu, "DWG", (object sender, EventArgs e) =>
            {
                if (m_mode != ExportMode.DWG)
                {
                    RecordUndoEvent("DWG");
                    m_mode = ExportMode.DWG;
                    ExpireSolution(recompute: true);
                }
            }, true, m_mode == ExportMode.DWG);
                    toolStripMenuItem2.ToolTipText =
                        "将O端输入的内容写入DWG文件" + Environment.NewLine + "请确保输入的内容为可写入DWG文件的类型";
            ToolStripMenuItem toolStripMenuItem3 = Menu_AppendItem(
            menu, "Excel", (object sender, EventArgs e) =>
            {
                if (m_mode != ExportMode.Excel)
                {
                    RecordUndoEvent("Excel");
                    m_mode = ExportMode.Excel;
                    ExpireSolution(recompute: true);
                }
            }, true, m_mode == ExportMode.Excel);
                    toolStripMenuItem3.ToolTipText =
                        "将O端输入的内容写入Excel文件" + Environment.NewLine + "请确保输入的内容一个列表";
        }
    }
}
