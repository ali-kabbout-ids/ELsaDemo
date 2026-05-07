namespace PurchaseOrderApi.Services;
using PurchaseOrderApi.Models;

/// Singleton in-memory store — simulates a database for this demo.
public class PurchaseOrderStore
{
    private readonly Dictionary<int, PurchaseOrder> _orders = new();
    private int _counter = 1;

    public PurchaseOrder Create(CreateOrderRequest req)
    {
        var order = new PurchaseOrder
        {
            Id             = _counter++,
            Title          = req.Title,
            Amount         = req.Amount,
            RequesterEmail = req.RequesterEmail,
            ManagerEmail   = req.ManagerEmail
        };
        _orders[order.Id] = order;
        return order;
    }

    public PurchaseOrder? GetById(int id)
        => _orders.TryGetValue(id, out var o) ? o : null;

    public IEnumerable<PurchaseOrder> GetAll() => _orders.Values;

    public void Save(PurchaseOrder order) => _orders[order.Id] = order;
}
