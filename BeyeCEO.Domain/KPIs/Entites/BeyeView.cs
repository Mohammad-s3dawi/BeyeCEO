using BeyeCEO.Domain.Shared;
using System;

namespace BeyeCEO.Domain.KPIs.Entites
{
    public class BeyeView : BaseEntity
    {
        public Guid BankId { get; private set; }
        public int ViewId { get; private set; }
        public string ViewKeyName { get; private set; } = string.Empty;
        public string Section { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;
        public int SortOrder { get; private set; }

        private BeyeView() { }

        public static BeyeView Create(
            Guid bankId, int viewId, string viewKeyName,
            string section, int sortOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(viewKeyName))
                throw new ArgumentException("ViewKeyName is required");

            if (string.IsNullOrWhiteSpace(section))
                throw new ArgumentException("Section is required");

            return new BeyeView
            {
                BankId = bankId,
                ViewId = viewId,
                ViewKeyName = viewKeyName,
                Section = section,
                IsActive = true,
                SortOrder = sortOrder
            };
        }
    }
}
