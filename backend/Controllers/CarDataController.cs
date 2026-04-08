using System;
using System.Linq;
using System.Security.Claims;
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
        public IActionResult CarData([FromBody] DataFromCarDto datadto)
        {
            if (datadto == null)
            {
                return BadRequest("null data");
            }
            else
            {
                Claim userid = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

                DataFromCar data = new DataFromCar();

                data.User_id = userid.Value;
                data.CarYear = datadto.CarYear;
                data.EnginePower = datadto.EnginePower;
                data.EngineCoolantTemp = datadto.EngineCoolantTemp;
                data.EngineLoad = datadto.EngineLoad;
                data.EngineRPM = datadto.EngineRPM;
                data.AirIntakeTemp = datadto.AirIntakeTemp;
                data.Speed = datadto.Speed;
                data.ShortTermFuelBank1 = datadto.ShortTermFuelBank1;
                data.throttlePosition = datadto.throttlePosition;
                data.TimingAdvance = datadto.TimingAdvance;
                data.TroubleCode = datadto.TroubleCode;
                data.Description = datadto.Description;
                data.Date = datadto.Date;

                db.DataFromCar.Add(data);
                db.SaveChanges();

                return Ok();
            }
        }
    }
}
