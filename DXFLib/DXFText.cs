namespace DXFLib
{
    [Entity("TEXT")]
    public class DXFText : DXFGenericEntity
    {
        public string Text { get; set; }

        public double Thickness { get; set; }

        public double TextHeight { get; set; }

        public double TextRotation { get; set; }

        private DXFPoint start = new DXFPoint();

        public DXFPoint Start { get { return start; } }

        public override void ParseGroupCode(int groupcode, string value)
        {
            base.ParseGroupCode(groupcode, value);
            switch (groupcode)
            {
                case 1:
                    Text = value;
                    break;
                case 10:
                    Start.X = double.Parse(value);
                    break;
                case 20:
                    Start.Y = double.Parse(value);
                    break;
                case 39:
                    Thickness = double.Parse(value);
                    break;
                case 40:
                    TextHeight = double.Parse(value);
                    break;
                case 50:
                    TextRotation = double.Parse(value);
                    break;
                default:
                    base.ParseGroupCode(groupcode, value);
                    break;
            }
        }
    }
}
