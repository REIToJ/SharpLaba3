using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CsvFileDAL : IDataAccessLayer
{
    private readonly string storesFilePath = "stores.csv";
    private readonly string productsFilePath = "products.csv";

    private bool StoreExists(int storeCode)
    {
        var stores = ReadStoresFromFile();
        return stores.Any(s => s.Code == storeCode);
    }

    private bool ProductExists(string productName, int storeCode)
    {
        var products = ReadProductsFromFile();
        return products.Any(p => p.Name == productName && p.StoreCode == storeCode);
    }

    public void CreateStore(Store store)
    {
        var stores = ReadStoresFromFile();
        if (stores.Any(s => s.Code == store.Code))
        {
            throw new InvalidOperationException($"A store with code {store.Code} already exists.");
        }

        using (StreamWriter sw = File.AppendText(storesFilePath))
        {
            sw.WriteLine($"{store.Code},{store.Name},{store.Address}");
        }
    }

    public void CreateProduct(Product product)
    {
        if (!StoreExists(product.StoreCode))
        {
            throw new InvalidOperationException($"Store with code {product.StoreCode} does not exist.");
        }

        var products = ReadProductsFromFile();
        var existingProduct = products.FirstOrDefault(p => p.Name == product.Name && p.StoreCode == product.StoreCode);

        if (existingProduct != null)
        {
            existingProduct.Quantity += product.Quantity;
            UpdateProductInFile(products);
        }
        else
        {
            using (StreamWriter sw = File.AppendText(productsFilePath))
            {
                sw.WriteLine($"{product.Name},{product.StoreCode},{product.Quantity},{product.Price}");
            }
        }
    }

    public void ImportGoodsToStore(int storeCode, List<Product> products)
    {
        if (!StoreExists(storeCode))
        {
            throw new InvalidOperationException($"Store with code {storeCode} does not exist.");
        }

        var existingProducts = ReadProductsFromFile();

        foreach (var product in products)
        {
            var existingProduct = existingProducts.FirstOrDefault(p => p.Name == product.Name && p.StoreCode == storeCode);
            if (existingProduct != null)
            {
                existingProduct.Quantity += product.Quantity;
                existingProduct.Price = product.Price;
            }
            else
            {
                existingProducts.Add(product);
            }
        }

        UpdateProductInFile(existingProducts);
    }

    public Store FindCheapestStoreForProduct(string productName)
    {
        //if (!ProductExists(productName, -1)) // -1 as a placeholder for any store code
        //{
        //    throw new InvalidOperationException($"Product {productName} does not exist in any store.");
        //}

        var products = ReadProductsFromFile();
        var cheapestProduct = products.Where(p => p.Name == productName).OrderBy(p => p.Price).FirstOrDefault();

        if (cheapestProduct != null)
        {
            var stores = ReadStoresFromFile();
            return stores.FirstOrDefault(s => s.Code == cheapestProduct.StoreCode);
        }

        return null;
    }

    public List<Product> GetAffordableProductsInStore(int storeCode, decimal budget)
    {
        if (!StoreExists(storeCode))
        {
            throw new InvalidOperationException($"Store with code {storeCode} does not exist.");
        }

        var products = ReadProductsFromFile();
        return products.Where(p => p.StoreCode == storeCode && p.Price <= budget).ToList();
    }

    public decimal PurchaseGoods(int storeCode, Dictionary<string, int> goodsToBuy)
    {
        if (!StoreExists(storeCode))
        {
            throw new InvalidOperationException($"Store with code {storeCode} does not exist.");
        }

        var products = ReadProductsFromFile();
        decimal totalCost = 0;

        foreach (var entry in goodsToBuy)
        {
            var product = products.FirstOrDefault(p => p.StoreCode == storeCode && p.Name == entry.Key && p.Quantity >= entry.Value);
            if (product != null)
            {
                totalCost += product.Price * entry.Value;
                product.Quantity -= entry.Value;
            }
            else
            {
                return -1; // Indicate that the purchase cannot be completed
            }
        }

        UpdateProductInFile(products);
        return totalCost;
    }

    public Store FindCheapestStoreForBatch(Dictionary<string, int> goodsToBuy)
    {
        foreach (var item in goodsToBuy)
        {
            //if (!ProductExists(item.Key, -1)) // -1 as a placeholder for any store code
            //{
            //    throw new InvalidOperationException($"Product {item.Key} does not exist in any store.");
            //}
        }

        var stores = ReadStoresFromFile();
        var products = ReadProductsFromFile();
        Store cheapestStore = null;
        decimal lowestCost = decimal.MaxValue;

        foreach (var store in stores)
        {
            decimal totalCost = 0;
            bool canCompletePurchase = true;

            foreach (var item in goodsToBuy)
            {
                var product = products.FirstOrDefault(p => p.StoreCode == store.Code && p.Name == item.Key);
                if (product != null && product.Quantity >= item.Value)
                {
                    totalCost += product.Price * item.Value;
                }
                else
                {
                    canCompletePurchase = false;
                    break;
                }
            }

            if (canCompletePurchase && totalCost < lowestCost)
            {
                lowestCost = totalCost;
                cheapestStore = store;
            }
        }

        return cheapestStore;
    }

    private List<Store> ReadStoresFromFile()
    {
        var stores = new List<Store>();

        if (File.Exists(storesFilePath))
        {
            var lines = File.ReadAllLines(storesFilePath);
            foreach (var line in lines)
            {
                var values = line.Split(',');
                var store = new Store
                {
                    Code = int.Parse(values[0]),
                    Name = values[1],
                    Address = values[2]
                };
                stores.Add(store);
            }
        }

        return stores;
    }

    private List<Product> ReadProductsFromFile()
    {
        var products = new List<Product>();

        if (File.Exists(productsFilePath))
        {
            var lines = File.ReadAllLines(productsFilePath);
            foreach (var line in lines)
            {
                var values = line.Split(',');
                var product = new Product
                {
                    Name = values[0],
                    StoreCode = int.Parse(values[1]),
                    Quantity = int.Parse(values[2]),
                    Price = decimal.Parse(values[3])
                };
                products.Add(product);
            }
        }

        return products;
    }

    private void UpdateProductInFile(List<Product> products)
    {
        using (StreamWriter sw = new StreamWriter(productsFilePath))
        {
            foreach (var product in products)
            {
                sw.WriteLine($"{product.Name},{product.StoreCode},{product.Quantity},{product.Price}");
            }
        }
    }
}
