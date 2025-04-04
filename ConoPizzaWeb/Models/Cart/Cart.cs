namespace ConoPizzaWeb.Models.Cart
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Unique product count (number of distinct products)
        public int TotalUniqueItems => Items.Count;

        public int TotalItems => Items.Sum(i => i.Quantity);

        // Overall cart total (summing unit price * quantity for each item)
        public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
    }
}
