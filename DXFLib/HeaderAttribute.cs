using System;

namespace DXFLib
{
    [AttributeUsage(AttributeTargets.Property)]
    class HeaderAttribute : Attribute
    {
        public string Name;

        public HeaderAttribute(string varname)
        {
            this.Name = varname;
        }
    }
}
