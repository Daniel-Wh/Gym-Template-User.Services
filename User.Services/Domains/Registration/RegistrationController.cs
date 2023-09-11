using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace User.Services.Domains
{
    public class RegistrationController : ControllerBase
    {
        private readonly ILogger<RegistrationController> _logger;
        public RegistrationController(ILogger<RegistrationController> logger)
        {
            _logger = logger;
        }

        [HttpPost("/register")]
        public async Task<IActionResult> RegisterUser()
        {

            return new ObjectResult("Ok") { StatusCode = 200 };
        }
        [HttpGet("/health")]
        public async Task<IActionResult> HealthCheck()
        {
            return Ok(200);
        }
    }
}

