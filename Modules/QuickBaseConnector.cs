using DCElectricWebAPI.Models;
using Microsoft.Extensions.Options;
using System.Text;
using static DCElectricWebAPI.Models.QuickBaseLibrary;

namespace DCElectricWebAPI.Modules
{
    public class QuickBaseConnector  
    {
        string url = "https://api.quickbase.com";
        string slToken = "***REMOVED***";
        string domain = "dcelectricgroup.quickbase.com";
    
 


        //class-wide variables
        static Intuit.QuickBase.Client.IQApplication appSafe = null;
        static Intuit.QuickBase.Client.IQApplication appSL = null;
        static Intuit.QuickBase.Client.IQApplication appTS = null;
        static Intuit.QuickBase.Client.IQApplication appJL = null;
 
        IOptions<QuickBaseSettings>_settings;

        public QuickBaseConnector(IOptions<QuickBaseSettings> settings)
        {
            _settings = settings;
        }

        

        public async Task<QBDatabase?> GetApp(string appid)
        {

            using (var client = new HttpClient { BaseAddress = new Uri(url) })
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url + "/v1/apps/" + appid),
                    Method = HttpMethod.Get,
                };

                request.Headers.Add("Authorization", "QB-USER-TOKEN "+ slToken);
                request.Headers.Add("Qb-Realm-Hostname", domain);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
         
                    string responseContent = await response.Content.ReadAsStringAsync();
                    QBDatabase? retval = Newtonsoft.Json.JsonConvert.DeserializeObject<QBDatabase>(responseContent);                     
                    return retval;
                }
                else
                {
                    Console.WriteLine($"API request failed with status code: {response.StatusCode}");
                    throw new Exception();
                }

             
            }
 

        }
        public async Task<List<QBTable>?> GetTables(string appId)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(url) })
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url + "/v1/tables?appId=" + appId),
                    Method = HttpMethod.Get,
                };

                request.Headers.Add("Authorization", "QB-USER-TOKEN " + slToken);
                request.Headers.Add("Qb-Realm-Hostname", domain);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {

                    string responseContent = await response.Content.ReadAsStringAsync();
                    List<QBTable>? retval = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QBTable>>(responseContent);
                    return retval;
                }
                else
                {
                    Console.WriteLine($"API request failed with status code: {response.StatusCode}");
                    throw new Exception();
                }


            }

        }

        public async Task<QBResultSet?> Query(QBQuery query)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(url) })
            {
                //form "postable object" if that makes any sense
                var stringObject = Newtonsoft.Json.JsonConvert.SerializeObject(query);

                var content = new StringContent(stringObject.ToString(), Encoding.UTF8, "application/json");


                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url + "/v1/records/query"),
                    Method = HttpMethod.Post,
                    Content = content
                };

                request.Headers.Add("Authorization", "QB-USER-TOKEN " + slToken);
                request.Headers.Add("Qb-Realm-Hostname", domain);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {

                    string responseContent = await response.Content.ReadAsStringAsync();
                   QBResultSet? retval = Newtonsoft.Json.JsonConvert.DeserializeObject<QBResultSet>(responseContent);
                    return retval;
                }
                else
                {
                    Console.WriteLine($"API request failed with status code: {response.StatusCode}");
                    throw new Exception();
                }


            }

        }


        //public async Task<Type> convertCustomerTable(QBResultSet retval)
        //{

        //    foreach(var f in retval.fields  )
        //    {
        //        var fieldName = f.label.Replace(" ", "").Replace("#", "");

        //    }
        //}



        public Intuit.QuickBase.Client.IQApplication getQBApp(int intApp) //Logs into Quickbase and gets app
        {
            //variables
            string strDomain = _settings.Value.domain;
            string strToken = _settings.Value.token;
            string strJlToken = _settings.Value.jltoken; //assigned in Quickbase here: https://dcelectricgroup.quickbase.com/db/bkykszyj4?a=GetAppDevKey
            string strSafeToken = _settings.Value.safetoken;  



            string strApSlId = "";
            string strApTsId = "";
            string strApJlId = "";
            string strApSafeId = "";


            string strAccount = _settings.Value.account;
            string strPW = _settings.Value.password;

            //Log in and get app
            try
            {
                var client = Intuit.QuickBase.Client.QuickBase.Login(strAccount, strPW, strDomain);


                var apps = _settings.Value.apps;

                strApSlId = apps.streetlights;//Sl app id
                strApTsId =apps.ts; // TS app id
                strApJlId = apps.jobs; //JL app id
                strApSafeId =apps.safety; //Safety app id

                switch (intApp)
                {
                    case 0:

                        appSL = client.Connect(strApSlId, strToken);
                        return appSL;
                    case 1:
                        appTS = client.Connect(strApTsId, strToken);
                        return appTS;
                    case 2:

                        appJL = client.Connect(strApJlId, strJlToken);
                        return appJL;
                    case 3:
                        appSafe = client.Connect(strApSafeId, strSafeToken);
                        return appSafe;
                    default:
                        return null;
                }//end else
            }//end try
            catch (Exception ex)
            {
                writeLog(ex.Message.ToString() + " Message returned while processing GetQBApp. Check for errors in loading tables and columns.");
                return (null);
            }//end catch
        }//end getQBApp
        private static void writeLog(string msg) { }
    }

 
}
