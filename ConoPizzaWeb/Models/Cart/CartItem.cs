namespace ConoPizzaWeb.Models.Cart
{
    public class CartItem
    {
        public Guid CartItemId { get; set; } = Guid.NewGuid();

        public int ProductId { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }

        // Base price for the chosen size
        public decimal BasePrice { get; set; }

        // The name of the chosen size, e.g. "Vogël", "Mesme", "Madhe"
        public string SelectedSize { get; set; }

        // A list of selected extras with name & price
        public List<CartItemExtra> Extras { get; set; } = new List<CartItemExtra>();

        public int Quantity { get; set; }

        // Sum of all extras for one unit
        public decimal ExtrasTotal => Extras.Sum(e => e.Price);

        // Price for a single item (base + extras)
        // We store it in the cart item for display if desired.
        public decimal UnitPrice => BasePrice + ExtrasTotal;

        // Total for this line item (UnitPrice * Quantity)
        public decimal LineTotal => UnitPrice * Quantity;
    }

    // Helper class to store each extra's name & price
    public class CartItemExtra
    {
        public string ExtraName { get; set; }
        public decimal Price { get; set; }
    }
}
