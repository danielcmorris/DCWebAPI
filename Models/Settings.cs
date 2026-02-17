namespace DCElectricWebAPI.Models
{
    public class QuickBaseSettings
    {
        public string domain { get; set; }
        public string token { get; set; }
        public string jltoken { get; set; }
        public string safetoken { get; set; }
        public string traffictoken { get; set; }
        public string account { get; set; }
        public string password { get; set; }
        public QuickBaseApps apps { get; set; }

    }
    public class QuickBaseApps
    {
        public string streetlights { get; set; }
        public string jobs { get; set; }
        public string safety { get; set; }
        public string ts { get; set; }
   
    }
    public class Connections
    {
        public string DefaultConnection { get; set; } = string.Empty;

    }
}
