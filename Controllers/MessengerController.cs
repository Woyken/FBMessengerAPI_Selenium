using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessengerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;

namespace MessengerAPI.Controllers
{
    [Route("api/[controller]")]
    public class MessengerController : Controller
    {
        public IMessengerServices MessengerService { get; }

        public MessengerController(IMessengerServices messengerService)
        {
            MessengerService = messengerService;
        }
        protected override void Dispose(bool disposing){
            if(disposing){
            }
        }

        /// <summary>
        /// Start login process to Messenger
        /// </summary>
        /// <remarks>
        /// This request starts new instance of messengerAPI process. Returns new id for further requests
        /// </remarks>
        /// <group>Login</group>
        [HttpPost("login")]
        [ProducesResponseType(200, Type = typeof(ServiceStatus))]
        [ProducesResponseType(400)]
        public IActionResult Login([FromBody]FBCredentials fbCredentials)
        {
            
            if(fbCredentials == null || !ModelState.IsValid)
                return BadRequest("Invalid parameters");

            var newToken = Guid.NewGuid();
            var service = MessengerService.GetCreateMessenger(newToken);
            if(null != service)
                service.LoginWithCredentials(fbCredentials.Username, fbCredentials.Password);
            return Ok(new ServiceStatus(service));
        }

        /// <summary>
        /// Complete login to messenger.
        /// </summary>
        /// <remarks>
        /// This request completes logging in process with code generator's generated code.
        /// </remarks>
        /// <group>Login</group>
        [HttpPost("{token}/confirmLogin")]
        [ProducesResponseType(200, Type = typeof(ServiceStatus))]
        [ProducesResponseType(404, Type = typeof(ServiceStatus))]
        public IActionResult ConfirmLogin(Guid token, [FromBody]string code)
        {
            var service = MessengerService.GetMessenger(token);
            if(null != service)
                service.CompleteLogin(code);
            if(service == null)
                return NotFound(new ServiceStatus(service));
            return Ok(new ServiceStatus(service));
        }

        /// <summary>
        /// Get service status.
        /// </summary>
        /// <remarks>
        /// This request get status of your messenger running service.
        /// </remarks>
        /// <group>General</group>
        [HttpGet("{token}/status")]
        [ProducesResponseType(200, Type = typeof(ServiceStatus))]
        [ProducesResponseType(404, Type = typeof(ServiceStatus))]
        public IActionResult GetStatus(Guid token)
        {
            var service = MessengerService.GetMessenger(token);
            if(service == null)
                return NotFound(new ServiceStatus(service));
            return Ok(new ServiceStatus(service));
        } 

        /// <summary>
        /// Send message.
        /// </summary>
        /// <remarks>
        /// This request sends actual message with logged in Messenger.
        /// </remarks>
        /// <group>Action</group>
        [HttpPost("{token}/send")]
        [ProducesResponseType(200, Type = typeof(ServiceStatus))]
        [ProducesResponseType(404, Type = typeof(ServiceStatus))]
        public IActionResult ConfirmLogin(Guid token, [FromQuery]string to, [FromQuery]string message)
        {
            var service = MessengerService.GetMessenger(token);
            if(null != service)
                service.SendMessage(to, message);
            if(service == null)
                return NotFound(new ServiceStatus(service));
            return Ok(new ServiceStatus(service));
        }

        /// <summary>
        /// Keep service alive.
        /// </summary>
        /// <remarks>
        /// This request is used to keep Messenger service alive.
        /// </remarks>
        /// <group>Action</group>
        [HttpGet("{token}/keepAlive")]
        [ProducesResponseType(200, Type = typeof(ServiceStatus))]
        [ProducesResponseType(404, Type = typeof(ServiceStatus))]
        public IActionResult KeepAlive(Guid token)
        {
            var service = MessengerService.GetMessenger(token);
            if(null != service)
                service.KeepAlive();
            if(service == null)
                return NotFound(new ServiceStatus(service));
            return Ok(new ServiceStatus(service));
        }
    }
}
