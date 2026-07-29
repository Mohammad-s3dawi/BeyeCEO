using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.ExternalServices
{
    public class TranslationService
    {
        private readonly HttpClient _http;
        private readonly ILogger<TranslationService> _logger;
        private const string MyMemoryUrl = "https://api.mymemory.translated.net/get";

        public TranslationService(HttpClient http, ILogger<TranslationService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            try
            {
                var langPair = $"{sourceLang}|{targetLang}";
                var url = $"{MyMemoryUrl}?q={Uri.EscapeDataString(text)}&langpair={langPair}";
                var response = await _http.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                var translation = json.RootElement
                    .GetProperty("responseData")
                    .GetProperty("translatedText")
                    .GetString();
                return translation ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Translation failed: {Message}", ex.Message);
                return string.Empty;
            }
        }
    }
}
