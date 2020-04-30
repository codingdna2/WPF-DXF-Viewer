using System;

namespace DXFLib
{
    [AttributeUsage(AttributeTargets.Property)]
    class TableAttribute : Attribute
    {
        public string TableName;

        public Type TableParser;

        public TableAttribute(string name, Type parser)
        {
            this.TableName = name;
            this.TableParser = parser;
        }
    }
}
