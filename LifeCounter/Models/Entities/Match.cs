using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeCounterAPI.Models.Entities
{
    [Table("matches")]
    public class Match
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("Game")]
        public int GameId { get; set; }
        // Aqui talvez será necessário acrescentar o conceito de inverse property.
        // Razão: ser possível acessar um objeto match a partir de Game
        // [InverseProperty("Match")]
        public Game Game {  get; set; }

        public required List<Player> Players {  get; set; }

        public DateTime StartingTime { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime? EndingTime { get; set; }
        public TimeSpan Duration {  get; set; } = TimeSpan.Zero;
        public int test {  get; set; }
        public bool IsFinished { get; set; } = false;
    }
}
