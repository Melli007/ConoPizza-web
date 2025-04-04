using ConoPizzaWeb.Models.Base;
using System.ComponentModel.DataAnnotations;
using ConoPizzaWeb.Models.Categories;

namespace ConoPizzaWeb.Models.Products
{
    public class Product : BaseEntity
    {
        public int ProductId { get; set; }

        [Required, MaxLength(60)]
        public string Title { get; set; }

        [Required, MaxLength(200)]
        public string Description { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        // Featured flag
        public bool IsFeatured { get; set; } = false;

        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public List<ProductPrice> Prices { get; set; } = new List<ProductPrice>();
        public List<ProductIngredient> Ingredients { get; set; } = new List<ProductIngredient>();
        public List<ProductExtra> ExtraOptions { get; set; } = new List<ProductExtra>();
    }
}
