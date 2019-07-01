using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Runtime;

namespace Noah.Components
{
    public class Importer : GH_Param<IGH_Goo>
    {
        public Importer() : base(new GH_InstanceDescription("Importer", string.Empty, "进口数据", "Noah", "Utils"))
        {
            base.ObjectChanged += ObjectChangedHandler;
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

        protected override void OnVolatileDataCollected()
        {
            if (SourceCount == 1)
            {
                //m_data.Append(new GH_ObjectWrapper(Sources[0].VolatileData.get_Branch(0)[0]));
            }
            else
            {
                m_data.Clear();
                m_data.Append(new GH_ObjectWrapper(NickName));
            }
        }

        /// <summary>
        /// 保存文件内容
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">需写入的内容</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        public static bool SaveTextFile(string path, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding(936));
                    sw.Write(content);
                    sw.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        protected override void ValuesChanged()
        {
            base.ValuesChanged();
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
    }
}
