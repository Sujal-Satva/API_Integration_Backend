using Microsoft.EntityFrameworkCore;
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

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<InvoiceLineItem> InvoiceLineItem { get; set; }

        public DbSet<Vendor> Vendors { get; set; }

        public DbSet<Bill> Bills { get; set; }

        public DbSet<BillLine> BillLines { get; set; }

        public DbSet<ConnectionModal> Connections { get; set; }

        public DbSet<AllCustomer> AllCustomers { get; set; }



    }
}
