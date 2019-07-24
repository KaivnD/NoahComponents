using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Utils
{
    public class LayerInfo
    {
        public string Name { set; get; }
        public Color Color { set; get; }
        public LayerInfo (string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public override string ToString ()
        {
            return "Name: " + Name + "; Color:" + Color.ToString();
        }
    }
}
