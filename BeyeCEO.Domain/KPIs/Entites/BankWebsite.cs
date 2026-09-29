using BeyeCEO.Domain.Shared;
using System;

namespace BeyeCEO.Domain.KPIs.Entites
{
    public class BankWebsite : BaseEntity
    {
        public Guid? BankId { get; private set; }
        public string BankName { get; private set; } = string.Empty;
        public string? BankNameAR { get; private set; }
        public string CountryCode { get; private set; } = string.Empty;
        public string WebsiteUrl { get; private set; } = string.Empty;
        public string ScrapingStrategy { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        private BankWebsite() { }

        public static BankWebsite Create(
            string bankName, string? bankNameAR, string countryCode,
            string websiteUrl, string scrapingStrategy, Guid? bankId = null)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                throw new ArgumentException("BankName is required");

            if (string.IsNullOrWhiteSpace(websiteUrl))
                throw new ArgumentException("WebsiteUrl is required");

            if (string.IsNullOrWhiteSpace(scrapingStrategy))
                throw new ArgumentException("ScrapingStrategy is required");

            return new BankWebsite
            {
                BankId = bankId,
                BankName = bankName,
                BankNameAR = bankNameAR,
                CountryCode = countryCode.ToUpper(),
                WebsiteUrl = websiteUrl,
                ScrapingStrategy = scrapingStrategy,
                IsActive = true
            };
        }
    }
}
