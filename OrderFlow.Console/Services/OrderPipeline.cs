using OrderFlow.Console.Events;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderPipeline
{
    public event EventHandler<OrderStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<OrderValidationEventArgs>? ValidationCompleted;

    private readonly OrderValidator _validator = new();

    public void ProcessOrder(Order order)
    {
        Transition(order, OrderStatus.New);

        var (isValid, errors) = _validator.ValidateAll(order);
        ValidationCompleted?.Invoke(this, new OrderValidationEventArgs(order, isValid, errors));

        if (!isValid)
            return;

        Transition(order, OrderStatus.Validated);
        Transition(order, OrderStatus.Processing);
        Transition(order, OrderStatus.Completed);
    }

    public Task ProcessOrderAsync(Order order) => Task.Run(() => ProcessOrder(order));

    private void Transition(Order order, OrderStatus newStatus)
    {
        var old = order.Status;
        order.Status = newStatus;
        StatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, old, newStatus));
    }
}
