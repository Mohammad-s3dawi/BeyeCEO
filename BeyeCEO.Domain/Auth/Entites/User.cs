using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.Shared;
using BeyeCEO.Domain.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Domain.Auth.Entites
{
    public class User : BaseEntity
    {
        public Guid BankId { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string FullNameEN { get; private set; } = string.Empty;
        public string FullNameAR { get; private set; } = string.Empty;
        public string? PasswordHash { get; private set; }
        public AuthType AuthType { get; private set; } = AuthType.Local;
        public string? WindowsSid { get; private set; }
        public UserRole Role { get; private set; } = UserRole.CEO;
        public string? DefaultCountryCode { get; private set; }
        public Language PreferredLanguage { get; private set; } = Language.EN;
        public bool IsActive { get; private set; } = true;
        public DateTime? LastLoginAt { get; private set; }

        // Navigation ← جديد
        public Bank Bank { get; private set; } = null!;
        public Country? DefaultCountry { get; private set; }   // ← nullable

        private User() { }

        // Factory للـ Local Auth
        public static User CreateLocal(Guid bankId, string email,
            string fullNameEN, string fullNameAR,
            string passwordHash, UserRole role = UserRole.CEO,
            string? defaultCountryCode = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required for Local auth");

            return new User
            {
                BankId = bankId,
                Email = email.ToLower().Trim(),
                FullNameEN = fullNameEN,
                FullNameAR = fullNameAR,
                PasswordHash = passwordHash,
                AuthType = AuthType.Local,
                Role = role,
                DefaultCountryCode = defaultCountryCode?.ToUpper(),
                IsActive = true
            };
        }

        // Factory للـ Windows Auth
        public static User CreateWindows(Guid bankId, string email,
            string fullNameEN, string fullNameAR,
            string windowsSid, UserRole role = UserRole.CEO,
            string? defaultCountryCode = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(windowsSid))
                throw new ArgumentException("WindowsSid is required for Windows auth");

            return new User
            {
                BankId = bankId,
                Email = email.ToLower().Trim(),
                FullNameEN = fullNameEN,
                FullNameAR = fullNameAR,
                WindowsSid = windowsSid,
                AuthType = AuthType.Windows,
                Role = role,
                DefaultCountryCode = defaultCountryCode?.ToUpper(),
                IsActive = true
            };
        }

        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public void SetLanguage(Language language)
        {
            PreferredLanguage = language;
            MarkAsUpdated();
        }

        public void SetDefaultCountry(string countryCode)
        {
            if (countryCode.Length != 2)
                throw new ArgumentException("CountryCode must be 2 characters");

            DefaultCountryCode = countryCode.ToUpper();
            MarkAsUpdated();
        }

        public void Deactivate()
        {
            IsActive = false;
            MarkAsUpdated();
        }

        public void UpdatePassword(string newPasswordHash)
        {
            if (AuthType != AuthType.Local)
                throw new InvalidOperationException("Cannot set password for Windows auth user");

            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("Password hash is required");

            PasswordHash = newPasswordHash;
            MarkAsUpdated();
        }
    }
}
