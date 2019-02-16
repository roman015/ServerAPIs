using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactorioApi.Controllers
{
    [Route("Factorio")]
    [ApiController]
    public class FactorioController : ControllerBase
    {        
        [Authorize]
        [HttpGet("Start")]
        public IActionResult StartGame()
        {
            return Ok("Game has been started");
        }

        [Authorize]
        [HttpGet("Stop")]
        public IActionResult StopGame()
        {
            return Ok("Game has been stopped");
        }
    }

    public class GameType
    {
        public string Status { get; set; }
        public string Version { get; set; }
        public string SaveFile { get; set; }
    }
}
