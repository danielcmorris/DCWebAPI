using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Intuit.QuickBase.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections;
using Intuit.QuickBase.Client;


namespace DCElectricWebAPI.Controllers
{




   
    [ApiController]
    public class FixtureController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;
        string strMaintPriceTableId = "bjrvqd35a";
        Hashtable htPriceLevel = new Hashtable();
        Hashtable htFixturePrice = new Hashtable();


        public FixtureController(IOptions<QuickBaseSettings> options)
        {

            _settings = options;

        }

        [HttpGet]
        [Route("api/fixture")]
        public async Task<IActionResult> Get(string customer, string pricelevel)
        {


            var strAccount = "shannon@dcelectricgroup.com";
            var strPW = "Ms$@DC01";
            var strDomain = "dcelectricgroup.quickbase.com";
            var strToken = "***REMOVED***";
            var strApId = "bjrvqd33c";
            var tableId = "";
            

            //var client = QuickBase.Login(strAccount, strPW, strDomain);
            var client = Intuit.QuickBase.Client.QuickBase.Login(strAccount, strPW, strDomain);
             var app = client.Connect("bjrvqd33c");

            var application = client.Connect(strApId, strToken);
            var table = application.GetTable("bjrvqd33q");
            table.Query();

            var qbc = new QuickBaseConnector(_settings);
          //  var app = qbc.getQBApp(2);
 
            var tblPrice = app.GetTable(strMaintPriceTableId);

            string strService = htPriceLevel[customer].ToString();

 
            Query qPrice = new Query();
            QueryStrings qsPrice = new QueryStrings(12, ComparisonOperator.EX, strService, LogicalOperator.NONE);

            try
            {
                qPrice.Add(qsPrice);
                tblPrice.Query(qPrice);
                int intRecCnt = tblPrice.Records.Count;
                if (intRecCnt == 0)
                {
                    throw new Exception("NORECS");
                }//end if no records
                foreach (var priceRow in tblPrice.Records)
                {
                   var decPrice = Convert.ToDecimal(priceRow["Maintenance Price"]);
                   var  strType = priceRow["Location Type"];
                    htFixturePrice.Add(strType, decPrice);
                }//end for each
            }//end try
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }


            return Ok(htFixturePrice);
        }
    }

}
