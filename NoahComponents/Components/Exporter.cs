using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.FileIO;
using System.IO;
using System.Collections.Generic;

namespace Noah.Components
{
    public class Exporter : GH_Component , IGH_VariableParameterComponent
    {
        private enum ExportMode
        {
            None,
            Rhino,
            Text
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
            pManager.AddGenericParameter("物件", "E", "需要出口的内容", GH_ParamAccess.list);
            pManager.AddIntegerParameter("序号", "X", "对应Noah包输出的序号，从0起", GH_ParamAccess.item);
            pManager.AddTextParameter("属性", "A", "对应的图层属性", GH_ParamAccess.item);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            switch (m_mode)
            {
                case ExportMode.None:
                    Message = null;
                    break;
                case ExportMode.Rhino:
                    Message = "Rhino";
                    string filePath = @"D:\Desktop\test.3dm";
                    string layerName = "默认值";
                    List<object> geo = new List<object>();
                    DA.GetDataList(0, geo);
                    DA.GetData(2, ref layerName);
                    layerName = layerName.ToString();
                    File3dm f = null;
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            f = File3dm.Read(filePath);
                            f.Objects.Clear();
                        }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                        }
                    }
                    else
                    {
                        f = new File3dm();
                    }

                    ObjectAttributes att = getObjAttr(layerName, f, Color.Black);
                    if (geo != null)
                    {
                        writeRhino3dm(f, filePath, geo, att);
                    }

                    break;
                case ExportMode.Text:
                    Message = "Text";
                    break;
            }
        }

        private void writeRhino3dm (File3dm f, string filePath, List<object> G, ObjectAttributes att)
        {
            G.ForEach(x =>
            {
                GeometryBase g = GH_Convert.ToGeometryBase(x);
                
                if (g != null)
                {
                    switch (g.ObjectType)
                    {
                        case ObjectType.Brep:
                            f.Objects.AddBrep(g as Brep, att);
                            break;
                        case ObjectType.Curve:
                            f.Objects.AddCurve(g as Curve, att);
                            break;
                        case ObjectType.Point:
                            f.Objects.AddPoint((g as Rhino.Geometry.Point).Location, att);
                            break;
                        case ObjectType.Surface:
                            f.Objects.AddSurface(g as Surface, att);
                            break;
                        case ObjectType.Mesh:
                            f.Objects.AddMesh(g as Mesh, att);
                            break;
                        case ObjectType.PointSet:
                            f.Objects.AddPointCloud(g as PointCloud, att); //This is a speculative entry
                            break;
                        default:
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "不能识别的物体: " + G.GetType().FullName);
                            break;
                    }
                }
            });

            f.Write(filePath, 5);
            f.Dispose();
        }

        public static ObjectAttributes getObjAttr(string L, File3dm doc, Color c)
        {
            //储存物件的信息
            ObjectAttributes att = new ObjectAttributes();
            //设置图层信息
            if (!string.IsNullOrEmpty(L) && Layer.IsValidName(L))
            {
                int layerIndex = 0;
                if (L.Contains("::"))
                {//包含子图层信息
                    L = L.Replace("::", "-");
                    string[] xArr = L.Split('-');
                    Layer parent = new Layer();
                    parent.Name = xArr[0];
                    doc.AllLayers.Add(parent);

                    Layer child = new Layer();
                    child.Name = xArr[1];
                    child.Color = c;
                    child.ParentLayerId = parent.Id;
                    doc.AllLayers.Add(child);

                    layerIndex = child.Index;
                }
                else
                {//不包含子图层信息
                    Layer parent = new Layer();
                    parent.Name = L;
                    parent.Color = c;
                    doc.AllLayers.Add(parent);
                    layerIndex = parent.Index;
                }
                att.LayerIndex = layerIndex;                
                return att;
            }
            else return null;
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

                        Params.RegisterInputParam(new Param_GenericObject());
                        VariableParameterMaintenance();
                        Params.OnParametersChanged();
                        ExpireSolution(recompute: true);
                    }
                }, true, m_mode == ExportMode.Rhino);
            toolStripMenuItem.ToolTipText = 
                "将O端输入的内容写入3DM文件" + Environment.NewLine + "请确保输入的内容为可写入3DM文件的类型";
            ToolStripMenuItem toolStripMenuItem2 = Menu_AppendItem(
            menu, "DWG", (object sender, EventArgs e) =>
            {
                if (m_mode != ExportMode.Text)
                {
                    RecordUndoEvent("Text");
                    m_mode = ExportMode.Text;

                    Params.UnregisterInputParameter(Params.Input[Params.Input.Count - 1]);
                    ExpireSolution(recompute: true);
                }
            }, true, m_mode == ExportMode.Text);
                    toolStripMenuItem2.ToolTipText =
                        "将O端输入的内容写入DWG文件" + Environment.NewLine + "请确保输入的内容为可写入DWG文件的类型";
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return new Param_GenericObject
            {
                NickName = string.Empty
            };
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
            if (Params.Input.Count == 3)
            {
                IGH_Param param = Params.Input[2];
                param.Name = "属性";
                param.NickName = "A";
                param.Description = "储存到Rhino文件的图层信息等等";
                param.Access = GH_ParamAccess.item;
                param.Optional = true;
            }
        }
    }
}
