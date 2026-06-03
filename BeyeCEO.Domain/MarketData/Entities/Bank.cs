using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class Bank:BaseEntity
    {
        public string NameEN { get; private set; } = string.Empty;
        public string NameAR { get; private set; } = string.Empty;
        public string LogoUrl { get; private set; } = string.Empty;
        public string WebsiteUrl { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        // Navigation
        public ICollection<BankCountry> BankCountries { get; private set; } = new List<BankCountry>();

        private Bank() { }

        public static Bank Create(string nameEN, string nameAR,
            string logoUrl = "", string websiteUrl = "")
        {
            if (string.IsNullOrWhiteSpace(nameEN))
                throw new ArgumentException("Bank name is required");

            return new Bank
            {
                NameEN = nameEN,
                NameAR = nameAR,
                LogoUrl = logoUrl,
                WebsiteUrl = websiteUrl,
                IsActive = true
            };
        }

        public void UpdateInfo(string nameEN, string nameAR, string logoUrl)
        {
            NameEN = nameEN;
            NameAR = nameAR;
            LogoUrl = logoUrl;
            MarkAsUpdated();
        }

        public void Deactivate()
        {
            IsActive = false;
            MarkAsUpdated();
        }
    }
}
