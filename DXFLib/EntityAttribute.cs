using System;

namespace DXFLib
{
    [AttributeUsage(AttributeTargets.Class)]
    class EntityAttribute : Attribute
    {
        public string EntityName;
        public EntityAttribute(string Name)
        {
            this.EntityName = Name;
        }
    }
}
