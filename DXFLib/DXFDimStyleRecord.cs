namespace DXFLib
{
    //https://ezdxf.readthedocs.io/en/stable/_images/dimvars1.svg
    public class DXFDimStyleRecord : DXFRecord
    {
        public string StyleName { get; set; }

        public int DimensionLineWeight { get; set; }

        public string DimensionLineType { get; set; }

        //TODO: Include more fields

        public override string ToString()
        {
            return StyleName;
        }
    }

    class DXFDimStyleRecordParser : DXFRecordParser
    {
        private DXFDimStyleRecord _record;
        protected override DXFRecord currentRecord
        {
            get { return _record; }
        }

        protected override void createRecord(DXFDocument doc)
        {
            _record = new DXFDimStyleRecord();
            doc.Tables.DimStyles.Add(_record);
        }

        public override void ParseGroupCode(DXFDocument doc, int groupcode, string value)
        {
            base.ParseGroupCode(doc, groupcode, value);
            switch (groupcode)
            {
                case 2:
                    _record.StyleName = value;
                    break;

                case 345:
                    _record.DimensionLineType = value;
                    break;

                case 371:
                    _record.DimensionLineWeight = int.Parse(value);
                    break;
            }
        }
    }

}
