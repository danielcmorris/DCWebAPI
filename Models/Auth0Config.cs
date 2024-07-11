namespace DCElectricWebAPI.Models
{
    public class Auth0Config
    {
        //Auth0 Management API  
        public static string identityConnection = "sitereview";




        public static string ManagementClient = "yblZIoySOafwCNLsoQDMt0n94vuz1s9y";
        public static string ManagementKey = "ntc1NwbcKC2f1g1xwkdecqSAr1IsV3c2oEfmaO2eqvXLEUT7fxMKqMH51EfkUS33";
        public static string ManagementGrantType = "password";


        public static string ManagementAudience { get { return "https://" + identityConnection + ".auth0.com/api/v2/"; } }
        public static string ManagementDomain { get { return identityConnection + ".auth0.com"; } }
        public static string TokenURL { get { return "https://" + identityConnection + ".auth0.com/oauth/token"; } }
        public static string UsersURL { get { return "https://" + identityConnection + ".auth0.com/api/v2/users"; } }


        // used for client authentication
        public static string Client = "sBSlyzjdO_O3QAmshDOgDkBR";
        public static string Auth0Key = "ntc1NwbcKC2f1g1xwkdecqSAr1IsV3c2oEfmaO2eqvXLEUT7fxMKqMH51EfkUS33";

        public static string UserURL(string username)
        {
            return $"https://{identityConnection}.auth0.com/api/v2/users?q=identities.connection:{identityConnection}%20AND%20username={username}&%20search_engine=v3";
        }


    }
}
