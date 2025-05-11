using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Models
{
    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<T> Data { get; set; }

        public PagedResponse(IEnumerable<T> data, int page, int pageSize, int totalRecords)
        {
            Data = data;
            Page = page;
            PageSize = pageSize;
            TotalRecords = totalRecords;
            TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;
        }
    }
}
