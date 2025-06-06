﻿using System.ComponentModel.DataAnnotations;
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

        public required int PlayersStartingLife { get; set; }

        public required bool FixedMaxLife { get; set; }
        
        public required bool AutoEndMatch { get; set; }

        public List<Match>? Matches { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
