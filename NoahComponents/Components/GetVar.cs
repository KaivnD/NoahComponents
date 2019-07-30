using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Runtime;

namespace Noah.Components
{
    public class GetVar : GH_Param<IGH_Goo>
    {
        public GetVar() : base(new GH_InstanceDescription("Get", string.Empty, "获取数据", "Noah", "Utils"))
        {
            base.ObjectChanged += ObjectChangedHandler;
        }
        public override Guid ComponentGuid => new Guid("DD985C7A-B088-4AC4-8DC2-E08545206E5B");
        public override void CreateAttributes()
        {
            m_attributes = new GetVarAttr(this);
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

        protected override void CollectVolatileData_Custom()
        {
            m_data.Clear();
            string k = NickName;
            var script = PythonScript.Create();
            script.ExecuteScript("import scriptcontext as sc\nif sc.sticky.has_key('" + k + "'):\t\t\t\tV = sc.sticky['" + k + "']\nelse : V = 0");
            object value = script.GetVariable("V");
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
                m_data.Append((IGH_Goo)value);
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;
    }
}
