using DCElectricWebAPI.Models;
using Microsoft.Extensions.Options;
using System.Text;
using static DCElectricWebAPI.Models.QuickBaseLibrary;

namespace DCElectricWebAPI.Modules
{
    /// <summary>
    /// QuickBase REST API connector - uses modern REST API exclusively
    /// </summary>
    public class QuickBaseConnector
    {
        private readonly string _url = "https://api.quickbase.com";
        private readonly string _token = "***REMOVED***";
        private readonly string _domain = "dcelectricgroup.quickbase.com";
        private readonly IOptions<QuickBaseSettings> _settings;

        public QuickBaseConnector(IOptions<QuickBaseSettings> settings)
        {
            _settings = settings;
        }

        

        public async Task<QBDatabase?> GetApp(string appid)
        {

            using (var client = new HttpClient { BaseAddress = new Uri(_url) })
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(_url + "/v1/apps/" + appid),
                    Method = HttpMethod.Get,
                };

                request.Headers.Add("Authorization", "QB-USER-TOKEN "+ _token);
                request.Headers.Add("Qb-Realm-Hostname", _domain);
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
            using (var client = new HttpClient { BaseAddress = new Uri(_url) })
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(_url + "/v1/tables?appId=" + appId),
                    Method = HttpMethod.Get,
                };

                request.Headers.Add("Authorization", "QB-USER-TOKEN " + _token);
                request.Headers.Add("Qb-Realm-Hostname", _domain);
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

        public async Task<List<QBFieldDetails>?> GetFields(string tableId)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(_url) })
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(_url + "/v1/fields?tableId=" + tableId),
                    Method = HttpMethod.Get,
                };

                request.Headers.Add("Authorization", "QB-USER-TOKEN " + _token);
                request.Headers.Add("Qb-Realm-Hostname", _domain);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    List<QBFieldDetails>? retval = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QBFieldDetails>>(responseContent);
                    return retval;
                }
                else
                {
                    Console.WriteLine($"API request failed with status code: {response.StatusCode}");
                    throw new Exception($"Failed to get fields: {response.StatusCode}");
                }
            }
        }

        public async Task<QBResultSet?> Query(QBQuery query)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(_url) })
            {
                //form "postable object" if that makes any sense
                var stringObject = Newtonsoft.Json.JsonConvert.SerializeObject(query);

                // Debug logging
                Console.WriteLine($"QuickBase Query JSON: {stringObject}");

                var content = new StringContent(stringObject.ToString(), Encoding.UTF8, "application/json");


                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(_url + "/v1/records/query"),
                    Method = HttpMethod.Post,
                    Content = content
                };

                request.Headers.Add("Authorization", "QB-USER-TOKEN " + _token);
                request.Headers.Add("Qb-Realm-Hostname", _domain);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {

                    string responseContent = await response.Content.ReadAsStringAsync();
                   QBResultSet? retval = Newtonsoft.Json.JsonConvert.DeserializeObject<QBResultSet>(responseContent);
                    return retval;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API request failed with status code: {response.StatusCode}, Body: {errorBody}");
                    throw new Exception($"QuickBase Query failed: {response.StatusCode} - {errorBody}");
                }


            }

        }


    }
}
