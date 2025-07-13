using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationPreferenceRepository _notificationPreferenceRepository;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationPreferenceRepository notificationPreferenceRepository, ILogger<NotificationController> logger)
        {
            _notificationPreferenceRepository = notificationPreferenceRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationPreference>>> GetAllNotificationPreferences()
        {
            _logger.LogInformation("Fetching all notification preferences.");
            try
            {
                var preferences = await _notificationPreferenceRepository.GetAllNotificationPreferencesAsync();
                _logger.LogInformation("Successfully fetched {Count} notification preferences.", preferences.Count());
                return Ok(preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all notification preferences.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationPreference>> GetNotificationPreference(string id)
        {
            _logger.LogInformation("Fetching notification preference with ID: {Id}", id);
            try
            {
                var preference = await _notificationPreferenceRepository.GetNotificationPreferenceByIdAsync(id);
                if (preference == null)
                {
                    _logger.LogWarning("Notification preference with ID: {Id} not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully fetched notification preference with ID: {Id}", id);
                return Ok(preference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notification preference with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<NotificationPreference>> AddNotificationPreference([FromBody] NotificationPreference preference)
        {
            _logger.LogInformation("Adding new notification preference for user: {UserId}", preference.UserId);
            try
            {
                await _notificationPreferenceRepository.AddNotificationPreferenceAsync(preference);
                _logger.LogInformation("Successfully added notification preference with ID: {Id}", preference.Id);
                return CreatedAtAction(nameof(GetNotificationPreference), new { id = preference.Id }, preference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notification preference for user: {UserId}", preference.UserId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotificationPreference(string id, [FromBody] NotificationPreference preference)
        {
            _logger.LogInformation("Updating notification preference with ID: {Id}", id);
            if (id != preference.Id)
            {
                _logger.LogWarning("Notification preference ID mismatch for update. Route ID: {RouteId}, Body ID: {BodyId}", id, preference.Id);
                return BadRequest("Notification preference ID mismatch.");
            }

            try
            {
                var existingPreference = await _notificationPreferenceRepository.GetNotificationPreferenceByIdAsync(id);
                if (existingPreference == null)
                {
                    _logger.LogWarning("Notification preference with ID: {Id} not found for update.", id);
                    return NotFound();
                }

                await _notificationPreferenceRepository.UpdateNotificationPreferenceAsync(preference);
                _logger.LogInformation("Successfully updated notification preference with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preference with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotificationPreference(string id)
        {
            _logger.LogInformation("Deleting notification preference with ID: {Id}", id);
            try
            {
                var existingPreference = await _notificationPreferenceRepository.GetNotificationPreferenceByIdAsync(id);
                if (existingPreference == null)
                {
                    _logger.LogWarning("Notification preference with ID: {Id} not found for deletion.", id);
                    return NotFound();
                }

                await _notificationPreferenceRepository.DeleteNotificationPreferenceAsync(id);
                _logger.LogInformation("Successfully deleted notification preference with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification preference with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}