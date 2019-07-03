using Grasshopper.Kernel;
using Rhino.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah
{
    public class NoahComponentsLoader : GH_AssemblyPriority
    {
        public NoahComponentsLoader ()
        {
        }

        public override GH_LoadingInstruction PriorityLoad()
        {
            return GH_LoadingInstruction.Proceed;
        }
    }
}
