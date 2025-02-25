namespace LifeCounterAPI.Models.Dtos.Request.Admin
{
    public class AdminsEditGameRequest
    {
        public int GameId { get; set; }

        public string? GameName {  get; set; }
        public int? StartingLife { get; set; }
        public bool? FixedMaxLife { get; set; }
        public bool? AutoEndMatch { get; set; }
    }
}
