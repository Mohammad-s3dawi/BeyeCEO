using System;
using System.Collections.Generic;

namespace BeyeCEO.Application.MarketData.DTOs
{
    public class BankAdsListDto
    {
        public IEnumerable<BankAdDto> Ads { get; set; } = [];
    }

    public class BankAdDto
    {
        public Guid Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string? BankNameAR { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public DateTime ScrapedAt { get; set; }
    }
}
