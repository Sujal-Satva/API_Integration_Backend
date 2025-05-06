using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using task_14.Models;

namespace task_14.Data
{
    public class ApplicationDbContext:DbContext

    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<QuickBooksToken> QuickBooksTokens { get; set; }

        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Vendor> Vendors { get; set; }

        public DbSet<Bill> Bills { get; set; }
            
        public DbSet<BillLine> BillLines { get; set; }

        public DbSet<ConnectionModal> Connections { get; set; }

        public DbSet<UnifiedCustomer> UnifiedCustomers { get; set; }

        public DbSet<UnifiedItem> UnifiedItems { get; set; }

        public DbSet<UnifiedInvoice> UnifiedInvoices { get; set; }
    }
}
