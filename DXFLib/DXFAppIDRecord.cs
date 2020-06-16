﻿namespace DXFLib
{
    public class DXFAppIDRecord : DXFRecord
    {
        public string ApplicationName { get; set; }

        public override string ToString()
        {
            return ApplicationName;
        }
    }

    class DXFAppIDParser : DXFRecordParser
    {
        #region ISectionParser Member

        public override void ParseGroupCode(DXFDocument doc, int groupcode, string value)
        {
            base.ParseGroupCode(doc, groupcode, value);
            switch (groupcode)
            {
                case 2:
                    _currentRecord.ApplicationName = value;
                    break;
            }
        }

        #endregion

        DXFAppIDRecord _currentRecord;

        protected override DXFRecord currentRecord
        {
            get { return _currentRecord; }
        }

        protected override void createRecord(DXFDocument doc)
        {
            _currentRecord = new DXFAppIDRecord();
            doc.Tables.AppIDs.Add(_currentRecord);
        }
    }

}
