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

namespace Noah.Components
{
    public class CreateLayer : GH_Component
    {
        public CreateLayer ()
            :base("创建图层", "CreateLayer", "", "Noah", "Utils")
        {

        }
        public override Guid ComponentGuid => new Guid("8E5427FE-BE49-4B42-91AE-BD96748A8817");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("图层名称", "L", "与Exporter搭配，指定输出物件的图层名称", GH_ParamAccess.item, "默认值");
            pManager.AddColourParameter("图层颜色", "C", "与Exporter搭配，指定输出物件的图层颜色", GH_ParamAccess.item, Color.Black);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("图层信息", "I", "将此图层信息输出至Exporter A端", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            Color color = Color.Black;
            DA.GetData(0, ref name);
            DA.GetData(1, ref color);

            LayerInfo layer = new LayerInfo(name, color);

            DA.SetData(0, layer);
        }
    }
}
