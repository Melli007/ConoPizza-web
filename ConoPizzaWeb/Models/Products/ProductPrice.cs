using ConoPizzaWeb.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Products
{
    public class ProductPrice : BaseEntity
    {
        public int ProductPriceId { get; set; }

        // Add this property to match the foreign key in OnModelCreating:
        public int ProductId { get; set; }

        [Required, MaxLength(50)]
        public string Size { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }
    }
}
