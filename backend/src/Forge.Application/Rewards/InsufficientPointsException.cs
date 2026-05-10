namespace Forge.Application.Rewards;

public class InsufficientPointsException : Exception
{
    public int Balance { get; }
    public int CostPoints { get; }

    public InsufficientPointsException(int balance, int costPoints)
        : base($"Balance {balance} is below cost {costPoints}.")
    {
        Balance = balance;
        CostPoints = costPoints;
    }
}
