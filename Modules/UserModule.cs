using DCElectricWebAPI.Models;

namespace DCElectricWebAPI.Modules
{
    public class UserModule
    {
        public bool Secured = true;
        private string SessionID;
        private User user;

        public UserModule(string sid)
        {
            SessionID = sid.Replace("Bearer ","") ;
            var dl = new DataLayerBase();
            user = GetUserBySessionID(SessionID);

            
            Secured = user.UserId>0?true:false; 
             
        }

        private User GetUserBySessionID(string sid)
        {
            string sql = $"SELECT * FROM  [dbo].[fnSecurity_UserBySessionId]('{SessionID}');";
            using (var dl = new DataLayerBase())
            {
                var userSet = dl.Query<User>(sql);
                return userSet.First();
            };
        }
    }
}
