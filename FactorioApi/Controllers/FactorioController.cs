using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FactorioApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FactorioApi.Controllers
{
    [Route("Factorio")]
    [ApiController]
    public class FactorioController : ControllerBase
    {
        private readonly IFactorioService factorioService;
        public FactorioController(IFactorioService factorioService)
        {
            this.factorioService = factorioService;
        }

        [Authorize]
        [HttpGet("Start")]
        public IActionResult StartGame()
        {
            var result = factorioService.StartGame();

            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "Factorio Service Did not work, something is wrong");
            }
        }

        [Authorize]
        [HttpGet("Stop")]
        public IActionResult StopGame()
        {
            var result = factorioService.StopGame();

            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Factorio Service Did not work, something is wrong");
            }
        }
    }    
}
