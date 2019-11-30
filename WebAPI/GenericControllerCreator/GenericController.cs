using System.Collections.Generic;
using System.Linq;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.DataModels;
using Models.GenericControllerDTOs;

namespace WebAPI.GenericControllerCreator
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    [GenericControllerName]
    public class GenericController<T, DIn, DOut> : ControllerBase where T : BaseModel, new()
                                                                  where DOut : BaseDTO, new()
    {
        private readonly IGenericLogic<T, DIn, DOut> _genericLogic;

        public GenericController(IGenericLogic<T, DIn, DOut> logic)
        {
            _genericLogic = logic;
        }

        /// <summary>
        ///     Returns all the ids of the instances of The Model.
        /// </summary>
        /// <response code="200">All Instances Ids</response>
        [ProducesResponseType(200, Type = typeof(IEnumerable<int>))]
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
        [HttpGet("{id}")]
        public DOut Get(int id)
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
        [ProducesResponseType(201, Type = typeof(int))]
        [ProducesResponseType(400, Type = typeof(string))]
        [HttpPost("Insert")]
        public IActionResult Insert([FromBody]DIn instance)
        {
            if (instance == null) return BadRequest("The inserted instance is null");
            var returnedId = _genericLogic.Insert(instance);
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
        public IActionResult InsertRange([FromBody] IEnumerable<DIn> instances)
        {
            return StatusCode(StatusCodes.Status201Created, _genericLogic.InsertRange(instances));
        }

        /// <summary>
        ///     Updates an instance of The Model from the Database
        /// </summary>
        /// <remarks>
        ///     NO nulls are accepted
        /// </remarks>
        /// <param name="instance">The instance</param>
        /// <response code="202">Instance updated</response>
        [ProducesResponseType(202, Type = null)]
        [HttpPut("Update")]
        public IActionResult Update([FromBody] DIn instance)
        {
            if (instance == null) return BadRequest();
            _genericLogic.Update(instance);
            return StatusCode(StatusCodes.Status202Accepted);
        }

        /// <summary>
        ///     Updates a List of instances of The Model into the Database
        /// </summary>
        /// <remarks>
        ///     NO nulls are accepted
        /// </remarks>
        /// <param name="instances">The instance</param>
        /// <response code="202">Instances updated</response>
        [ProducesResponseType(202, Type = null)]
        [HttpPut("UpdateRange")]
        public IActionResult UpdateRange([FromBody] IEnumerable<DIn> instances)
        {
            foreach (var instance in instances)
                if (instance == null)
                    return BadRequest();
            _genericLogic.UpdateRange(instances);
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
        [ProducesResponseType(202, Type = null)]
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
        [ProducesResponseType(202, Type = null)]
        [HttpDelete("DeleteRange")]
        public IActionResult DeleteEnablers([FromBody] IEnumerable<int> ids)
        {
            if (ids == null) return BadRequest();
            _genericLogic.SoftDeleteRange(ids);
            return StatusCode(StatusCodes.Status202Accepted);
        }
    }
}