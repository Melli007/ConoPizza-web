namespace ConoPizzaWeb.Models.Products
{
    public class ProductEditViewModel : ProductCreateViewModel
    {
        public int ProductId { get; set; }
        // Optionally, store the existing image path if you want to preview or remove it
        public string ExistingImageUrl { get; set; }
    }

}
