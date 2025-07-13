using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KiteConnectApi.Services
{
    public class VaultService
    {
        private readonly IVaultClient _vaultClient;
        private readonly ILogger<VaultService> _logger;
        private readonly IConfiguration _configuration;

        public VaultService(IConfiguration configuration, ILogger<VaultService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var vaultAddress = _configuration["Vault:Address"];
            var vaultToken = _configuration["Vault:Token"];

            if (string.IsNullOrEmpty(vaultAddress) || string.IsNullOrEmpty(vaultToken))
            {
                _logger.LogWarning("Vault address or token not configured. Skipping Vault initialization.");
                return;
            }

            var vaultClientSettings = new VaultClientSettings(vaultAddress, new TokenAuthMethodInfo(vaultToken));
            _vaultClient = new VaultClient(vaultClientSettings);
        }

        public async Task<Secret<SecretData>?> GetSecretAsync(string path)
        {
            if (_vaultClient == null)
            {
                _logger.LogWarning("Vault client not initialized. Cannot retrieve secret.");
                return null;
            }

            try
            {
                var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: path, mountPoint: "secret");
                return secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading secret from Vault at path: {path}");
                return null;
            }
        }
    }
}