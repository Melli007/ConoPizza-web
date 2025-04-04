namespace ConoPizzaWeb.Models.Ingredients
{
    // A separate ViewModel for your create/edit form
    public class IngredientViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        // The path of the currently stored image
        public string ExistingImagePath { get; set; }

        // The newly uploaded file
        public IFormFile UploadFile { get; set; }
    }

}
