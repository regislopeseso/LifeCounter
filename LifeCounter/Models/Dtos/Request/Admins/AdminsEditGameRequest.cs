namespace LifeCounterAPI.Models.Dtos.Request.Admin
{
    public class AdminsEditGameRequest
    {
        public int GameId { get; set; }

        public string? GameName {  get; set; }
        public int? LifeTotal { get; set; }
        public bool? FixedMaxLife { get; set; }
        public bool? AutoEndBattle { get; set; }
    }
}
