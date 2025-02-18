using LifeCounterAPI.Models.Dtos.Request.Players;
using LifeCounterAPI.Models.Dtos.Response;
using LifeCounterAPI.Models.Dtos.Response.Players;
using LifeCounterAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LifeCounterAPI.Controllers
{
    [ApiController]
    [Route("players/[action]")]
    public class PlayersController : ControllerBase
    {
        private readonly PlayersService _playersService;

        public PlayersController(PlayersService playersService)
        {
            this._playersService = playersService;
        }

        [HttpPost]
        public async Task<IActionResult> StartGame(PlayersStartGameRequest request)
        {
            var (content, message) = await this._playersService.StartGame(request);

            var response = new Response<List<PlayersStartGameResponse>>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> IncreaseLifeTotal(PlayersIncreaseLifeTotalRequest request)
        {
            var (content, message) = await this._playersService.IncreaseLifeTotal(request);

            var response = new Response<PlayersIncreaseLifeTotalResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> DecreaseLifeTotal(PlayersDecreaseLifeTotalRequest request)
        {
            var (content, message) = await this._playersService.DecreaseLifeTotal(request);

            var response = new Response<PlayersDecreaseLifeTotalResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> SetLifeTotal(PlayersSetLifeTotalRequest request)
        {
            var (content, message) = await this._playersService.SetLifeTotal(request);

            var response = new Response<PlayersSetLifeTotalResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }
    }
}
