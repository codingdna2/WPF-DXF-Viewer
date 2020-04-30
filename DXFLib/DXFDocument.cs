using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace DXFLib
{
    //https://documentation.help/AutoCAD-DXF/
    public class DXFDocument
    {
        private DXFHeader header = new DXFHeader();

        public DXFHeader Header { get { return header; } }

        private List<DXFClass> classes = new List<DXFClass>();

        public List<DXFClass> Classes { get { return classes; } }

        private List<DXFBlock> blocks = new List<DXFBlock>();

        public List<DXFBlock> Blocks { get { return blocks; } }

        private DXFTables tables = new DXFTables();

        public DXFTables Tables { get { return tables; } }

        private List<DXFEntity> entities = new List<DXFEntity>();

        public List<DXFEntity> Entities { get { return entities; } }

        public DXFDocument()
        {

        }

        public void Load(string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Open);
            Load(stream);
            stream.Close();
        }

        private enum LoadState
        {
            OutsideSection,
            InSection
        }

        public event EventHandler OnFileVersionIncompatible;

        public void Load(Stream file)
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            bool versionwarningsent = false;

            Dictionary<string, ISectionParser> sectionparsers = new Dictionary<string, ISectionParser>();

            sectionparsers["HEADER"] = new HeaderParser();
            sectionparsers["CLASSES"] = new ClassParser();
            sectionparsers["TABLES"] = new TableParser();
            sectionparsers["ENTITIES"] = new EntityParser();
            sectionparsers["BLOCKS"] = new BlockParser();
            ISectionParser currentParser = null;

            TextReader reader = new StreamReader(file);
            LoadState state = LoadState.OutsideSection;
            int? groupcode;
            string value;
            reader.ReadDXFEntry(out groupcode, out value);
            while (groupcode != null)
            {
                switch (state)
                {
                    case LoadState.OutsideSection:
                        if (groupcode == 0 && value.Trim() == "SECTION")
                        {
                            state = LoadState.InSection;
                            reader.ReadDXFEntry(out groupcode, out value);
                            if (groupcode != 2)
                                throw new InvalidDataException("Section with no name found");

                            value = value.Trim();
                            if (sectionparsers.ContainsKey(value))
                                currentParser = sectionparsers[value];
                        }
                        break;
                    case LoadState.InSection:
                        if (groupcode == 0 && value.Trim() == "ENDSEC")
                        {
                            state = LoadState.OutsideSection;
                            //after each section check wether the File Version is set
                            if (Header.AutoCADVersion != null &&
                                Header.AutoCADVersion != "AC1014")
                            {
                                if (!versionwarningsent)
                                {
                                    OnFileVersionIncompatible?.Invoke(this, EventArgs.Empty);
                                    versionwarningsent = true;
                                }
                            }
                        }
                        else
                        {
                            if (currentParser != null)
                            {
                                currentParser.ParseGroupCode(this, (int)groupcode, value);
                            }
                        }
                        break;
                    default:
                        break;
                }
                reader.ReadDXFEntry(out groupcode, out value);
            }
            if (state == LoadState.InSection)
                throw new InvalidDataException("End of file reached with an open section");

            Thread.CurrentThread.CurrentCulture = currentCulture;
        }
    }
}
