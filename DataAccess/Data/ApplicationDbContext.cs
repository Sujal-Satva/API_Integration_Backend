using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using task_14.Models;

namespace task_14.Data
{
    public class ApplicationDbContext:DbContext

    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }

        public DbSet<ConnectionModal> Connections { get; set; }

        public DbSet<UnifiedCustomer> UnifiedCustomers { get; set; }

        public DbSet<UnifiedItem> UnifiedItems { get; set; }

        public DbSet<UnifiedInvoice> UnifiedInvoices { get; set; }

        public DbSet<UnifiedBill> UnifiedBills { get; set; }

        public DbSet<UnifiedVendor> UnifiedVendors { get; set; }


    }
}
