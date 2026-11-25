using System;
using System.Collections.Generic;
using Aronium.Pos.Core;
using Aronium.Pos.Core.Models;
using Aronium.Pos.Facade.Services;

namespace Aronium.Pos.MultiUom
{
    internal sealed class MultiUomOrderService : IOrderService
    {
        private readonly IOrderService inner;
        private readonly MultiUomRepository repository;

        public MultiUomOrderService(IOrderService inner, MultiUomRepository repository)
        {
            this.inner = inner;
            this.repository = repository;
        }

        public Order GetOrder(string number, object userId) { var value = inner.GetOrder(number, userId); repository.RestoreOrderMetadata(value); return value; }
        public IEnumerable<OrderTotal> GetOrderTotals() { return inner.GetOrderTotals(); }
        public IEnumerable<OrderTotal> GetOrderTotals(object userId) { return inner.GetOrderTotals(userId); }
        public IEnumerable<OrderTotal> GetOpenOrders() { return inner.GetOpenOrders(); }
        public OrderTotal GetOpenOrders(object userId) { return inner.GetOpenOrders(userId); }
        public decimal GetOrderedQuantity(object productId) { return inner.GetOrderedQuantity(productId); }
        public void Save(Order order) { inner.Save(order); repository.SaveOrderMetadata(order); }
        public void Delete(Order order, User user) { inner.Delete(order, user); }
        public void Delete(Order order, User user, IEnumerable<int> rounds) { inner.Delete(order, user, rounds); }
        public bool ApplyDiscounts(Order order) { return inner.ApplyDiscounts(order); }
        public bool ApplyDiscount(Customer customer, OrderItem item) { return inner.ApplyDiscount(customer, item); }
        public DateTime GetBusinessDay() { return inner.GetBusinessDay(); }
        public void Transfer(Order order, Order transferred) { inner.Transfer(order, transferred); }
        public void TransferAllOrders(User from, User to) { inner.TransferAllOrders(from, to); }
        public void LockOrder(User user, Order order) { inner.LockOrder(user, order); }
        public void UnlockOrder(User user, Order order) { inner.UnlockOrder(user, order); }
        public bool HasOpenOrders(User user) { return inner.HasOpenOrders(user); }
        public string GetNextOrderNumber() { return inner.GetNextOrderNumber(); }
        public void DeleteOrderItem(object id) { inner.DeleteOrderItem(id); }
        public void UpdateOrderTotal(Order order) { inner.UpdateOrderTotal(order); }
        public void ResetOrderNumber() { inner.ResetOrderNumber(); }
        public void UpdateOrderDiscount(object id, Discount discount) { inner.UpdateOrderDiscount(id, discount); }
        public void ChangeOrderName(Order order, string name) { inner.ChangeOrderName(order, name); }
        public void ChangeServiceType(Order order, ServiceType type) { inner.ChangeServiceType(order, type); }
    }
}
