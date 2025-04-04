using ConoPizzaWeb.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace ConoPizzaWeb.Models.Ingredients
{
    public class Ingredient : BaseEntity
    {
        public int IngredientId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        // New property for an image URL
        [MaxLength(255)]
        public string InImgUrl { get; set; }  // or whatever you want to call it

    }
}
