using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayartii.Api.Data;
using Sayartii.Api.Models;

namespace Sayartii.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarDataController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public CarDataController(ApplicationDbContext _db)
        {
            db = _db;
        }

        [Authorize]
        [HttpPost("CarData")]
        public async Task<IActionResult> CarData([FromBody] DataFromCarDto datadto)
        {
            if (datadto == null)
            {
                return BadRequest("null data");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            DataFromCar data = new DataFromCar
            {
                User_id         = userIdClaim.Value,
                CarYear          = datadto.CarYear,
                EnginePower      = datadto.EnginePower,
                EngineCoolantTemp = datadto.EngineCoolantTemp,
                EngineLoad       = datadto.EngineLoad,
                EngineRPM        = datadto.EngineRPM,
                AirIntakeTemp    = datadto.AirIntakeTemp,
                Speed            = datadto.Speed,
                ShortTermFuelBank1 = datadto.ShortTermFuelBank1,
                throttlePosition = datadto.throttlePosition,
                TimingAdvance    = datadto.TimingAdvance,
                TroubleCode      = datadto.TroubleCode,
                Description      = datadto.Description,
                Date             = datadto.Date
            };

            db.DataFromCar.Add(data);
            await db.SaveChangesAsync();

            return Ok();
        }
    }
}

