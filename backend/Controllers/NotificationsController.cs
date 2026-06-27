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
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public NotificationsController(ApplicationDbContext _db)
        {
            db = _db;
        }

        [Authorize]
        [HttpPost("Notifications")]
        public async Task<IActionResult> Notifications([FromBody] NotificationsDto noti)
        {
            if (noti == null)
            {
                return BadRequest("null data");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            Notifications notifications = new Notifications
            {
                User_id      = userIdClaim.Value,
                Notification = noti.Notification
            };

            db.Notifications.Add(notifications);
            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
