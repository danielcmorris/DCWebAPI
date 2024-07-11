namespace DCElectricWebAPI.Models
{
    public class Auth0AccessToken
    {
        public string access_token { get; set; }
        public string id_token { get; set; }
        public string scope { get; set; }
        public double expires_in { get; set; }
        public string token_type { get; set; }
 

    }
}
