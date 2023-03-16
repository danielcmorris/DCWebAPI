using DCElectricWebAPI.Models;
using Intuit.QuickBase.Client;
using Microsoft.Extensions.Options;
using System.Configuration;

namespace DCElectricWebAPI.Modules
{
    public class QuickBaseConnector  
    {

        //class-wide variables
        static Intuit.QuickBase.Client.IQApplication appSL = null;
        static Intuit.QuickBase.Client.IQApplication appTS = null;
        static Intuit.QuickBase.Client.IQApplication appJL = null;
        static Intuit.QuickBase.Client.IQApplication appSafe = null;

        IOptions<QuickBaseSettings>_settings;

        public QuickBaseConnector(IOptions<QuickBaseSettings> settings)
        {
            _settings = settings;
        }


        public  Intuit.QuickBase.Client.IQApplication getQBApp(int intApp) //Logs into Quickbase and gets app
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
