using System.Collections.Generic;

public interface IDataAccessLayer
{
    void CreateStore(Store store);
    void CreateProduct(Product product);
    void ImportGoodsToStore(int storeCode, List<Product> products);
    Store FindCheapestStoreForProduct(string productName);
    List<Product> GetAffordableProductsInStore(int storeCode, decimal budget);
    decimal PurchaseGoods(int storeCode, Dictionary<string, int> goodsToBuy);
    Store FindCheapestStoreForBatch(Dictionary<string, int> goodsToBuy);
}