using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using Noah.Utils;
using System.Data;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using System.Drawing;

namespace Noah.Components
{
    public class ReadExcel : GH_Component
    {
        public ReadExcel()
            :base("ReadExcel", "RE", "读取EXCEL为数性数据", "Noah", "Utils")
        {
        }

        protected override Bitmap Icon => Properties.Resources.table;

        public override Guid ComponentGuid => new Guid("FC188698-D731-45F9-AC24-2771FE3797CA");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            Param_FilePath param_FilePath = new Param_FilePath();
            param_FilePath.FileFilter = "Excel files (*.xlsx)|*.xlsx|Excel files (*.xls)|*.xls";
            pManager.AddParameter(param_FilePath, "Excel文件", "E", "被转换的Excel文件路径", GH_ParamAccess.item);
            pManager.AddTextParameter("Sheet", "S", "工作表名称", GH_ParamAccess.item, "Sheet1");
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("表格数据", "D", "Excel表格中的数据{行，列}", GH_ParamAccess.tree);
            pManager.AddNumberParameter("表格行数", "R", "Excel表格中的数据的总行数", GH_ParamAccess.item);
            pManager.AddNumberParameter("表格列数", "C", "Excel表格中的数据的总列数", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double[] aa = new double[12];

            string path = string.Empty;
            string sheet = string.Empty;
            DA.GetData(0, ref path);
            DA.GetData(1, ref sheet);
            try
            {
                using (ExcelReader excelHelper = new ExcelReader(path))
                {
                    DataTable dt = excelHelper.ExcelToDataTable(sheet, false);
                    if (dt != null)
                    {
                        DataTree<IGH_Goo> tree = new DataTree<IGH_Goo>();
                        for (int i = 0; i < dt.Rows.Count; ++i)
                        {
                            for (int j = 0; j < dt.Columns.Count; ++j)
                            {
                                var value = dt.Rows[i][j];
                                GH_Number castNumber = null;
                                GH_String castString = null;
                                if (GH_Convert.ToGHNumber(value, GH_Conversion.Both, ref castNumber))
                                {
                                    tree.Add(new GH_ObjectWrapper(castNumber), new GH_Path(i, j));
                                }
                                else if (GH_Convert.ToGHString(value, GH_Conversion.Both, ref castString))
                                {
                                    tree.Add(new GH_ObjectWrapper(castString), new GH_Path(i, j));
                                }
                                else tree.Add(null, new GH_Path(i, j));
                            }
                        }
                        DA.SetDataTree(0, tree);
                        DA.SetData(1, dt.Rows.Count);
                        DA.SetData(2, dt.Columns.Count);
                    } else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("读取{0}失败",path));
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);            
            }         
        }
    }
}
