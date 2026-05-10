namespace Forge.Api.Controllers;

public record UpdateKitchenWindowRequest(TimeOnly Start, TimeOnly End, bool NudgeEnabled);
