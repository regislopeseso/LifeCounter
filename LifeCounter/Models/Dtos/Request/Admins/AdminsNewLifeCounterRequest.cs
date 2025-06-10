namespace LifeCounterAPI.Models.Dtos.Request.Admin
{
    public class AdminsNewLifeCounterRequest
    {
        public string? GameName {  get; set; }
        public int? PlayersStartingLife { get; set; }
        public bool? FixedMaxLife { get; set; }
        public bool? AutoEndMatch { get; set; }
    }
}
