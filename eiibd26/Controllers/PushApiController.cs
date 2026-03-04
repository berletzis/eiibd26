using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Services;

namespace eiibd26.Controllers
{
    [ApiController]
    [Route("api/push")]
    public class PushApiController : ControllerBase
    {
        private readonly PushNotificationService _pushService;

        public PushApiController(PushNotificationService pushService)
        {
            _pushService = pushService;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            try
            {
                var key = _pushService.GetVapidPublicKey();

                if (string.IsNullOrEmpty(key))
                {
                    return BadRequest(new { error = "VAPID Public Key no configurada en appsettings.json" });
                }

                return Ok(key);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("subscribe")]
        [Authorize]
        public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var success = await _pushService.SaveSubscriptionAsync(
                userId,
                request.Endpoint,
                request.Keys.P256dh,
                request.Keys.Auth
            );

            if (success)
            {
                return Ok(new { success = true });
            }

            return BadRequest(new { success = false });
        }

        public class SubscriptionRequest
        {
            public string Endpoint { get; set; } = string.Empty;
            public KeysData Keys { get; set; } = new();
        }

        public class KeysData
        {
            public string P256dh { get; set; } = string.Empty;
            public string Auth { get; set; } = string.Empty;
        }
    }
}
