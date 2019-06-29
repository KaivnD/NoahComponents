using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime;

namespace Noah.Components
{
    public class Cooker : GH_Component
    {
        public Cooker()
          : base("Cooker", "Cooker",
              "烧菜",
              "Noah", "Utils")
        {
        }
        protected override Bitmap Icon => Properties.Resources.cook;
        private RhinoDoc rhinoDoc = RhinoDoc.ActiveDoc;

        public override Guid ComponentGuid => new Guid("79EF4718-2B5A-4BFF-AB97-76A036598DB9");
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("GH物件", "G", "需要Cook的GH物件", GH_ParamAccess.item);
            pManager.AddTextParameter("图层名字", "L", "Rhino文档相应图层，可以不事先创建，可为某个图层的子图层（父图层::子图层）", GH_ParamAccess.item);
            pManager.AddCurveParameter("填充曲线", "H", "需要填充的曲线列表", GH_ParamAccess.list);
            pManager.AddColourParameter("图层颜色", "C", "该图层的颜色", GH_ParamAccess.item, Color.Black);
            pManager[0].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GeometryBase G = null;
            string L = "预设值";
            List<Curve> H = new List<Curve>();
            Color C = Color.Black;
            DA.GetData(0, ref G);
            DA.GetData(1, ref L);
            DA.GetDataList(2, H);
            DA.GetData(3, ref C);
            int NOAH_BAKE_INFO = 0;
            try
            {
                PythonScript script = PythonScript.Create();
                script.ExecuteScript("import scriptcontext as sc\nV=sc.sticky['NOAH_BAKE_INFO']");
                NOAH_BAKE_INFO = (int) script.GetVariable("V");
            } catch
            {
                NOAH_BAKE_INFO = 0;
            }
            
            if (NOAH_BAKE_INFO == 1)
            {
                if (G != null && H.Count == 0)
                {
                    //写入物件
                    ObjectAttributes att = getObjAttr(L, rhinoDoc, C);
                    switch (G.ObjectType)
                    {
                        case ObjectType.Brep:
                            rhinoDoc.Objects.AddBrep(G as Brep, att);
                            break;
                        case ObjectType.Curve:
                            rhinoDoc.Objects.AddCurve(G as Curve, att);
                            break;
                        case ObjectType.Point:
                            rhinoDoc.Objects.AddPoint((G as Rhino.Geometry.Point).Location, att);
                            break;
                        case ObjectType.Surface:
                            rhinoDoc.Objects.AddSurface(G as Surface, att);
                            break;
                        case ObjectType.Mesh:
                            rhinoDoc.Objects.AddMesh(G as Mesh, att);
                            break;
                        case ObjectType.PointSet:
                            rhinoDoc.Objects.AddPointCloud(G as Rhino.Geometry.PointCloud, att); //This is a speculative entry
                            break;
                        default:
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "不能识别的物体: " + G.GetType().FullName);
                            break;
                    }
                }
                else if (G == null && H.Count > 0)
                {
                    ObjectAttributes att = getObjAttr(L, rhinoDoc, C);
                    Hatch[] hatches = Hatch.Create(H, 0, 0, 1, 0);
                    foreach (Hatch hatch in hatches)
                    {
                        rhinoDoc.Objects.AddHatch(hatch, att);
                    }
                }
                else AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "G和H只能输入一个");
            }
            else AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "等待写入");
        }
        public static ObjectAttributes getObjAttr(string L, RhinoDoc doc, Color c)
        {
            //储存物件的信息
            ObjectAttributes att = new ObjectAttributes();
            //设置图层信息
            if (!string.IsNullOrEmpty(L) && Layer.IsValidName(L))
            {
                int layerIndex = doc.Layers.FindByFullPath(L, -1);
                if (layerIndex < 0)
                {//如果这个图层没有，则创建
                    if (L.Contains("::"))
                    {//包含子图层信息
                        L = L.Replace("::", "-");
                        string[] xArr = L.Split('-');
                        int parentLayerIx = doc.Layers.FindByFullPath(xArr[0], -1);

                        if (parentLayerIx < 0)
                        {//父图层不存在，则创建
                            parentLayerIx = doc.Layers.Add(xArr[0], System.Drawing.Color.Black);
                            if (parentLayerIx < 0)
                            {//父图层创建成功
                                return null;
                            }
                        }

                        Layer chirldLayer = new Layer();
                        chirldLayer.Name = xArr[1];
                        chirldLayer.Color = c;
                        chirldLayer.ParentLayerId = doc.Layers[parentLayerIx].Id;
                        int chirldLayerIndex = doc.Layers.Add(chirldLayer);

                        if (chirldLayerIndex > 0)
                        {
                            att.LayerIndex = chirldLayerIndex;
                        }
                        else return null;

                    }
                    else
                    {//不包含子图层信息
                        int newLayerIndex = doc.Layers.Add(L, System.Drawing.Color.Black);
                        if (newLayerIndex > 0)
                        {
                            att.LayerIndex = newLayerIndex;
                        }
                        else return null;
                    }
                }
                else
                {
                    att.LayerIndex = layerIndex;
                }
                return att;
            }
            else return null;
        }
    }
}
