using LifeCounterAPI.Utilities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeCounterAPI.Models.Entities
{
    [Table("LifeCounters")]
    public class LifeCounter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string GameName { get; set; }
        public int LifeTotal { get; set; } = Constants.DefaultLifeTotal;
    }
}
