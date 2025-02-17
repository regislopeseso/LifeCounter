using LifeCounterAPI.Services;
using LifeCounterAPI.Utilities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeCounterAPI.Models.Entities
{
    [Table("Games")]
    public class Game
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }
        public int LifeTotal { get; set; } = Constants.DefaultLifeTotal;
        public List<Player>? Players { get; set; }
    }
}
