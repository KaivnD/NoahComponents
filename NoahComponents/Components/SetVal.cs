using Grasshopper.Kernel;
using Noah.Properties;
using Rhino.Runtime;
using System;
using System.Drawing;

namespace Noah.Components
{
	public class SetVal : GH_Component
	{
		protected override Bitmap Icon => Resources.setvar;

		public override Guid ComponentGuid => new Guid("B686D7FA-8503-43A4-96D7-22832CA4B4CE");

		public SetVal()
			: base("Noah", "Set", "设置一个全局变量", "Noah", "Utils")
		{
		}

		protected override void RegisterInputParams(GH_InputParamManager pManager)
		{
			pManager.AddTextParameter("键", "K", "作为之后提取变量的依据", 0);
			pManager.AddGenericParameter("值", "V", "作为之后提取变量的依据", 0);
		}

		protected override void RegisterOutputParams(GH_OutputParamManager pManager)
		{
		}

		protected override void SolveInstance(IGH_DataAccess DA)
		{
			string str = "";
			object obj = null;
			DA.GetData<string>(0, ref str);
			DA.GetData<object>(1, ref obj);
			PythonScript val = PythonScript.Create();
			val.SetVariable("V", obj);
			val.ExecuteScript("import scriptcontext as sc\nsc.sticky['" + str + "'] = V");
		}
	}
}
