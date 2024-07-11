namespace DCElectricWebAPI.Modules
{
    public class UserModule
    {
        public bool Secured = true;
        private string SessionID;

        public UserModule(string sid)
        {
            SessionID = sid;
        }

        
    }
}
