using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationPreferenceRepository _notificationPreferenceRepository;

        public NotificationController(INotificationPreferenceRepository notificationPreferenceRepository)
        {
            _notificationPreferenceRepository = notificationPreferenceRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationPreference>>> GetAllNotificationPreferences()
        {
            var preferences = await _notificationPreferenceRepository.GetAllNotificationPreferencesAsync();
            return Ok(preferences);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationPreference>> GetNotificationPreference(string id)
        {
            var preference = await _notificationPreferenceRepository.GetNotificationPreferenceByIdAsync(id);
            if (preference == null)
            {
                return NotFound();
            }
            return Ok(preference);
        }

        [HttpPost]
        public async Task<ActionResult<NotificationPreference>> AddNotificationPreference([FromBody] NotificationPreference preference)
        {
            await _notificationPreferenceRepository.AddNotificationPreferenceAsync(preference);
            return CreatedAtAction(nameof(GetNotificationPreference), new { id = preference.Id }, preference);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotificationPreference(string id, [FromBody] NotificationPreference preference)
        {
            if (id != preference.Id)
            {
                return BadRequest("Notification preference ID mismatch.");
            }

            var existingPreference = await _notificationPreferenceRepository.GetNotificationPreferenceByIdAsync(id);
            if (existingPreference == null)
            {
                return NotFound();
            }

            await _notificationPreferenceRepository.UpdateNotificationPreferenceAsync(preference);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotificationPreference(string id)
        {
            var existingPreference = await _notificationPreferenceRepository.GetNotificationPreferenceByIdAsync(id);
            if (existingPreference == null)
            {
                return NotFound();
            }

            await _notificationPreferenceRepository.DeleteNotificationPreferenceAsync(id);
            return NoContent();
        }
    }
}
