using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.DTOs;
using System.Collections.Generic;

namespace WebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        /// <summary>
        /// Returns the name of the currently logged in user
        /// </summary>
        /// <response code="200">The name of the current logged in user</response>
        [ProducesResponseType(200, Type = typeof(string))]
        [HttpGet("Name")]
        public IActionResult Get()
        {
            return Ok(User.Identity.Name);
        }
        /// <summary>
        /// Returns all the possible values for any enum
        /// </summary>
        /// <response code="404">Enum not found</response>
        /// <response code="200">Enum found, possible values returned</response>
        [ProducesResponseType(200, Type = typeof(IEnumerable<EnumResult>))]
        [ProducesResponseType(404, Type = null)]
        [AllowAnonymous]
        [HttpGet("Enum")]
        public IActionResult GetEnumValues([FromQuery]string EnumName)
        {
            var res = Helpers.GetEnumOptions(EnumName);
            if (res == null)
                return NotFound();
            return Ok(res);
        }
    }
}
