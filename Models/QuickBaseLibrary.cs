using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace DCElectricWebAPI.Models
{
    public class QuickBaseLibrary
    {
        public class QBTable
        {

            public string alias { get; set; }
            public DateTime created { get; set; }
            public int defaultSortFieldId { get; set; }
            public string defaultSortOrder { get; set; }
            public string description { get; set; }
            public string id { get; set; }
            public int keyFieldId { get; set; }
            public string name { get; set; }
            public int nextFieldId { get; set; }
            public int nextRecordId { get; set; }
            public string pluralRecordName { get; set; }
            public string singleRecordName { get; set; }
            public string sizeLimit { get; set; }
            public string spaceRemaining { get; set; }
            public string spaceUsed { get; set; }
            public DateTime updated { get; set; }



        }
        public class QBDatabase

        {
            public string id { get; set; }
            public string name { get; set; }
            public string ancestorId { get; set; }
            public string ancestorName { get; set; }
            public DateTime created { get; set; }
            public string dateFormat { get; set; }
            public string description { get; set; }
            public string timeZone { get; set; }
            public DateTime updated { get; set; }


        }


        public class QBQuery
        {
            public string from { get; set; }
            public List<int> select { get; set; }
            public string? where { get; set; }
            public List<QBFieldSet>? sortBy { get; set; }
            public QBQueryOptions options { get; set; }

        }
        public class QBFieldSet
        {
            public int fieldId { get; set; }
            public string? order { get; set; }
            public string? grouping { get; set; }
        }
        public class QBQueryOptions
        {
            public int skip { get; set; } = 0;
            public int top { get; set; } = 0;
            public bool compareWithAppLocalTime { get; set; } = false;

        }
        public class QBField
        {
            public int id { get; set; }
            public string label { get; set; }
            public string type { get; set; }
         
        }

       
        public class QBResultSet
        {
            public List<JObject>? data { get; set; }
            public List<QBField> fields { get; set; }
            public dynamic metadata { get; set; }

            

        }

       public class QBResultData
        {

        }

    }
}
