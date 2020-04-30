namespace DXFLib
{
    public class DXFBlockRecord : DXFRecord
    {
        public string BlockName { get; set; }

        public override string ToString()
        {
            return BlockName;
        }
    }

    class DXFBlockRecordParser : DXFRecordParser
    {
        private DXFBlockRecord _currentRecord;

        protected override DXFRecord currentRecord
        {
            get { return _currentRecord; }
        }

        protected override void createRecord(DXFDocument doc)
        {
            _currentRecord = new DXFBlockRecord();
            doc.Tables.Blocks.Add(_currentRecord);
        }

        public override void ParseGroupCode(DXFDocument doc, int groupcode, string value)
        {
            base.ParseGroupCode(doc, groupcode, value);
            if (groupcode == 2)
            {
                _currentRecord.BlockName = value;
            }
        }
    }

}
