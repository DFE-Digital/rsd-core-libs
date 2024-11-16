namespace DfE.CoreLibs.Security.Configurations
{
    public class TokenSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int TokenLifetimeMinutes { get; set; } = 5;
    }
}
