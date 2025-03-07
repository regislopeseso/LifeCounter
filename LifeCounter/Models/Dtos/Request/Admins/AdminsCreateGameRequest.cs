namespace LifeCounterAPI.Models.Dtos.Request.Admin
{
    public class AdminsCreateGameRequest
    {
        public string? GameName {  get; set; }
        public int? MaxLife { get; set; }
        public bool? FixedMaxLife { get; set; }
        public bool? AutoEndMatch { get; set; }
    }
}
