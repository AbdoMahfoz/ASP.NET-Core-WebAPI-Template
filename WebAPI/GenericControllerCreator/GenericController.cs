using System.Collections.Generic;
using System.Linq;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.DataModels;

namespace WebAPI.GenericControllerCreator
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    [GenericControllerNameAttribute]
    public class GenericController<T> : ControllerBase where T : BaseModel
    {
        private readonly IGenericLogic<T> _genericLogic;

        public GenericController(IGenericLogic<T> logic)
        {
            _genericLogic = logic;
        }

        /// <summary>
        ///     Returns all the ids of the instances of The Model.
        /// </summary>
        /// <response code="200">All Instances Ids</response>
        /// <response code="401">UnAuthorized Access</response>
        [ProducesResponseType(200, Type = typeof(IEnumerable<int>))]
        [ProducesResponseType(401, Type = typeof(string))]
        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            return StatusCode(StatusCodes.Status200OK, _genericLogic.GetAll().ToList().Select(x => x.Id));
        }

        /// <summary>
        ///     Returns the instance of The Model with the sent Id.
        /// </summary>
        /// <param name="id">Instance Id</param>
        /// <response code="200">The Instance with the Id</response>
        /// <response code="401">UnAuthorized Access</response>
        [ProducesResponseType(403, Type = typeof(string))]
        [ProducesResponseType(400, Type = typeof(string))]
        [ProducesResponseType(401, Type = typeof(string))]
        [HttpGet("{id}")]
        public T Get(int id)
        {
            return _genericLogic.Get(id);
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
        /// <response code="401">UnAuthorized Access</response>
        /// <response code="403">Invalid Data</response>
        [ProducesResponseType(201, Type = typeof(int))]
        [ProducesResponseType(400, Type = typeof(string))]
        [ProducesResponseType(401, Type = typeof(string))]
        [ProducesResponseType(403, Type = typeof(string))]
        [HttpPost("Insert")]
        public IActionResult Insert([FromBody] T instance)
        {
            if (instance == null) return BadRequest("The inserted instance is null");
            var returnedEntity = _genericLogic.Insert(instance);
            return StatusCode(StatusCodes.Status201Created, returnedEntity.Id);
        }


        /// <summary>
        ///     Inserts a List of instances of The Model into the Database
        /// </summary>
        /// <remarks>
        ///     NO nulls are accepted
        /// </remarks>
        /// <param name="instances">List of instances</param>
        /// <response code="201">Entities Inserted</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">UnAuthorized Access</response>
        /// <response code="403">Invalid Data</response>
        [ProducesResponseType(201, Type = typeof(IEnumerable<int>))]
        [ProducesResponseType(400, Type = typeof(string))]
        [ProducesResponseType(401, Type = typeof(string))]
        [ProducesResponseType(403, Type = typeof(string))]
        [HttpPost("InsertRange")]
        public IActionResult InsertRange([FromBody] IEnumerable<T> instances)
        {
            foreach (var enabler in instances)
                if (enabler == null)
                    return BadRequest();
            _genericLogic.InsertRange(instances).Wait();
            var ids = instances.Select(x => x.Id);
            return StatusCode(StatusCodes.Status201Created, ids);
        }

        /// <summary>
        ///     Updates an instance of The Model from the Database
        /// </summary>
        /// <remarks>
        ///     NO nulls are accepted
        /// </remarks>
        /// <param name="instance">The instance</param>
        /// <response code="202">Instance updated</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">UnAuthorized Access</response>
        /// <response code="403">Invalid Data</response>
        [ProducesResponseType(202, Type = typeof(int))]
        [ProducesResponseType(400, Type = typeof(string))]
        [ProducesResponseType(401, Type = typeof(string))]
        [ProducesResponseType(403, Type = typeof(string))]
        [HttpPut("Update")]
        public IActionResult Update([FromBody] T instance)
        {
            if (instance == null) return BadRequest();
            _genericLogic.Update(instance);
            return StatusCode(StatusCodes.Status202Accepted, instance.Id);
        }

        /// <summary>
        ///     Updates a List of instances of The Model into the Database
        /// </summary>
        /// <remarks>
        ///     NO nulls are accepted
        /// </remarks>
        /// <param name="instances">The instance</param>
        /// <response code="202">Instances updated</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">UnAuthorized Access</response>
        /// <response code="403">Invalid Data</response>
        [ProducesResponseType(202, Type = null)]
        [ProducesResponseType(400, Type = typeof(string))]
        [ProducesResponseType(401, Type = typeof(string))]
        [ProducesResponseType(403, Type = typeof(string))]
        [HttpPut("UpdateRange")]
        public IActionResult UpdateRange([FromBody] IEnumerable<T> instances)
        {
            foreach (var instance in instances)
                if (instance == null)
                    return BadRequest();
            _genericLogic.UpdateRange(instances.ToList());
            return StatusCode(StatusCodes.Status202Accepted);
        }

        /// <summary>
        ///     Deletes an on The Model from the Database
        /// </summary>
        /// <remarks>
        ///     Depends on the id only
        /// </remarks>
        /// <param name="Id">The instance id</param>
        /// <response code="202">instance Deleted</response>
        /// <response code="401">UnAuthorized Access</response>
        /// <response code="403">Invalid Data</response>
        /// <response code="403">instance Not Found</response>
        [ProducesResponseType(202, Type = null)]
        [ProducesResponseType(401, Type = typeof(string))]
        [ProducesResponseType(403, Type = typeof(string))]
        [ProducesResponseType(404, Type = typeof(string))]
        [HttpDelete("Delete")]
        public IActionResult Delete(int Id)
        {
            _genericLogic.SoftDelete(Id);
            return StatusCode(StatusCodes.Status202Accepted);
        }

        /// <summary>
        ///     Deletes a List of instances from the Database
        /// </summary>
        /// <param name="ids">The instances' ids</param>
        /// <response code="202">instances Deleted</response>
        /// <response code="401">UnAuthorized Access</response>
        /// <response code="403">Invalid Data</response>
        /// <response code="403">Enablers Not Found</response>
        [ProducesResponseType(202, Type = null)]
        [ProducesResponseType(401, Type = typeof(string))]
        [ProducesResponseType(403, Type = typeof(string))]
        [ProducesResponseType(404, Type = typeof(string))]
        [HttpDelete("DeleteRange")]
        public IActionResult DeleteEnablers([FromBody] IEnumerable<int> ids)
        {
            if (ids == null) return BadRequest();
            _genericLogic.SoftDeleteRange(ids);
            return StatusCode(StatusCodes.Status202Accepted);
        }
    }
}