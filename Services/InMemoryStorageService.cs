using BlockedConuntries.Models;
using System.Collections.Concurrent;

namespace BlockedConuntries.Services
{
    public class InMemoryStorageService
    {
        private readonly ConcurrentDictionary<string, Country> _blockedCountries = new();
        private readonly ConcurrentDictionary<string, TemporalBlock> _temporalBlocks = new();
        private readonly List<BlockedAttemptLog> _blockedAttempts = new();

        public bool AddBlockedCountry(string code, string name) =>
            _blockedCountries.TryAdd(code.ToUpper(), new Country { Code = code.ToUpper(), Name = name });

        public bool RemoveBlockedCountry(string code) =>
            _blockedCountries.TryRemove(code.ToUpper(), out _);

        public List<Country> GetBlockedCountries() => _blockedCountries.Values.ToList();

        public bool AddTemporalBlock(string code, int durationMinutes)
        {
            var expiration = DateTime.UtcNow.AddMinutes(durationMinutes);
            return _temporalBlocks.TryAdd(code.ToUpper(), new TemporalBlock { CountryCode = code.ToUpper(), Expiration = expiration });
        }

        public void RemoveExpiredTemporalBlocks()
        {
            var now = DateTime.UtcNow;
            var expired = _temporalBlocks.Where(kvp => kvp.Value.Expiration <= now).Select(kvp => kvp.Key).ToList();
            foreach (var code in expired) _temporalBlocks.TryRemove(code, out _);
        }

        public bool IsCountryBlocked(string code) =>
            _blockedCountries.ContainsKey(code.ToUpper()) ||
            (_temporalBlocks.TryGetValue(code.ToUpper(), out var block) && block.Expiration > DateTime.UtcNow);

        public void LogBlockedAttempt(BlockedAttemptLog log)
        {
            lock (_blockedAttempts) { _blockedAttempts.Add(log); }
        }

        public List<BlockedAttemptLog> GetBlockedAttempts() => _blockedAttempts;
    }
}
