using ConoPizzaWeb.Models.Products;
using ConoPizzaWeb.Models.Ingredients;
using X.PagedList;
using System.Collections.Generic;

namespace ConoPizzaWeb.Models.ViewModels
{
    public class AdminMenuDashboardViewModel
    {
        public IPagedList<Product> Products { get; set; }
        public IPagedList<Ingredient> Ingredients { get; set; }
        public List<Product> FeaturedProducts { get; set; }

        // New properties for filter criteria:
        public int? FilterCategoryId { get; set; }
        public bool? FilterIsFeatured { get; set; }
        public string SearchTerm { get; set; }
    }
}

