namespace RoBin
{

    public class DataInfo
    {
        public string name { get; set; }
        public object Object { get; set; }
        public DataInfo(string _name, object value)
        {
            name = _name;
            Object = value;
        }
    }


}
