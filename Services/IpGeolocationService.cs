using BlockedConuntries.Models;
using Newtonsoft.Json;

namespace BlockedConuntries.Services
{
    public class IpGeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public IpGeolocationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.BaseAddress = new Uri(_config["IpApi:BaseUrl"]!); // https://api.ipgeolocation.io/
        }

        public async Task<IpLookupResponse> GetCountryByIpAsync(string ipAddress)
        {
            try
            {
                var apiKey = _config["IpApi:ApiKey"];
                var response = await _httpClient.GetAsync($"ipgeo?apiKey={apiKey}&ip={ipAddress}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                return new IpLookupResponse
                {
                    CountryCode = data!.country_code2,
                    CountryName = data.country_name,
                    Isp = data.isp
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error fetching geolocation for IP '{ipAddress}': {ex.Message}", ex);
            }
        }
    }
}
