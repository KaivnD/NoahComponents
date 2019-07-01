using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;

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
            pManager.AddGenericParameter("Object", "O", "需要出口的内容", GH_ParamAccess.item);
            pManager[0].Optional = true;
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
                }, enabled: true, (m_mode == ExportMode.Rhino) ? true : false);
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
            }, enabled: true, (m_mode == ExportMode.DWG) ? true : false);
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
            }, enabled: true, (m_mode == ExportMode.Excel) ? true : false);
                    toolStripMenuItem3.ToolTipText =
                        "将O端输入的内容写入Excel文件" + Environment.NewLine + "请确保输入的内容一个列表";
        }
    }
}
