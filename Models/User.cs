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
        public string UserLevel { get; set; } = "User";
        public string Password { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Permissions { get; set; } = "None";
        public string Status { get; set; } = "Active";

    }
    public class Credentials
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
