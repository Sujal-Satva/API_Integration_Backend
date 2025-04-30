using System.ComponentModel.DataAnnotations;

namespace task_14.Models
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
    }

}
