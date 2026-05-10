namespace Forge.Api.Controllers;

public record UpdateMorningWindowRequest(TimeOnly Start, TimeOnly End, bool ReminderEnabled);
