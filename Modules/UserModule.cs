using DCElectricWebAPI.Models;

namespace DCElectricWebAPI.Modules
{
    public class UserModule
    {
        public bool Secured = true;
        private string SessionID;
        private User user;
        DataLayerBase _dl;
        public UserModule(string sid, DataLayerBase dl)
        {
            _dl = dl;
            SessionID = sid.Replace("Bearer ","") ;
           
            user = GetUserBySessionID(SessionID);
          

            Secured = user.UserId>0?true:false; 
             
        }

        private User GetUserBySessionID(string sid)
        {
            string sql = $"SELECT * FROM fn_security_user_by_session_id('{SessionID}');";
           
                var userSet = _dl.Query<User>(sql);
                // No matching session (expired/invalid) -> return an empty user so
                // Secured becomes false and callers return 401, instead of First()
                // throwing "Sequence contains no elements" (a 500).
                return userSet.FirstOrDefault() ?? new User();
             
        }
    }
}
