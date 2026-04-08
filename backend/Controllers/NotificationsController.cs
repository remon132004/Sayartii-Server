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
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public NotificationsController(ApplicationDbContext _db)
        {
            db = _db;
        }

        [Authorize]
        [HttpPost("Notifications")]
        public IActionResult Notifications([FromBody] NotificationsDto noti)
        {
            if (noti == null)
            {
                return BadRequest("null data");
            }
            else
            {
                Notifications notifications = new Notifications();
                Claim userid = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

                notifications.User_id = userid.Value;
                notifications.Notification = noti.Notification;

                db.Notifications.Add(notifications);
                db.SaveChanges();

                return Ok();
            }
        }
    }
}
