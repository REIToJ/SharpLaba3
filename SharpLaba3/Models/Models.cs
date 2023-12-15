public class Store
{
    public int Code { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
}

public class Product
{
    public string Name { get; set; }
    public int StoreCode { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
