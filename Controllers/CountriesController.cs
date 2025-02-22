using BlockedConuntries.Models;
using BlockedConuntries.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BlockedConuntries.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesController : ControllerBase
    {
        private readonly InMemoryStorageService _storage;
        private readonly IpGeolocationService _geoService;

        public CountriesController(InMemoryStorageService storage, IpGeolocationService geoService)
        {
            _storage = storage;
            _geoService = geoService;
        }

        [HttpPost("block")]
        public IActionResult BlockCountry([FromBody] Country country)
        {
            if (string.IsNullOrWhiteSpace(country.Code) || !_storage.AddBlockedCountry(country.Code, country.Name))
                return BadRequest("Invalid or duplicate country code.");
            return Ok();
        }

        [HttpDelete("block/{countryCode}")]
        public IActionResult UnblockCountry(string countryCode)
        {
            if (!_storage.RemoveBlockedCountry(countryCode)) return NotFound();
            return NoContent();
        }

        [HttpGet("blocked")]
        public IActionResult GetBlockedCountries([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "")
        {
            var countries = _storage.GetBlockedCountries()
                .Where(c => string.IsNullOrEmpty(search) || c.Code.Contains(search, StringComparison.OrdinalIgnoreCase) || c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Ok(countries);
        }

        [HttpGet("ip/lookup")]
        public async Task<IActionResult> LookupIp([FromQuery] string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress)) ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString()!;
            if (!IPAddress.TryParse(ipAddress, out _)) return BadRequest("Invalid IP address format.");
            var result = await _geoService.GetCountryByIpAsync(ipAddress);
            return Ok(result);
        }
        [HttpGet("ip/check-block")]
        public async Task<IActionResult> CheckIpBlocked()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ip) || ip == "::1" || ip == "127.0.0.1")
            {
                return BadRequest("Localhost IP addresses are not supported for geolocation checks.");
            }
            try
            {
                var geo = await _geoService.GetCountryByIpAsync(ip);
                var isBlocked = _storage.IsCountryBlocked(geo.CountryCode);
                _storage.LogBlockedAttempt(new BlockedAttemptLog
                {
                    IpAddress = ip,
                    Timestamp = DateTime.UtcNow,
                    CountryCode = geo.CountryCode,
                    IsBlocked = isBlocked,
                    UserAgent = Request.Headers["User-Agent"]!
                });
                return Ok(new { IsBlocked = isBlocked });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error checking IP block status: {ex.Message}");
            }
        }

        [HttpGet("logs/blocked-attempts")]
        public IActionResult GetBlockedAttempts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var logs = _storage.GetBlockedAttempts()
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Ok(logs);
        }

        [HttpPost("temporal-block")]
        public IActionResult TemporalBlock([FromBody] dynamic request)
        {
            string countryCode = request.countryCode;
            int durationMinutes = request.durationMinutes;
            if (string.IsNullOrWhiteSpace(countryCode) || durationMinutes < 1 || durationMinutes > 1440)
                return BadRequest("Invalid country code or duration.");
            if (_storage.IsCountryBlocked(countryCode)) return Conflict("Country already blocked.");
            _storage.AddTemporalBlock(countryCode, durationMinutes);
            return Ok();
        }
    }
}
