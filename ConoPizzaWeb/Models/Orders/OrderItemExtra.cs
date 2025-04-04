using ConoPizzaWeb.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Orders
{
    public class OrderItemExtra : BaseEntity
    {
        public int OrderItemExtraId { get; set; }

        [MaxLength(100)]
        public string ExtraName { get; set; }

        [DataType(DataType.Currency)]
        public decimal ExtraPrice { get; set; }
    }
}
