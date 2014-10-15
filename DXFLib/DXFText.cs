using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DXFLib
{
    [Entity("TEXT")]
    public class DXFText : DXFGenericEntity
    {
        public string Text { get; set; }

        public override void ParseGroupCode(int groupcode, string value)
        {
            base.ParseGroupCode(groupcode, value);
            switch (groupcode)
            {
                case 1:
                    Text = value;
                    break;
                default:
                    base.ParseGroupCode(groupcode, value);
                    break;
            }
        }
    }
}
