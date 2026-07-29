using System;
using System.Collections.Generic;

namespace BeyeCEO.Application.MarketData.DTOs
{
    public class CircularsListDto
    {
        public IEnumerable<CircularDto> Circulars { get; set; } = [];
        public int Total { get; set; }
    }

    public class CircularDto
    {
        public Guid Id { get; set; }
        public string? TitleAR { get; set; }
        public string? TitleEN { get; set; }
        public string? CircularNumber { get; set; }
        public DateOnly? CircularDate { get; set; }
        public string? PdfUrl { get; set; }
        public string? FileSize { get; set; }
    }
}
