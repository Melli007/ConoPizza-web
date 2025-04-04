using ConoPizzaWeb.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Orders
{
    public enum OrderStatus
    {
        Konfirmo,
        NPërgaditje,
        Rruges,
        Arritur,
        Paguar,
        Anuluar
    }

    public class Order : BaseEntity
    {
        public int OrderId { get; set; }

        [Required, MaxLength(100)]
        public string ConsumerName { get; set; }

        [Required, MaxLength(200)]
        public string Address { get; set; }

        [Required, Phone, MaxLength(20)]
        public string PhoneNumber { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Konfirmo;

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
