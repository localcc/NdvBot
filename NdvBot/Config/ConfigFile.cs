namespace NdvBot.Config
{
    public class ConfigFile
    {
        public static ConfigFile? Current;
        
        public string Token { get; set; }
        
        public string DatabaseHost { get; set; }
        public int DatabasePort { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseUsername { get; set; }
        public string CACertName { get; set; }
        public string CertName { get; set; }
        public string CertPassword { get; set; }
    }
}