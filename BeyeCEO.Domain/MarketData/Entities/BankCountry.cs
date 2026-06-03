using BeyeCEO.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class BankCountry:BaseEntity
    {
        public Guid BankId { get; private set; }
        public string CountryCode { get; private set; } = string.Empty;
        public string LocalBankNameEN { get; private set; } = string.Empty;
        public string LocalBankNameAR { get; private set; } = string.Empty;
        public bool IsHeadquarters { get; private set; } = false;
        public bool IsActive { get; private set; } = true;

        // Navigation
        public Bank Bank { get; private set; } = null!;
        public Country Country { get; private set; } = null!;

        private BankCountry() { }

        public static BankCountry Create(Guid bankId, string countryCode,
            string localNameEN = "", string localNameAR = "",
            bool isHeadquarters = false)
        {
            if (bankId == Guid.Empty)
                throw new ArgumentException("BankId is required");

            if (countryCode.Length != 2)
                throw new ArgumentException("CountryCode must be 2 characters");

            return new BankCountry
            {
                BankId = bankId,
                CountryCode = countryCode.ToUpper(),
                LocalBankNameEN = localNameEN,
                LocalBankNameAR = localNameAR,
                IsHeadquarters = isHeadquarters,
                IsActive = true
            };
        }
    }
}
