using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OLS.Casy.Core.Logging.SQLite.EF.Entities
{
    [Table("Log")]
    public class LogEntity
    {
        [Key]
        public int LogId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Level { get; set; }

        [Required]
        public string Logger { get; set; }

        public string Message { get; set; }

        public string User { get; set; }
        public int Category { get; set; }
    }
}
