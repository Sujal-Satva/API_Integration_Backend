using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickBookService.Interfaces
{
    public interface IQuickBooksBillService
    {
        Task<CommonResponse<object>> FetchBillsFromQuickBooks(ConnectionModal connection);

        Task<CommonResponse<object>> AddBillToQuickBooks(CreateBillRequest billDto, ConnectionModal connection);
    }
}
