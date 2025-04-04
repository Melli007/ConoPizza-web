using ConoPizzaWeb.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Products
{
    public class ProductExtra : BaseEntity
    {
        public int ProductExtraId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
