using BeyeCEO.Domain.Shared;
using System;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class BankAd : BaseEntity
    {
        public string BankName { get; private set; } = string.Empty;
        public string? BankNameAR { get; private set; }
        public string CountryCode { get; private set; } = string.Empty;
        public string ImageUrl { get; private set; } = string.Empty;
        public string? AltText { get; private set; }
        public string? SourceUrl { get; private set; }
        public DateTime ScrapedAt { get; private set; }
        public bool IsActive { get; private set; } = true;

        private BankAd() { }

        public static BankAd Create(
            string bankName, string? bankNameAR, string countryCode,
            string imageUrl, string? altText, string? sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("ImageUrl is required");

            return new BankAd
            {
                BankName = bankName,
                BankNameAR = bankNameAR,
                CountryCode = countryCode.ToUpper(),
                ImageUrl = imageUrl,
                AltText = altText,
                SourceUrl = sourceUrl,
                ScrapedAt = DateTime.UtcNow,
                IsActive = true
            };
        }
    }
}
