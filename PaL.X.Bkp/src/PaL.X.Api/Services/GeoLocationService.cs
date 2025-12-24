using System.Text.Json;

namespace PaL.X.API.Services
{
    public interface IGeoLocationService
    {
        Task<string?> GetCountryFromIpAsync(string ipAddress);
    }

    public class GeoLocationService : IGeoLocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeoLocationService> _logger;

        public GeoLocationService(HttpClient httpClient, ILogger<GeoLocationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> GetCountryFromIpAsync(string ipAddress)
        {
            // Skip localhost/private IPs
            if (string.IsNullOrEmpty(ipAddress) || 
                ipAddress == "::1" || 
                ipAddress == "127.0.0.1" || 
                ipAddress.StartsWith("192.168.") ||
                ipAddress.StartsWith("10.") ||
                ipAddress.StartsWith("172."))
            {
                return "Local";
            }

            try
            {
                // Using free ip-api.com service (150 requests/minute limit)
                var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}?fields=country");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("country", out var countryProp))
                    {
                        return countryProp.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get country for IP {ipAddress}: {ex.Message}");
            }

            return "Unknown";
        }
    }
}
