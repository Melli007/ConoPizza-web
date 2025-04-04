using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ConoPizzaWeb.Models.Products
{
    public class ProductCreateViewModel
    {
        // Basic Product Details
        [Required]
        [MaxLength(60)]
        public string Title { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; }

        // For image upload
        public IFormFile ImageFile { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public bool IsFeatured { get; set; }  // This property will hold Yes/No selection


        // Pricing fields:
        // For non-Pizza products
        public decimal? PriceSingle { get; set; }

        // For Pizza products (3 sizes)
        public decimal? PriceSmall { get; set; }
        public decimal? PriceMedium { get; set; }
        public decimal? PriceLarge { get; set; }

        // Extras: dynamically added extras (name and price)
        public List<ProductExtraViewModel> Extras { get; set; } = new List<ProductExtraViewModel>();

        // Selected ingredient IDs (from multi-select)
        public List<int> SelectedIngredientIds { get; set; } = new List<int>();
    }

    public class ProductExtraViewModel
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
