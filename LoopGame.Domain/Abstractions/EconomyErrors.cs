namespace LoopGame.Domain.Abstractions;

public static class EconomyErrors
{
    public static readonly Error InvalidAmount         = new("Economy.InvalidAmount", "The transaction amount must be greater than zero.");
    public static readonly Error InvalidPagination     = new("Economy.InvalidPagination", "Page must be positive and page size must be between 1 and 100.");
    public static readonly Error PlayerNotFound        = new("Economy.PlayerNotFound", "No player exists with this identifier.");
    public static readonly Error PlayerEconomyNotFound = new("Economy.PlayerEconomyNotFound", "No economy record exists for this player.");
    public static readonly Error InsufficientBalance   = new("Economy.InsufficientBalance", "The player's balance is insufficient for this operation.");
    public static readonly Error SalaryAlreadyPaid     = new("Economy.SalaryAlreadyPaid", "The salary for this shift has already been paid.");
}
