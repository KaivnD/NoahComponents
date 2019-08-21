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
using GH_IO.Serialization;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Noah.Components
{
    public class Exporter : GH_Component
    {
        private enum ExportMode
        {
            None = 0,
            Rhino = 1,
            Text = 2,
            Data = 3,
            CSV = 4
        }
        private string NOAH_PROJECT { get; set; }
        private string TASK_TICKET { get; set; }
        private string UUID { get; set; }
        private int NOAH_GENERATOR = 0;
        private JObject ProjectInfo = null;
        private bool exported = false;

        protected override Bitmap Icon => Properties.Resources.setvar;
        private ExportMode m_mode;
        public Exporter()
            : base("Exporter", "Exporter", "出口", "Noah", "Utils")
        {
            m_mode = ExportMode.Rhino;
            UpdateMessage();
            SolutionExpired += SolutionExpiredHandler;
            ObjectChanged += ObjectChangedHandler;
        }

        private void SolutionExpiredHandler(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            exported = false;
        }

        public override Guid ComponentGuid => new Guid("03067262-63C4-4B7E-A742-2712EA89B5CC");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("内容", "E", "需要出口的内容", GH_ParamAccess.list);
            pManager.AddIntegerParameter("序号", "X", "对应Noah包输出的序号，从0起", GH_ParamAccess.item, 0);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        private void ObjectChangedHandler(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            exported = false;
            ExpireSolution(true);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            UpdateMessage();
            PythonScript script = PythonScript.Create();
            string outDir = "";
            try
            {
                script.ExecuteScript("import scriptcontext as sc\nV=sc.sticky['NOAH_PROJECT']\nT=sc.sticky['TASK_TICKET']\nG=int(sc.sticky['NOAH_GENERATOR'])\nID=sc.sticky['UUID']");
                NOAH_PROJECT = (string)script.GetVariable("V");
                TASK_TICKET = (string)script.GetVariable("T");
                NOAH_GENERATOR = (int)script.GetVariable("G");
                UUID = (string)script.GetVariable("ID");
                if (File.Exists(NOAH_PROJECT))
                {
                    outDir = Path.Combine(Path.GetDirectoryName(NOAH_PROJECT), ".noah", "tasks", UUID, TASK_TICKET, "out");
                    ProjectInfo = JObject.Parse(File.ReadAllText(NOAH_PROJECT));
                }
            }
            catch
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "出口功能需要从客户端启动才能运行");
            }
            int outIndex = 0;
            DA.GetData(1, ref outIndex);

            JArray output = JArray.Parse(ProjectInfo["generators"][NOAH_GENERATOR]["output"].ToString());
            if (outIndex >= output.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "定义时未指定此输出端口");
            }
            switch (m_mode)
            {
                case ExportMode.None:
                    Message = null;
                    break;
                case ExportMode.Rhino:
                    string fileName = Convert.ToString(outIndex) + ".3dm";
                    string filePath = Path.Combine(outDir, fileName);

                    File3dmWriter writer = new File3dmWriter(filePath);
                    List<int> ll = new List<int>();
                    List<ObjectLayerInfo> layeredObj = new List<ObjectLayerInfo>();
                    DA.GetDataList(0, layeredObj);
                    layeredObj.ForEach(x =>
                    {
                        writer.ChildLayerSolution(x.Name);
                        ll.Add(writer.CreateLayer(x.Name, x.Color));
                    });
                    if (layeredObj.Count > 0)
                    {
                        writer.Write(layeredObj, ll);
                        if (!exported)
                        {
                            ProjectInfo["generators"][NOAH_GENERATOR]["output"][outIndex]["value"] = filePath;
                            File.WriteAllText(NOAH_PROJECT, JsonConvert.SerializeObject(ProjectInfo, Formatting.Indented));
                            exported = true;
                        }
                    }

                    break;
                case ExportMode.Text:
                    if (!exported)
                    {
                        string outputData = "";
                        DA.GetData(0, ref outputData);
                        ProjectInfo["generators"][NOAH_GENERATOR]["output"][outIndex]["value"] = outputData;
                        File.WriteAllText(NOAH_PROJECT, JsonConvert.SerializeObject(ProjectInfo, Formatting.Indented));
                        exported = true;
                    }
                    break;
                case ExportMode.Data:
                    fileName = Convert.ToString(outIndex) + ".noahdata";
                    filePath = Path.Combine(outDir, fileName);
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "未指定文件.");
                        return;
                    }
                    
                    GH_LooseChunk val = new GH_LooseChunk("Grasshopper Data");
                    val.SetGuid("OriginId", base.InstanceGuid);
                    val.SetInt32("Count", base.Params.Input.Count);
                    IGH_Param iGH_Param = base.Params.Input[0];
                    IGH_Structure volatileData = iGH_Param.VolatileData;
                    GH_IWriter val2 = val.CreateChunk("Block", 0);
                    val2.SetString("Name", iGH_Param.NickName);
                    val2.SetBoolean("Empty", volatileData.IsEmpty);
                    if (!volatileData.IsEmpty)
                    {
                        GH_Structure<IGH_Goo> tree = null;
                        DA.GetDataTree(0, out tree);
                        if (!tree.Write(val2.CreateChunk("Data")))
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"There was a problem writing the {iGH_Param.NickName} data.");
                        }
                    }
                    byte[] bytes = val.Serialize_Binary();
                    try
                    {
                        File.WriteAllBytes(filePath, bytes);
                    }
                    catch (Exception ex)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                    }
                    if (!exported)
                    {
                        ProjectInfo["generators"][NOAH_GENERATOR]["output"][outIndex]["value"] = filePath;
                        File.WriteAllText(NOAH_PROJECT, JsonConvert.SerializeObject(ProjectInfo, Formatting.Indented));
                        exported = true;
                    }
                    break;
                case ExportMode.CSV:
                    fileName = Convert.ToString(outIndex) + ".csv";
                    filePath = Path.Combine(outDir, fileName);
                    List<object> oList = new List<object>();
                    List<string> sList = new List<string>();
                    DA.GetDataList(0, oList);
                    oList.ForEach(el =>
                    {
                        string tmp = "";
                        GH_Convert.ToString(el, out tmp, GH_Conversion.Both);
                        sList.Add(tmp);
                    });
                    File.WriteAllText(filePath, string.Join(Environment.NewLine, sList));
                    if (!exported)
                    {
                        ProjectInfo["generators"][NOAH_GENERATOR]["output"][outIndex]["value"] = filePath;
                        File.WriteAllText(NOAH_PROJECT, JsonConvert.SerializeObject(ProjectInfo, Formatting.Indented));
                        exported = true;
                    }
                    break;
            }
        }

        private void writeRhino3dm (File3dm f, string filePath, List<ObjectLayerInfo> G, List<int> att)
        {
            for (int i = 0; i < G.Count; i ++)
            {
                GeometryBase g = GH_Convert.ToGeometryBase(G[i].Geometry);
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

        private static int getLayerIndex (ObjectLayerInfo li, File3dm f)
        {
            File3dmLayerTable layers =  f.AllLayers;
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
                        UpdateMessage();

                        ExpireSolution(recompute: true);
                    }
                }, true, m_mode == ExportMode.Rhino);
            toolStripMenuItem.ToolTipText = 
                "将E端输入的内容写入3DM文件" + Environment.NewLine + "请确保输入的内容为可写入3DM文件的类型";

            ToolStripMenuItem toolStripMenuItem2 = Menu_AppendItem(
            menu, "Text", (object sender, EventArgs e) =>
            {
                if (m_mode != ExportMode.Text)
                {
                    RecordUndoEvent("Text");
                    m_mode = ExportMode.Text;
                    UpdateMessage();
                    ExpireSolution(recompute: true);
                }
            }, true, m_mode == ExportMode.Text);
            toolStripMenuItem2.ToolTipText =
                "将E端输入的内容写到NOAH包输出端" + Environment.NewLine + "请确保输入的内容为可写入文本";

            ToolStripMenuItem toolStripMenuItem3 = Menu_AppendItem(
            menu, "Data", (object sender, EventArgs e) =>
            {
                if (m_mode != ExportMode.Data)
                {
                    RecordUndoEvent("Data");
                    m_mode = ExportMode.Data;
                    UpdateMessage();
                    ExpireSolution(recompute: true);
                }
            }, true, m_mode == ExportMode.Data);
            toolStripMenuItem3.ToolTipText =
                "将E端输入的内容写到NOAH包输出端" + Environment.NewLine + "数据结构将在导入数据的时候被还原";

            ToolStripMenuItem toolStripMenuItem4 = Menu_AppendItem(
                menu, "CSV", (object sender, EventArgs e) =>
                {
                    if (m_mode != ExportMode.CSV)
                    {
                        RecordUndoEvent("CSV");
                        m_mode = ExportMode.CSV;
                        UpdateMessage();
                        ExpireSolution(recompute: true);
                    }
                }, true, m_mode == ExportMode.CSV);
            toolStripMenuItem4.ToolTipText =
                "将E端输入的内容写到NOAH包输出端" + Environment.NewLine + "数据将被保存为CSV格式";
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ExportMode", (int)m_mode);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            m_mode = (ExportMode)reader.GetInt32("ExportMode");
            UpdateMessage();
            return base.Read(reader);
        }

        private void UpdateMessage()
        {
            switch (m_mode)
            {
                case ExportMode.Data:
                    Message = "Data";
                    Params.Input[0].Access = GH_ParamAccess.tree;
                    Params.Input[0].DataMapping = GH_DataMapping.None;
                    break;
                case ExportMode.Rhino:
                    Message = "Rhino";
                    Params.Input[0].Access = GH_ParamAccess.list;
                    Params.Input[0].DataMapping = GH_DataMapping.Flatten;
                    break;
                case ExportMode.Text:
                    Message = "Text";
                    Params.Input[0].Access = GH_ParamAccess.item;
                    Params.Input[0].DataMapping = GH_DataMapping.None;
                    break;
                case ExportMode.CSV:
                    Message = "CSV";
                    Params.Input[0].Access = GH_ParamAccess.list;
                    Params.Input[0].DataMapping = GH_DataMapping.Flatten;
                    break;
            }
        }
    }
}
