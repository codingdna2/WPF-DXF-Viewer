namespace DXFLib
{
    //http://help.autodesk.com/view/ACD/2015/ENU/?guid=GUID-D94802B0-8BE8-4AC9-8054-17197688AFDB

    public class DXFLayerRecord : DXFRecord
    {
        public string LayerName { get; set; }

        public int Color { get; set; } //Color number (if negative, layer is off)

        public string LineType { get; set; }

        public int LineWeight { get; set; } // LineWeight => -3 = Standard, -2 = ByLayer, -1 = ByBlock.

        public override string ToString()
        {
            return string.Format("Name:{0} LineType:{1}", LayerName, LineType);
        }    
    }

    class DXFLayerRecordParser : DXFRecordParser
    {
        private DXFLayerRecord _record;
        protected override DXFRecord currentRecord
        {
            get { return _record; }
        }

        protected override void createRecord(DXFDocument doc)
        {
            _record = new DXFLayerRecord();
            doc.Tables.Layers.Add(_record);
        }

        public override void ParseGroupCode(DXFDocument doc, int groupcode, string value)
        {
            base.ParseGroupCode(doc, groupcode, value);
            switch (groupcode)
            {
                case 2:
                    _record.LayerName = value;
                    break;
                case 62:
                    _record.Color = int.Parse(value);
                    break;
                case 6:
                    _record.LineType = value;
                    break;
                case 370:
                    _record.LineWeight = int.Parse(value); 
                    break;
            }
        }
    }
}
