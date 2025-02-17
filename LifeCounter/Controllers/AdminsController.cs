using LifeCounterAPI.Models.Dtos.Request.Admin;
using LifeCounterAPI.Models.Dtos.Response;
using LifeCounterAPI.Models.Dtos.Response.Admin;
using LifeCounterAPI.Services;
using Microsoft.AspNetCore.Http;
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
        public async Task<IActionResult> CreateLifeCounter(AdminsCreateLifeCounterRequest request)
        {
            var (content, message) = await this._adminsService.CreateLifeCounter(request);

            var response = new Response<AdminsCreateLifeCounterResponse>
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpPut]
        public async Task<IActionResult> EditLifeCounter(AdminsEditLifeCounterRequest request)
        {
            var (content, message) = await this._adminsService.EditLifeCounter(request);

            var response = new Response<AdminsEditLifeCounterResponse>
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveLifeCounter(AdminsRemoveLifeCounterRequest request)
        {
            var (content, message) = await this._adminsService.RemoveLifeCounter(request);

            var response = new Response<AdminsRemoveLifeCounterResponse>
            {
                Content = content,
                Message = message
            };

            return new JsonResult(response);
        }
    }
}
