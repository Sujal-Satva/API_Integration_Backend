﻿using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using task_14.Data;

namespace DataAccess.Helper
{
    public class SyncingFunction
    {
        private readonly ApplicationDbContext _context;
        public SyncingFunction(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateSyncingInfo(string realmId, string key, DateTime timestamp)
        {
            var connection = await _context.Connections.FirstOrDefaultAsync(c => c.ExternalId == realmId);
            if (connection == null) return;

            var dict = string.IsNullOrEmpty(connection.SyncingInfo)
                ? new Dictionary<string, string>()
                : JsonConvert.DeserializeObject<Dictionary<string, string>>(connection.SyncingInfo);

            dict[key] = timestamp.ToUniversalTime().ToString("o");
            connection.SyncingInfo = JsonConvert.SerializeObject(dict);

            await _context.SaveChangesAsync();
        }

        public async Task<CommonResponse<object>> StoreUnifiedItemsAsync(List<UnifiedItem> items)
        {
            var externalIds = items.Select(i => i.ExternalId).ToList();
            var sourceSystem = items.FirstOrDefault()?.SourceSystem;

            var existingItems = await _context.UnifiedItems
                .Where(i => externalIds.Contains(i.ExternalId) && i.SourceSystem == sourceSystem)
                .ToListAsync();

            foreach (var item in items)
            {
                var existing = existingItems.FirstOrDefault(e => e.ExternalId == item.ExternalId);

                if (existing != null)
                {

                    item.Id = existing.Id;
                    _context.Entry(existing).CurrentValues.SetValues(item);
                }
                else
                {
                    _context.UnifiedItems.Add(item);
                }
            }

            await _context.SaveChangesAsync();
            return new CommonResponse<object>(200, "Items upserted successfully", items);
        }

        public async Task<CommonResponse<object>> StoreUnifiedCustomersAsync(List<UnifiedCustomer> customers)
        {
            if (customers == null || !customers.Any())
            {
                return new CommonResponse<object>(400, "Customer list is null or empty", null);
            }

            var externalIds = customers.Select(i => i.ExternalId).ToList();
            var sourceSystem = customers.First().SourceSystem;

            var existingCustomers=await _context.UnifiedCustomers.Where(i => externalIds.Contains(i.ExternalId) && i.SourceSystem == sourceSystem)
                .ToListAsync();
         
            foreach (var item in customers)
            {
                var existing = existingCustomers.FirstOrDefault(e => e.ExternalId == item.ExternalId);

                if (existing != null)
                {
                    item.Id = existing.Id;
                    _context.Entry(existing).CurrentValues.SetValues(item);
                }
                else
                {
                    _context.UnifiedCustomers.Add(item);
                }
            }

            await _context.SaveChangesAsync();
            return new CommonResponse<object>(200, "Items upserted successfully", customers);
        }

        public async Task<CommonResponse<object>> StoreUnifiedInvoicesAsync(List<UnifiedInvoice> invoices)
        {
                if (invoices == null || !invoices.Any())
            {
                return new CommonResponse<object>(400, "Invoice list is null or empty", null);
            }

            var externalIds = invoices.Select(i => i.ExternalId).ToList();
            var sourceSystem = invoices.First().SourceSystem;

            var existingInvoices = await _context.UnifiedInvoices
                .Where(i => externalIds.Contains(i.ExternalId) && i.SourceSystem == sourceSystem)
                .ToListAsync();

            foreach (var item in invoices)
            {
                var existing = existingInvoices.FirstOrDefault(e => e.ExternalId == item.ExternalId);

                if (existing != null)
                {
                    item.Id = existing.Id;
                    _context.Entry(existing).CurrentValues.SetValues(item);
                }
                else
                {
                    _context.UnifiedInvoices.Add(item);
                }
            }

            await _context.SaveChangesAsync();
            return new CommonResponse<object>(200, "Invoices upserted successfully", invoices);
        }


        public async Task<CommonResponse<object>> StoreUnifiedBillsAsync(List<UnifiedBill> bills)
        {
            if (bills == null || !bills.Any())
            {
                return new CommonResponse<object>(400, "Bill list is null or empty", null);
            }

            var externalIds = bills.Select(b => b.ExternalId).ToList();
            var sourceSystem = bills.First().SourceSystem;

            var existingBills = await _context.UnifiedBills
                .Where(b => externalIds.Contains(b.ExternalId) && b.SourceSystem == sourceSystem)
                .ToListAsync();

            
            foreach (var bill in bills)
            {
                var existingBill = existingBills.FirstOrDefault(e => e.ExternalId == bill.ExternalId);

                if (existingBill != null)
                {
                   
                    bill.Id = existingBill.Id;
                    _context.Entry(existingBill).CurrentValues.SetValues(bill);
                }
                else
                {
                   
                    _context.UnifiedBills.Add(bill);
                }
            }
            await _context.SaveChangesAsync();

            return new CommonResponse<object>(200, "Bills upserted successfully", bills);
        }

        public async Task<CommonResponse<object>> StoreUnifiedVendorsAsync(List<UnifiedVendor> vendors)
        {
            if (vendors == null || !vendors.Any())
            {
                return new CommonResponse<object>(400, "Vendor list is null or empty", null);
            }

            var externalIds = vendors.Select(v => v.ExternalId).ToList();
            var sourceSystem = vendors.First().SourceSystem;

            var existingVendors = await _context.UnifiedVendors
                .Where(v => externalIds.Contains(v.ExternalId) && v.SourceSystem == sourceSystem)
                .ToListAsync();

            foreach (var vendor in vendors)
            {
                var existing = existingVendors.FirstOrDefault(e => e.ExternalId == vendor.ExternalId);

                if (existing != null)
                {
                    vendor.Id = existing.Id;
                    _context.Entry(existing).CurrentValues.SetValues(vendor);
                }
                else
                {
                    _context.UnifiedVendors.Add(vendor);
                }
            }

            await _context.SaveChangesAsync();
            return new CommonResponse<object>(200, "Vendors upserted successfully", vendors);
        }



        public async Task<CommonResponse<object>> MarkInvoiceAsDeletedAsync(string id, string status)
        {
            var invoice = await _context.UnifiedInvoices.FirstOrDefaultAsync(item => item.ExternalId == id);

            if (invoice == null)
                return new CommonResponse<object>(404, "Invoice not found");

            invoice.Status = status;
            await _context.SaveChangesAsync();

            return new CommonResponse<object>(200, "Invoice status updated successfully");
        }

    }
}
