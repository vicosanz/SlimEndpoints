namespace WebApplication1.Endpoints.Products
{
    public record Product
    {
        public string Name { get; init; }
        public decimal Price { get; init; }
    }
}
