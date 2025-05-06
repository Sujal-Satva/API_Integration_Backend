using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Models
{
    public class ConnectionModal
    {
        [Key]
        public int Id { get; set; }
        public string SourceAccounting { get; set; }

        public string ExternalId { get; set; }

        public string ExternalName { get; set; }

        public string TokenJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastModifiedDate { get; set; } = DateTime.UtcNow;

        public string? SyncingInfo { get; set; }
    }
}
