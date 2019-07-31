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
            :base("指定图层", "CreateLayer", "为物件指定图层以出口", "Noah", "Utils")
        {

        }

        protected override Bitmap Icon => Properties.Resources.layers;

        public override Guid ComponentGuid => new Guid("8E5427FE-BE49-4B42-91AE-BD96748A8817");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("物件", "G", "与Exporter搭配，指定输出物件", GH_ParamAccess.item);
            pManager.AddTextParameter("图层名称", "L", "与Exporter搭配，指定输出物件的图层名称", GH_ParamAccess.item, "默认值");
            pManager.AddColourParameter("图层颜色", "C", "与Exporter搭配，指定输出物件的图层颜色", GH_ParamAccess.item, Color.Black);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("图层信息", "I", "将此图层信息输出至Exporter A端", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object geometry = null;
            string name = "";
            Color color = Color.Black;
            DA.GetData(0, ref geometry);
            DA.GetData(1, ref name);
            DA.GetData(2, ref color);

            ObjectLayerInfo layer = new ObjectLayerInfo(geometry, name, color);

            DA.SetData(0, layer);
        }
    }
}
