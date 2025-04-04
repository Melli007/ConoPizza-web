using ConoPizzaWeb.Models.Base;
using ConoPizzaWeb.Models.Products;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Categories
{
    public class Category : BaseEntity
    {
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();
    }
}
