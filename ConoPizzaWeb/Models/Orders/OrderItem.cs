using ConoPizzaWeb.Models.Base;
using ConoPizzaWeb.Models.Products;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Orders
{
    public class OrderItem : BaseEntity
    {
        public int OrderItemId { get; set; }

        // Snapshot of product info
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string SelectedSize { get; set; }

        public int Quantity { get; set; }

        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }  // price at time of ordering

        public List<OrderItemExtra> SelectedExtras { get; set; } = new List<OrderItemExtra>();
    }
}
