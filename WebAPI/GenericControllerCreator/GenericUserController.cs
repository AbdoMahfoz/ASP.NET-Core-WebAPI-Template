using System.Collections.Generic;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Models.DataModels;
using Models.GenericControllerDTOs;
using Services.Extensions;

namespace WebAPI.GenericControllerCreator;

[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
[GenericControllerName]
[Authorize]
public class GenericUserController<T, TDIn, TDOut>(IUserGenericLogic<T, TDIn, TDOut> logic) : ControllerBase
    where T : BaseUserModel, new()
    where TDOut : BaseDto, new()
{
    public static string ModelName = "";

    /// <summary>
    ///     Returns all the ids of the instances of The Model.
    /// </summary>
    /// <remarks>
    ///     You can provide additional ids as a json object, which will be used to filter resultant objects <br/>
    ///     This is useful when, for an example, you want to get all objects that belong to one specific object <br/>
    ///     That it's ids is known beforehand.
    /// </remarks>
    [HttpPost("GetAll")]
    public IEnumerable<TDOut> GetAll(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        IDictionary<string, object> relationalIds = null)
    {
        var res = logic.GetAll(User.GetId(), relationalIds);
        if (res == null) throw new BadHttpRequestException("relationalIds malformed");
        return res;
    }

    /// <summary>
    ///     Returns the instance of The Model with the sent Id.
    /// </summary>
    /// <param name="id">Instance Id</param>
    /// <response code="200">The Instance with the Id</response>
    [HttpGet("{id}")]
    public TDOut Get(int id)
    {
        return logic.Get(User.GetId(), id);
    }


    /// <summary>
    ///     Inserts an instance of The Model into the Database
    /// </summary>
    /// <remarks>
    ///     NO nulls are accepted
    /// </remarks>
    /// <param name="instance">The instance</param>
    /// <response code="201">instance Inserted</response>
    /// <response code="400">Bad Request</response>
    [ProducesResponseType(201, Type = typeof(int))]
    [ProducesResponseType(400, Type = typeof(string))]
    [HttpPost("Insert")]
    public IActionResult Insert([FromBody] TDIn instance)
    {
        if (instance == null) return BadRequest("The inserted instance is null");
        var returnedId = logic.Insert(User.GetId(), instance);
        return StatusCode(StatusCodes.Status201Created, returnedId);
    }


    /// <summary>
    ///     Inserts a List of instances of The Model into the Database
    /// </summary>
    /// <remarks>
    ///     NO nulls are accepted
    /// </remarks>
    /// <param name="instances">List of instances</param>
    /// <response code="201">Entities Inserted</response>
    [ProducesResponseType(201, Type = typeof(IEnumerable<int>))]
    [HttpPost("InsertRange")]
    public IActionResult InsertRange([FromBody] IEnumerable<TDIn> instances)
    {
        return StatusCode(StatusCodes.Status201Created, logic.InsertRange(User.GetId(), instances));
    }

    /// <summary>
    ///     Updates an instance of The Model from the Database
    /// </summary>
    /// <remarks>
    ///     NO nulls are accepted
    /// </remarks>
    /// <response code="202">Instance updated</response>
    [ProducesResponseType(202, Type = null)]
    [HttpPut("Update")]
    public IActionResult Update([FromQuery] int Id, [FromBody] TDIn instance)
    {
        if (instance == null) return BadRequest();
        logic.Update(User.GetId(), Id, instance);
        return StatusCode(StatusCodes.Status202Accepted);
    }

    /// <summary>
    ///     Deletes an on The Model from the Database
    /// </summary>
    /// <remarks>
    ///     Depends on the id only
    /// </remarks>
    /// <response code="202">instance Deleted</response>
    [ProducesResponseType(202, Type = null)]
    [HttpDelete("Delete")]
    public IActionResult Delete(int Id, bool IsHard)
    {
        if (IsHard) logic.HardDelete(User.GetId(), Id);
        else logic.SoftDelete(User.GetId(), Id);
        return StatusCode(StatusCodes.Status202Accepted);
    }

    /// <summary>
    ///     Deletes a List of instances from the Database
    /// </summary>
    /// <param name="ids">The instances' ids</param>
    /// <response code="202">instances Deleted</response>
    [ProducesResponseType(202, Type = null)]
    [HttpDelete("DeleteRange")]
    public IActionResult DeleteEnablers([FromBody] IEnumerable<int> ids)
    {
        if (ids == null) return BadRequest();
        logic.SoftDeleteRange(User.GetId(), ids);
        return StatusCode(StatusCodes.Status202Accepted);
    }
}