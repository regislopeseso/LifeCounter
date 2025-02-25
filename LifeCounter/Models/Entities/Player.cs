using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeCounterAPI.Models.Entities
{
    [Table("players")]
    public class Player
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int StartingLife { get; set; }

        public int CurrentLife { get; set; }

        [ForeignKey("Match")]
        public int MatchId { get; set; }
        public Match? Match { get; set; }

        public bool FixedMaxLife { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
