using System;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Runtime;

namespace Noah.Components
{
    public class Lighter : GH_Component
    {
        public Lighter()
          : base("Lighter", "Lighter",
              "点火",
              "Noah", "Utils")
        {
        }
        public GH_Document ghDoc => Instances.ActiveCanvas.Document;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.lighter;

        public override Guid ComponentGuid => new Guid("CCB4B630-ED2A-44D7-9C46-C997F209181F");
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("开关", "S", "设置为True即开始Cook", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool S = false;
            DA.GetData(0, ref S);
            PythonScript script = PythonScript.Create();
            script.SetVariable("bakeornot", S ? 1 : 0);
            script.ExecuteScript("import scriptcontext as sc\nsc.sticky['NOAH_BAKE_INFO'] = bakeornot");
            foreach (IGH_DocumentObject obj in ghDoc.Objects)
            {
                if (obj is GH_Cluster)
                {
                    GH_Cluster objCluster = (GH_Cluster)obj;
                    GH_Document clusterDoc = objCluster.Document("");
                    foreach (IGH_DocumentObject clusterObj in clusterDoc.Objects)
                    {
                        if (clusterObj.ComponentGuid == new Guid("79EF4718-2B5A-4BFF-AB97-76A036598DB9"))
                        {
                            clusterObj.ExpireSolution(true);
                        }
                    }
                    obj.ExpireSolution(true);
                }
                else
                {
                    if (obj.ComponentGuid == new Guid("79EF4718-2B5A-4BFF-AB97-76A036598DB9"))
                    {
                        obj.ExpireSolution(true);
                    }
                }
            }
        }
    }
}
