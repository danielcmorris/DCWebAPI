
using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace DCElectricWebAPI.Moduiles
{
    public class SecurityModule
    {
        DefaultDataLayer dl = new DefaultDataLayer();
        public string SessionId;

        public User currentUser = new User();

        public Auth0AccessToken tokens;

        //Auth0 Management API  
        public string Auth0ManagementUrl = "https://dcelectric.auth0.com/oauth/token";
        public string Auth0ManagementDomain = "dcelectric.us.auth0.com";
        public string Auth0ManagementClient = "eBzCUUvoyEAhTwxEaAn8u8S2VA2u8A6l";
        public string Auth0ManagementKey = "IqQWw5aKrEY84LaJ-k5_yDWyZVoSEJ6zIjsCqHD_aVpa20ZS_Lir8o7_GI2WZ_An";

        public string gatekey;
        public string Auth0Client = "LTIVRPTs7DBEyNuOGPa85RI0J2d5cfda";
        public string Auth0Key = "C2_Na4zuSQTzaY1R0-kEmSQeZdvm2RD8D_qd-PERP9zj0gYy93JAmDMQj_Y1fdgU";

        public JwtSecurityToken decryptJWT(string id_token)
        {


            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(id_token) as JwtSecurityToken;

            return tokenS;
        }


        /// <summary>
        /// Check to see if user is in current SQL Database.  This simply checks to see if they are an existing active user.  All passwords are held by Auth0
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string CheckLogin(string username)
        {
            string sql = "strpLogin '" + username + "';";
            DataSet ds = dl.GetData(sql);
            if (ds.Tables.Count > 0)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {

                    DataRow dr = ds.Tables[0].Rows[0];

                    SessionId = (string)dr["sessionkey"];
                    currentUser.SessionId = (string)dr["sessionkey"];
                    currentUser.UserId = (int)dr["internalcontactsid"];
                    currentUser.UserName = (string)dr["username"];
                    currentUser.OfficeId = (int)dr["office_id"];
                    currentUser.FirstName = (string)dr["First"];
                    currentUser.LastName = (string)dr["Last"];
                    currentUser.Email = (string)dr["email"] ?? "";
                    currentUser.UserGroup = (string)dr["Group"];
                    currentUser.CustomPermissions = (string)dr["CustomPermissions"] ?? "";
                    currentUser.UserLevel = (string)dr["Level"] ?? "";

                    return "SUCCESS";


                }
                else
                {
                    return "ERROR:No Record Found";
                }
            }
            else
            {
                return "ERROR: Stored Procedure Failed";
            }




        }

        /// <summary>
        /// Send credentials to Auth0 to login and get Tokens
        /// </summary>
        /// <param name="creds"></param>
        /// <returns></returns>

        public Auth0AccessToken Auth0(LoginCredentials creds)
        {


            WebClient client = new WebClient();

            Dictionary<string, Object> payload = new Dictionary<string, object>();
            payload.Add("grant_type", "password");
            payload.Add("audience", "https://dcelectric.us.auth0.com/api/v2/");
            payload.Add("client_id", this.Auth0Client);
            payload.Add("client_secret", this.Auth0Key);
            //payload.Add("username", creds.username);
            //payload.Add("password", creds.password);
            //payload.Add("scope", "openid profile email address phone ");



            string json = JsonConvert.SerializeObject(payload);

            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            string url = "https://dcelectric.us.auth0.com/oauth/token/";
            string responsedata = "";
            try
            {
                responsedata = client.UploadString(new Uri(url), "POST", json);
                this.tokens = JsonConvert.DeserializeObject<Auth0AccessToken>(responsedata);
                //this.tokens = new Auth0AccessToken() { id_token = responsedata.id_token, expires_in = responsedata.expires_in, scope = responsedata.scope, access_token = responsedata.access_token, token_type = responsedata.token_type};

                // If we need to access anything in the id_token, we can decrypt it here
                //var id_token = this.tokens["id_token"].ToString();
                //JwtSecurityToken decrypt = this.decryptJWT(id_token);

                return this.tokens;

            }
            catch (Exception e)
            {
                var retval = new Auth0AccessToken() { id_token = e.Message };

                this.tokens = retval;
                return retval;

            }

        }




        /// <summary>
        /// Get an administrative security token from Auth0 if you don't already have one
        /// </summary>
        /// <returns></returns>
        public string Auth0Token()
        {
            WebClient client = new WebClient();

            Dictionary<string, Object> payload = new Dictionary<string, object>();
            payload.Add("grant_type", "client_credentials");
            payload.Add("audience", $"https://{this.Auth0ManagementDomain}/api/v2/");
            payload.Add("client_id", this.Auth0ManagementClient);
            payload.Add("client_secret", this.Auth0ManagementKey);


            string json = JsonConvert.SerializeObject(payload);

            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            string url = this.Auth0ManagementUrl;
            string responsedata = client.UploadString(new Uri(url), "POST", json);

            JObject tokens = JObject.Parse(responsedata);
            string access_token = (string)(tokens["access_token"] ?? "");
            return access_token;
        }

        /// <summary>
        /// Check to see if the user already exists in the Auth0 DB.  You only need the username in the login creds for this to work
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="AuthToken"></param>
        /// <returns></returns>
        public Auth0User[] checkAuth0Membership(LoginCredentials creds, string AuthToken = "")
        {

            string url = $"https://avms.auth0.com/api/v2/users?q=identities.connection:AVMS%20AND%20username={creds.username}&%20search_engine=v3";

            WebClient client = new WebClient();
            if (AuthToken == "")
            {
                AuthToken = this.Auth0Token();
            }
            string token = "Bearer " + AuthToken;
            client.Headers.Add(HttpRequestHeader.Authorization, token);


            Stream data = client.OpenRead(url);
            StreamReader reader = new StreamReader(data);
            string responsedata = reader.ReadToEnd();
            data.Close();
            reader.Close();
            var result = JsonConvert.DeserializeObject<Auth0User[]>(responsedata);

            return result;



        }

        /// <summary>
        /// Create a new user in the auth0 database.  
        /// </summary>
        /// <param name="user"></param>
        /// <param name="AuthToken"></param>
        /// <returns></returns>
        public Auth0User Auth0UserCreate(User currentUser, string AuthToken = "")
        {

            WebClient client = new WebClient();
            if (AuthToken == "")
            {
                AuthToken = this.Auth0Token();
            }


            dynamic userMeta = new JObject();
            userMeta.given_name = currentUser.FirstName;
            userMeta.family_name = currentUser.LastName;

            dynamic appMeta = new JObject();
            appMeta.level = currentUser.UserLevel;
            appMeta.custom_permissions = currentUser.CustomPermissions;
            appMeta.office_id = currentUser.OfficeId;
            appMeta.internalcontactsid = currentUser.UserId;


            dynamic userData = new JObject();
            userData.connection = "AVMS";
            userData.email = currentUser.Email;
            userData.nickname = currentUser.FirstName + " " + currentUser.LastName;
            userData.name = currentUser.FirstName + " " + currentUser.LastName;
            userData.family_name = currentUser.LastName;
            userData.given_name = currentUser.FirstName;
            userData.username = currentUser.UserName;
            userData.password = currentUser.Password;
            userData.app_metadata = appMeta;
            userData.user_metadata = userMeta;
            string json = JsonConvert.SerializeObject(userData);
            string token = "Bearer " + AuthToken;
            client.Headers.Add(HttpRequestHeader.Authorization, token);
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            string url = "https://avms.auth0.com/api/v2/users";
            string responsedata;
            try
            {
                responsedata = client.UploadString(new Uri(url), "POST", json);
            }
            catch (WebException exception)
            {

                using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                {
                    responsedata = reader.ReadToEnd();
                }
            }

            var result = JsonConvert.DeserializeObject<Auth0User>(responsedata);
            return result;

        }

        /// <summary>
        /// Update an existing user.  
        /// note: Cannot update username and password simultaneously
        /// </summary>
        /// <param name="user"></param>
        /// <param name="AuthToken"></param>
        /// <returns></returns>
        public Auth0User Auth0UserUpdate(Auth0User user, string commandType, string AuthToken)
        {


            WebClient client = new WebClient();
            if (AuthToken == "")
            {
                AuthToken = this.Auth0Token();
            }



            dynamic userMeta = new JObject();
            userMeta.given_name = user.given_name;
            userMeta.family_name = user.family_name;
            dynamic appMeta = new JObject();
            appMeta.status = user.app_metadata.status;
            appMeta.level = user.app_metadata.level;
            appMeta.custom_permissions = user.app_metadata.custom_permissions;
            appMeta.office_id = user.app_metadata.office_id;
            appMeta.internalcontactsid = user.app_metadata.internal_contacts_id;



            dynamic userData = new JObject();
            userData.connection = "AVMS";
            if (commandType == "password")
                userData.password = user.password;
            if (commandType == "username")
                userData.username = user.username;
            if (commandType == "email")
                userData.email = user.email;

            if (commandType == "meta")
            {

                userData.user_metadata = userMeta;
                userData.app_metadata = appMeta;
            }

            string json = JsonConvert.SerializeObject(userData);
            string token = "Bearer " + AuthToken;
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(HttpRequestHeader.Authorization, token);

            string url = "https://avms.auth0.com/api/v2/users/" + user.user_id;
            string responsedata;
            try
            {
                //responsedata = client.UploadString(new Uri(url), "PATCH", json);
                responsedata = client.UploadString(url, "PATCH", json);
            }
            catch (WebException exception)
            {

                using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                {
                    responsedata = reader.ReadToEnd();
                }
            }

            var result = JsonConvert.DeserializeObject<Auth0User>(responsedata);
            return result;

        }


    }

    public class Auth0User
    {

        public string email { get; set; }
        public string username { get; set; }
        public string email_verified { get; set; }
        public string user_id { get; set; }
        public string picture { get; set; }
        public string nickname { get; set; }
        public string password { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }

        public Auth0Identities[] identities { get; set; }
        public Auth0AppMetaData app_metadata { get; set; }
        public Auth0UserMetaData user_metadata { get; set; }


    }
    public class Auth0Identities
    {
        public string user_id { get; set; }
        public string provider { get; set; }
        public string connection { get; set; }

    }
    public class Auth0AppMetaData
    {
        public string internal_contacts_id { get; set; }
        public string custom_permissions { get; set; }
        public string office_id { get; set; }
        public string session_id { get; set; }
        public string level { get; set; }
        public string status { get; set; }
    }
    public class Auth0UserMetaData
    {
        public string family_name { get; set; }
        public string given_name { get; set; }
    }
}