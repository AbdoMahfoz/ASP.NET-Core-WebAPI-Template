using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class NameController : ControllerBase
    {
        /// <summary>
        /// Returns the name of the currently logged in user
        /// </summary>
        /// <response code="200">The name of the current logged in user</response>
        [ProducesResponseType(200, Type = typeof(string))]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(User.Identity.Name);
        }
    }
}
