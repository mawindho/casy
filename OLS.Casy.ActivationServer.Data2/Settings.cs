using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OLS.Casy.ActivationServer.Data
{
    [Table("Settings")]
    public class Settings
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SettingsKey { get; set; }

        [StringLength(200)]
        public string SettingsValue { get; set; }
    }
}
