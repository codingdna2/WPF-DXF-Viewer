using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DXFLib
{
    public class DXFPoint
    {
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Z { get; set; }

        public override string ToString()
        {
            return string.Format("X:{0} Y:{1} Z:{2}", X, Y, Z);
        }
    }
}
