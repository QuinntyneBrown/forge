using FluentValidation;

namespace Forge.Application.HealthKit;

public class IngestHealthKitSampleCommandValidator : AbstractValidator<IngestHealthKitSampleCommand>
{
    public IngestHealthKitSampleCommandValidator()
    {
        RuleFor(x => x.SampleType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0m);
    }
}
