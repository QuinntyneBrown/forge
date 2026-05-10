using FluentValidation;

namespace Forge.Application.Profile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly HashSet<string> AllowedUnits = new(StringComparer.Ordinal)
    {
        "Imperial",
        "Metric"
    };

    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Units)
            .Must(AllowedUnits.Contains)
            .WithMessage("Units must be 'Imperial' or 'Metric'.");
        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .MaximumLength(64)
            .Must(IsValidTimeZone)
            .WithMessage("TimeZoneId must be a recognized IANA time zone.");
        RuleFor(x => x.DailyActiveCaloriesTarget).InclusiveBetween(100, 10_000);
        RuleFor(x => x.DailyWorkoutMinutesTarget).InclusiveBetween(0, 480);
    }

    private static bool IsValidTimeZone(string ianaTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(ianaTimeZoneId))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
