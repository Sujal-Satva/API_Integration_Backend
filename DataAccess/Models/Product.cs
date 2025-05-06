namespace task_14.Models
{
    public class Product
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public string SyncToken { get; set; }
        public string QBItemId { get; set; }
        public string FullyQualifiedName { get; set; }
        public string Type { get; set; }
        public decimal UnitPrice { get; set; }
        public bool Taxable { get; set; }
        public string? IncomeAccountName { get; set; }
        public string? IncomeAccountValue { get; set; }
        public string? ExpenseAccountName { get; set; }
        public string? ExpenseAccountValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    
}
