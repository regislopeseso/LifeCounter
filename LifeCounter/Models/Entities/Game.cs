using LifeCounterAPI.Services;
using LifeCounterAPI.Utilities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeCounterAPI.Models.Entities
{
    [Table("games")]
    public class Game
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }
        public int LifeTotal { get; set; } = Constants.DefaultLifeTotal;

        // Aqui talvez será necessário acrescentar o conceito de inverse property
        // Razão: ser possível acessar um objeto match a partir de Game
        // [InverseProperty("Game")]
        public List<Match>? Matches { get; set; }
    }
}
