using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public delegate bool ValidationRule(Order order, out string errorMessage);

public class OrderValidator
{
    private static bool HasItems(Order order, out string errorMessage)
    {
        if (order.Items == null || order.Items.Count == 0)
        {
            errorMessage = "Order must contain at least one item.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    private static bool TotalUnderLimit(Order order, out string errorMessage)
    {
        const decimal limit = 10_000m;
        if (order.TotalAmount > limit)
        {
            errorMessage = $"Order total {order.TotalAmount:C} exceeds the limit of {limit:C}.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    private static bool AllQuantitiesPositive(Order order, out string errorMessage)
    {
        if (order.Items != null && order.Items.Any(i => i.Quantity <= 0))
        {
            errorMessage = "All item quantities must be greater than zero.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    private static readonly List<(Func<Order, bool> Rule, string Description)> FuncRules = new()
    {
        (o => o.Date <= DateTime.Today,
            "Order date must not be in the future."),
        (o => o.Status != OrderStatus.Cancelled,
            "Cancelled orders cannot be revalidated."),
    };

    private readonly List<ValidationRule> _namedRules = new()
    {
        HasItems,
        TotalUnderLimit,
        AllQuantitiesPositive,
    };

    public (bool IsValid, List<string> Errors) ValidateAll(Order order)
    {
        var errors = new List<string>();

        foreach (var rule in _namedRules)
            if (!rule(order, out var msg) && !string.IsNullOrEmpty(msg))
                errors.Add(msg);

        foreach (var (rule, description) in FuncRules)
            if (!rule(order))
                errors.Add(description);

        return (errors.Count == 0, errors);
    }
}
