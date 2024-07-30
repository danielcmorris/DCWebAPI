using DCElectricWebAPI.Models;

namespace DCElectricWebAPI.Modules
{
    public class UserModule
    {
        public bool Secured = true;
        private string SessionID;
        private int UserID;

        public UserModule(string sid)
        {
            SessionID = sid.Replace("Bearer ","") ;
            var dl = new DataLayerBase();
            UserID= dl.RunSQL($"SELECT ISNULL(UserID,0) FROM  [dbo].[fnSecurity_UserBySessionId]('{SessionID}');");
            Secured = UserID>0?true:false; 
             
        }

        
    }
}
