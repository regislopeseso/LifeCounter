using LifeCounterAPI.Models.Dtos.Request.Players;
using LifeCounterAPI.Models.Dtos.Response;
using LifeCounterAPI.Models.Dtos.Response.Players;
using LifeCounterAPI.Services;
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
        public async Task<IActionResult> NewMatch(PlayersNewMatchRequest request)
        {
            var (content, message) = await this._playersService.NewMatch(request);

            var response = new Response<PlayersNewMatchResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> IncreaseLife(PlayersIncreaseLifeRequest request)
        {
            var (content, message) = await this._playersService.IncreaseLife(request);

            var response = new Response<PlayersIncreaseLifeResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> DecreaseLife(PlayersDecreaseLifeRequest request)
        {
            var (content, message) = await this._playersService.DecreaseLife(request);

            var response = new Response<PlayersDecreaseLifeResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> SetLife(PlayersSetLifeRequest request)
        {
            var (content, message) = await this._playersService.SetLife(request);

            var response = new Response<PlayersSetLifeResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> ResetLife(PlayersResetLifeRequest request)
        {
            var (content, message) = await this._playersService.ResetLife(request);

            var response = new Response<PlayersResetLifeResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpGet]
        public async Task<IActionResult> ShowMatchStatus(PlayersShowMatchStatusRequest request)
        {
            var (content, message) = await this._playersService.ShowMatchStatus(request);

            var response = new Response<PlayersShowMatchStatusResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpDelete]
        public async Task<IActionResult> EndMatch(PlayersEndMatchRequest request)
        {
            var (content, message) = await this._playersService.EndMatch(request);

            var response = new Response<PlayersEndMatchResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpGet]
        public async Task<IActionResult> ShowStats(PlayersShowStatsRequest request)
        {
            var (content, message) = await this._playersService.ShowStats(request);

            var response = new Response<PlayersShowStatsResponse>()
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }


    }
}
