using MediatR;

namespace Forge.Application.Profile;

public record SetMonthlyWeightGoalCommand(int MonthlyWeightGoalLb) : IRequest;
