namespace task_14.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public string? QuickBooksBillId { get; set; }
        public string? SyncToken { get; set; }
        public DateTime? TxnDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal Balance { get; set; }
        public string? PrivateNote { get; set; }
        public string? CurrencyValue { get; set; }
        public string? CurrencyName { get; set; }
        public string? VendorId { get; set; }      // assuming nullable
        public string? APAccountId { get; set; }   // assuming nullable
        public DateTime? CreateTime { get; set; }
        public DateTime? LastUpdatedTime { get; set; }

        public ICollection<BillLine> BillLines { get; set; } = new List<BillLine>();
    }


    public class BillLine
    {
        public int Id { get; set; }
        public string QuickBooksLineId { get; set; }
        public int BillId { get; set; }                    
        public int LineNum { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string DetailType { get; set; }              // ItemBased / AccountBased
        public string AccountId { get; set; }               // QuickBooks Account Id
        public int CustomerId { get; set; }              // QuickBooks Customer Id
        public string BillableStatus { get; set; }

        // Optional fields for ItemBasedExpenseLineDetail
        public string ItemId { get; set; }                  // QuickBooks Product Id
        public int? Qty { get; set; }
        public decimal? UnitPrice { get; set; }

        // Navigation
        public Bill Bill { get; set; }
    }

    public class QuickBooksBillDto
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public DateTime? TxnDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal Balance { get; set; }
        public string PrivateNote { get; set; }
        public Currency Currency { get; set; }
        public VendorRef VendorRef { get; set; }
        public APAccountRef APAccountRef { get; set; }
        public MetaData MetaData { get; set; }
        public List<QuickBooksLineDto> Line { get; set; }
    }
    public class VendorRef { public string Value { get; set; } public string Name { get; set; } }
    public class APAccountRef { public string Value { get; set; } public string Name { get; set; } }
    public class MetaData { public int Id { get; set; } public DateTime? CreateTime { get; set; } public DateTime? LastUpdatedTime { get; set; } }


    public class QuickBooksLineDto
    {
        public string Id { get; set; }
        public int? LineNum { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string DetailType { get; set; }
        public ItemDetail ItemBasedExpenseLineDetail { get; set; }
        public AccountDetail AccountBasedExpenseLineDetail { get; set; }
    }

    public class ItemDetail
    {
        public string BillableStatus { get; set; }
        public Ref ItemRef { get; set; }
        public int? Qty { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class Currency
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }


    public class AccountDetail
    {
        public Ref AccountRef { get; set; }

        public Ref CustomerRef { get; set; }
    }
    public class Ref
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class QuickBooksBillCreateDto
    {
        public ReferenceDto? VendorRef { get; set; }                  // Vendor Reference (ID)
        public ReferenceDto? APAccountRef { get; set; }                // Accounts Payable Account Reference (ID)
        public DateTime? TxnDate { get; set; }                         // Transaction Date
        public DateTime? DueDate { get; set; }                         // Due Date
        public string? PrivateNote { get; set; }                        // Private Note
        public List<QuickBooksBillLineDto>? Line { get; set; }         // List of Line items
        public decimal? TotalAmt { get; set; }                          // Total Amount
        public ReferenceDto? CurrencyRef { get; set; }                 // Currency Reference (e.g. USD)
    }

    public class ReferenceDto
    {
        public string? Value { get; set; }                              // Reference Value (e.g. Vendor ID, Account ID)
    }

    public class QuickBooksBillLineDto
    {
        public decimal? Amount { get; set; }                            // Amount for the line item
        public string? DetailType { get; set; }                         // Detail Type (e.g. "AccountBasedExpenseLineDetail")
        public string? Description { get; set; }                        // Description of the line item

        public AccountBasedExpenseLineDetailDto? AccountBasedExpenseLineDetail { get; set; } // Account-based details
        public ItemBasedExpenseLineDetailDto? ItemBasedExpenseLineDetail { get; set; }         // Item-based details
    }

    public class AccountBasedExpenseLineDetailDto
    {
        public ReferenceDto? AccountRef { get; set; }                  // Account Reference (ID)
        public ReferenceDto? CustomerRef { get; set; }                 // Optional Customer Reference (ID)
    }

    public class ItemBasedExpenseLineDetailDto
    {
        public ReferenceDto? ItemRef { get; set; }                     // Item Reference (ID)
        public decimal? Qty { get; set; }                               // Quantity for the item
        public decimal? UnitPrice { get; set; }                         // Unit Price of the item
        public ReferenceDto? CustomerRef { get; set; }                 // Optional Customer Reference (ID)
        public string? BillableStatus { get; set; }                     // Billable status for the item (optional)
    }




}
