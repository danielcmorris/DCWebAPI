using System.Data;

namespace DCElectricWebAPI.Modules
{
    public class DefaultDataLayer
    {
        public string ConnectionString = "";

        public DataSet GetData(string sql)
        {
            return new DataSet();
        }

        public int RunSQL(string sel)
        {
            return 1;
        }

        public string RunSQL_String(string sel)
        {
            return "string result";
        }
    }
}
