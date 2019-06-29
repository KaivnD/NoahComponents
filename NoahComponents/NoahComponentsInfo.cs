using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace NoahComponents
{
    public class NoahComponentsInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "NoahComponents";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("023d3600-80c1-43f9-9685-1d8fe5e03dad");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
