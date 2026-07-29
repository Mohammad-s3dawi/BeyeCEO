using BeyeCEO.Domain.Shared;
using System;

namespace BeyeCEO.Domain.MarketData.Entities
{
    public class CentralBankCircular : BaseEntity
    {
        public string CountryCode { get; private set; } = string.Empty;
        public string? TitleAR { get; private set; }
        public string? TitleEN { get; private set; }
        public string? CircularNumber { get; private set; }
        public DateOnly? CircularDate { get; private set; }
        public string? PdfUrl { get; private set; }
        public string? FileSize { get; private set; }

        private CentralBankCircular() { }

        public static CentralBankCircular Create(
            string countryCode, string? titleAR, string? titleEN,
            string? circularNumber, DateOnly? circularDate,
            string? pdfUrl, string? fileSize)
        {
            if (countryCode.Length > 5)
                throw new ArgumentException("CountryCode must be at most 5 characters");

            return new CentralBankCircular
            {
                CountryCode = countryCode.ToUpper(),
                TitleAR = titleAR,
                TitleEN = titleEN,
                CircularNumber = circularNumber,
                CircularDate = circularDate,
                PdfUrl = pdfUrl,
                FileSize = fileSize
            };
        }
    }
}
