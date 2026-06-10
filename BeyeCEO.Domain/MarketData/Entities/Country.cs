using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class Country
    {
        public string CountryCode { get; private set; } = string.Empty;
        public string NameEN { get; private set; } = string.Empty;
        public string NameAR { get; private set; } = string.Empty;
        public string CurrencyCode { get; private set; } = string.Empty;
        public string CurrencyNameEN { get; private set; } = string.Empty;
        public string CurrencyNameAR { get; private set; } = string.Empty;
        public string CentralBank { get; private set; } = string.Empty;
        public string StockExchange { get; private set; } = string.Empty;
        public string Region { get; private set; } = string.Empty;
        public string FlagUrl { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        // ← جديد
        public string StockScraperType { get; private set; } = "NONE";
        public string CentralBankScraperType { get; private set; } = "NONE";
        public string TimeZone { get; private set; } = "UTC";
        public bool HasLocalData { get; private set; } = false;

        // Navigation
        public ICollection<BankCountry> BankCountries { get; private set; }
            = new List<BankCountry>();

        private Country() { }

        public static Country Create(
            string countryCode, string nameEN, string nameAR,
            string currencyCode, string currencyNameEN, string currencyNameAR,
            string centralBank, string stockExchange, string region,
            string stockScraperType = "NONE",
            string centralBankScraperType = "NONE",
            string timeZone = "UTC")
        {
            if (countryCode.Length != 2)
                throw new ArgumentException("CountryCode must be 2 characters");

            return new Country
            {
                CountryCode = countryCode.ToUpper(),
                NameEN = nameEN,
                NameAR = nameAR,
                CurrencyCode = currencyCode.ToUpper(),
                CurrencyNameEN = currencyNameEN,
                CurrencyNameAR = currencyNameAR,
                CentralBank = centralBank,
                StockExchange = stockExchange,
                Region = region,
                StockScraperType = stockScraperType,
                CentralBankScraperType = centralBankScraperType,
                TimeZone = timeZone,
                HasLocalData = stockScraperType != "NONE",
                IsActive = true
            };
        }
    }
}
