using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Shared.ValueObjects
{
    public record Percentage
    {
        public decimal Value { get; init; }

        public Percentage(decimal value)
        {
            if (value < -999 || value > 999)
                throw new ArgumentException("Percentage out of valid range");
            Value = value;
        }

        public bool IsPositive => Value > 0;
        public bool IsNegative => Value < 0;
        public string Format() => $"{Value:+0.00;-0.00;0.00}%";
        public override string ToString() => Format();
    }
}
