using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Runtime;
using Newtonsoft.Json.Linq;
using System.Drawing;
using GH_IO.Serialization;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;

namespace Noah.Components
{
    public class Importer : GH_Component
    {
        public Importer() : base("Importer", string.Empty, "进口数据", "Noah", "Utils")
        {
            base.ObjectChanged += ObjectChangedHandler;
        }

        protected override Bitmap Icon => Properties.Resources.getvar;
        public override Guid ComponentGuid => new Guid("C7960BD2-930A-4E27-B197-B09C5DD6CD2D");
        /// <summary>
        /// Key发生变化时，重新获取数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObjectChangedHandler(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            ExpireSolution(true);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            Param_FilePath param_FilePath = new Param_FilePath();
            param_FilePath.FileFilter = "Noah Data (*.noahdata)|*.noahdata";
            param_FilePath.ExpireOnFileEvent = true;
            pManager.AddParameter(param_FilePath, "Path", "P", "Data位置", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {            
            DA.DisableGapLogic();
            string SourceFile = "";
            DA.GetData(0, ref SourceFile);
            if (string.IsNullOrWhiteSpace(SourceFile))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No source file has been specified.");
                return;
            }
            if (!File.Exists(SourceFile))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Source file location doesn't exist: " + SourceFile);
                return;
            }
            byte[] array;
            try
            {
                array = File.ReadAllBytes(SourceFile);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                return;
            }
            GH_LooseChunk val = new GH_LooseChunk("Grasshopper Data");
            val.Deserialize_Binary(array);
            if (val.ItemCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Source data file is corrupt.");
                return;
            }

            GH_Structure<IGH_Goo> gH_Structure = new GH_Structure<IGH_Goo>();
            GH_IReader val2 = val.FindChunk("Block", 0);
            if (val2 == null)
            {
                base.Params.Output[0].NickName = "?";
                DA.SetDataTree(0, gH_Structure);
            }
            bool boolean = val2.GetBoolean("Empty");

            if (!boolean)
            {
                GH_IReader val3 = val2.FindChunk("Data");
                if (val3 == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Source file is corrupt.");
                }
                else if (!gH_Structure.Read(val3))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Data could not be deserialized.");
                }
            }
            DA.SetDataTree(0, gH_Structure);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
    }
}
