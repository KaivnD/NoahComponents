using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Runtime;
using Newtonsoft.Json;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;
using Newtonsoft.Json.Linq;

namespace Noah.Components
{
    public class Importer : GH_Param<IGH_Goo>
    {
        private string NOAH_PROJECT = "";
        private int NOAH_GENERATOR = 0;
        private JObject ProjectInfo = null;
        public Importer() : base(new GH_InstanceDescription("Importer", string.Empty, "进口数据", "Noah", "Utils"))
        {
            base.ObjectChanged += ObjectChangedHandler;
        }

        private void ProjectFileChanged(string filename)
        {
            ProjectInfo = JObject.Parse(File.ReadAllText(NOAH_PROJECT));
            ExpireSolution(true);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.getvar;
        public override Guid ComponentGuid => new Guid("C7960BD2-930A-4E27-B197-B09C5DD6CD2D");
        public override void CreateAttributes()
        {
            m_attributes = new ImporterAttr(this);
        }

        protected override IGH_Goo InstantiateT()
        {
            return new GH_ObjectWrapper(null);
        }
        /// <summary>
        /// Key发生变化时，重新获取数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObjectChangedHandler(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            ExpireSolution(true);
        }

        public override void AddedToDocument(GH_Document document)
        {
            PythonScript script = PythonScript.Create();
            try
            {
                script.ExecuteScript("import scriptcontext as sc\nV=sc.sticky['NOAH_PROJECT']\nN=sc.sticky['NOAH_GENERATOR']");
                NOAH_PROJECT = (string)script.GetVariable("V");
                NOAH_GENERATOR = (int)script.GetVariable("N");
                if (File.Exists(NOAH_PROJECT))
                {
                    GH_FileWatcher.CreateFileWatcher(NOAH_PROJECT, GH_FileWatcherEvents.All, new GH_FileWatcher.FileChangedSimple(ProjectFileChanged));
                    ProjectInfo = JObject.Parse(File.ReadAllText(NOAH_PROJECT));
                }
            }
            catch
            {

            }
        }

        protected override void OnVolatileDataCollected()
        {            
            m_data.Clear();
            if (ProjectInfo != null)
            {
                JArray generators = JArray.Parse(ProjectInfo["generators"].ToString());
                var generator = generators[NOAH_GENERATOR];
                string value = null;
                JArray inputs = (JArray)generator["input"];
                for (int i = 0; i < inputs.Count; i++)
                {
                    if (inputs[i]["name"].ToString() == NickName)
                    {
                        value = inputs[i]["value"].ToString();
                        break;
                    }
                }

                GH_Number castNumber = null;
                GH_String castString = null;
                if (GH_Convert.ToGHNumber(value, GH_Conversion.Both, ref castNumber))
                {
                    m_data.Append(new GH_ObjectWrapper(castNumber));
                }
                else if (GH_Convert.ToGHString(value, GH_Conversion.Both, ref castString))
                {
                    m_data.Append(new GH_ObjectWrapper(castString));
                }
                else
                {
                    m_data.Append(null);
                }
                if (SourceCount == 1)
                {
                    int inputIdx = -1;
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        if (inputs[i]["name"].ToString() == NickName)
                        {
                            inputIdx = i;
                            break;
                        }
                    }
                    if (inputIdx >= 0)
                    {
                        ProjectInfo["generators"][NOAH_GENERATOR]["input"][inputIdx]["value"] = Sources[0].VolatileData.get_Branch(0)[0].ToString();
                        File.WriteAllText(NOAH_PROJECT, JsonConvert.SerializeObject(ProjectInfo, Formatting.Indented));
                    }
                    Sources[0].NickName = NickName;
                }
            } else m_data.Append(null);
        }

        protected override void ValuesChanged()
        {
            base.ValuesChanged();
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
    }
}
