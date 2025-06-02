using LifeCounterAPI.Models.Dtos.Request.Admin;
using LifeCounterAPI.Models.Dtos.Response;
using LifeCounterAPI.Models.Dtos.Response.Admin;
using LifeCounterAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LifeCounterAPI.Controllers
{
    [ApiController]
    [Route("admins/[action]")]
    public class AdminsController : ControllerBase
    {
        private readonly AdminsService _adminsService;

        public AdminsController(AdminsService adminsService)
        {
            this._adminsService = adminsService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGame([FromForm] AdminsCreateGameRequest? request)
        {
            var (content, message) = await this._adminsService.CreateGame(request);

            var response = new Response<AdminsCreateGameResponse>
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> EditGame(AdminsEditGameRequest? request)
        {
            var (content, message) = await this._adminsService.EditGame(request);

            var response = new Response<AdminsEditGameResponse>
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteGame(AdminsDeleteGameRequest? request)
        {
            var (content, message) = await this._adminsService.DeleteGame(request);

            var response = new Response<AdminsDeleteGameResponse>
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }
    }
}
