namespace DCElectricWebAPI.Models
{
    public class User
    {

        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string WorkPhone { get; set; }
        public string HomePhone { get; set; }
        public string CellPhone { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserLevel { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> StatusDate { get; set; }
        public string UserGroup { get; set; }
        public Nullable<int> OfficeId { get; set; }
        public string CustomPermissions { get; set; }
        public string OfficeName { get; set; }
        public string OfficeNumber { get; set; }
        public string SessionId { get; set; }
    }

    public class UserOfficeAccess
    {

        public int UserId { get; set; }
        public int OfficeId { get; set; }
    }
}
