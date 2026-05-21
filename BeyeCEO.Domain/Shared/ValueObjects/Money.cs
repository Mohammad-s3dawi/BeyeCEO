using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Shared.ValueObjects
{
    public  record Money
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; }

        public Money(decimal amount, string currency)
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative");
            if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required");
            Amount = amount;
            Currency = currency.ToUpper();
        }

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");
            return new Money(Amount + other.Amount, Currency);
        }

        public override string ToString() => $"{Amount:N2} {Currency}";
    }
}
