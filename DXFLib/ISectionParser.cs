namespace DXFLib
{
    interface ISectionParser
    {
        void ParseGroupCode(DXFDocument doc, int groupcode, string value);
    }
}
