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
using Rhino.Runtime;
using Noah.Utils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
        private GH_Document ghDoc { get; set; }

        protected override Bitmap Icon => Properties.Resources.setvar;
        private ExportMode m_mode;
        public Exporter()
            : base("Exporter", "Exporter", "出口", "Noah", "Utils")
        {
            m_mode = ExportMode.Rhino;
            ghDoc = OnPingDocument();
        }

        public override Guid ComponentGuid => new Guid("03067262-63C4-4B7E-A742-2712EA89B5CC");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("物件", "E", "需要出口的内容", GH_ParamAccess.list);
            pManager.AddIntegerParameter("序号", "X", "对应Noah包输出的序号，从0起", GH_ParamAccess.item, 0);
            pManager.AddGenericParameter("属性", "A", "对应的图层属性", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        private string NOAH_PROJECT { get; set; }
        private string TASK_TICKET { get; set; }
        private int NOAH_GENERATOR = 0;
        private JObject ProjectInfo = null;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            PythonScript script = PythonScript.Create();
            string outDir = "";
            try
            {
                script.ExecuteScript("import scriptcontext as sc\nV=sc.sticky['NOAH_PROJECT']\nT=sc.sticky['TASK_TICKET']\nG=int(sc.sticky['NOAH_GENERATOR'])");
                NOAH_PROJECT = (string)script.GetVariable("V");
                TASK_TICKET = (string)script.GetVariable("T");
                NOAH_GENERATOR = (int)script.GetVariable("G");
                if (File.Exists(NOAH_PROJECT))
                {
                    outDir = Path.Combine(Path.GetDirectoryName(NOAH_PROJECT), ".noah", "tasks", TASK_TICKET, "out");
                    ProjectInfo = JObject.Parse(File.ReadAllText(NOAH_PROJECT));
                }
            }
            catch
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "出口功能需要从客户端启动才能运行");
            }
            switch (m_mode)
            {
                case ExportMode.None:
                    Message = null;
                    break;
                case ExportMode.Rhino:
                    Message = "Rhino";
                    int outIndex = 0;
                    DA.GetData(1, ref outIndex);
                    string fileName = Convert.ToString(outIndex) + ".3dm";
                    string filePath = Path.Combine(outDir, fileName);
                    List<LayerInfo> layers = new List<LayerInfo>();
                    List<object> geo = new List<object>();
                    DA.GetDataList(0, geo);
                    DA.GetDataList(2, layers);
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

                    if (layers.Count != geo.Count)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "E, A两个列表长度不等");
                        return;
                    } else
                    {
                        List<int> ll = new List<int>();
                        layers.ForEach(x =>
                        {
                            if (getLayerIndex(x, f) < 0)
                            {
                                Layer l = new Layer();
                                l.Name = x.Name;
                                l.Color = x.Color;
                                f.AllLayers.Add(l);
                            }
                            ll.Add(getLayerIndex(x, f));
                        });

                        if (geo != null)
                        {
                            writeRhino3dm(f, filePath, geo, ll);
                            ProjectInfo["generators"][NOAH_GENERATOR]["output"][outIndex]["value"] = filePath;
                            File.WriteAllText(NOAH_PROJECT, JsonConvert.SerializeObject(ProjectInfo, Formatting.Indented));
                        }
                    }

                    break;
                case ExportMode.Text:
                    Message = "Text";
                    break;
            }
        }

        private void writeRhino3dm (File3dm f, string filePath, List<object> G, List<int> att)
        {
            for (int i = 0; i < G.Count; i ++)
            {
                GeometryBase g = GH_Convert.ToGeometryBase(G[i]);
                ObjectAttributes attr = new ObjectAttributes();
                attr.LayerIndex = att[i];

                if (g != null)
                {
                    switch (g.ObjectType)
                    {
                        case ObjectType.Brep:
                            f.Objects.AddBrep(g as Brep, attr);
                            break;
                        case ObjectType.Curve:
                            f.Objects.AddCurve(g as Curve, attr);
                            break;
                        case ObjectType.Point:
                            f.Objects.AddPoint((g as Rhino.Geometry.Point).Location, attr);
                            break;
                        case ObjectType.Surface:
                            f.Objects.AddSurface(g as Surface, attr);
                            break;
                        case ObjectType.Mesh:
                            f.Objects.AddMesh(g as Mesh, attr);
                            break;
                        case ObjectType.PointSet:
                            f.Objects.AddPointCloud(g as PointCloud, attr); //This is a speculative entry
                            break;
                        default:
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "不能识别的物体: " + G.GetType().FullName);
                            break;
                    }
                }
            }

            f.Write(filePath, 5);
            f.Dispose();
        }

        private static int getLayerIndex (LayerInfo li, File3dm f)
        {
            if (li.Name.Contains(Layer.PathSeparator))
            {
                foreach (Layer l in f.AllLayers)
                {
                    if (l.FullPath == li.Name)
                    {
                        return l.Index;
                    }
                }
            } else
            {
                foreach (Layer l in f.AllLayers)
                {
                    if (l.Name == li.Name)
                    {
                        return l.Index;
                    }
                }
            }

            return -1;
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
                param.Description = "对应的图层属性";
                param.Access = GH_ParamAccess.list;
                param.Optional = true;
            }
        }
    }
}
