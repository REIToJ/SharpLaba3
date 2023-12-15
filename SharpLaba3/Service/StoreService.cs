public class StoreService
{
    private readonly IDataAccessLayer _dataAccessLayer;

    public StoreService(IDataAccessLayer dataAccessLayer)
    {
        _dataAccessLayer = dataAccessLayer;
    }

    public void CreateStore(Store store)
    {
        try
        {
            _dataAccessLayer.CreateStore(store);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Error creating store: {ex.Message}", ex);
        }
    }

    public void CreateProduct(Product product)
    {
        try
        {
            _dataAccessLayer.CreateProduct(product);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Error creating product: {ex.Message}", ex);
        }
    }

    public void ImportGoodsToStore(int storeCode, List<Product> products)
    {
        try
        {
            _dataAccessLayer.ImportGoodsToStore(storeCode, products);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Error importing goods to store: {ex.Message}", ex);
        }
    }

    public Store FindCheapestStoreForProduct(string productName)
    {
        try
        {
            return _dataAccessLayer.FindCheapestStoreForProduct(productName);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Error finding cheapest store for product: {ex.Message}", ex);
        }
    }

    public List<Product> GetAffordableProductsInStore(int storeCode, decimal budget)
    {
        try
        {
            return _dataAccessLayer.GetAffordableProductsInStore(storeCode, budget);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Error getting affordable products: {ex.Message}", ex);
        }
    }

    public decimal PurchaseGoods(int storeCode, Dictionary<string, int> goodsToBuy)
    {
        try
        {
            return _dataAccessLayer.PurchaseGoods(storeCode, goodsToBuy);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Error purchasing goods: {ex.Message}", ex);
        }
    }

    public Store FindCheapestStoreForBatch(Dictionary<string, int> goodsToBuy)
    {
        try
        {
            return _dataAccessLayer.FindCheapestStoreForBatch(goodsToBuy);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Error finding cheapest store for batch: {ex.Message}", ex);
        }
    }
}
