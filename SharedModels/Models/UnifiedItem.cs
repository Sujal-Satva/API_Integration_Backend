namespace SharedModels.Models
{
    public class UnifiedItem
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        public string? PurchaseDescription { get; set; }
        public decimal? SalesUnitPrice { get; set; }
        public decimal? PurchaseUnitPrice { get; set; }
        public string? IncomeAccountName { get; set; }
        public string? ExpenseAccountName { get; set; }
        public string? AssetAccountName { get; set; }
        public bool? IsTrackedAsInventory { get; set; }
        public decimal? QuantityOnHand { get; set; }
        public bool IsActive { get; set; }
        public string SourceSystem { get; set; }
        public DateTime CreatedAt { get; set; } // Record creation timestamp
        public DateTime UpdatedAt { get; set; } // Record update timestamp
    }
}