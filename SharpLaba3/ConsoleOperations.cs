using System;
using System.Collections.Generic;

public class ConsoleOperations
{
    private readonly StoreService _storeService;

    public ConsoleOperations(StoreService storeService)
    {
        _storeService = storeService;
    }

    public void CreateStoreFromConsole()
    {
        try
        {
            Console.WriteLine("Enter store code:");
            int code = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter store name:");
            string name = Console.ReadLine();

            Console.WriteLine("Enter store address:");
            string address = Console.ReadLine();

            var store = new Store { Code = code, Name = name, Address = address };
            _storeService.CreateStore(store);

            Console.WriteLine("Store created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public void CreateProductFromConsole()
    {
        try
        {
            Console.WriteLine("Enter product name:");
            string name = Console.ReadLine();

            Console.WriteLine("Enter store code for the product:");
            int storeCode = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter quantity of the product:");
            int quantity = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter price of the product:");
            decimal price = decimal.Parse(Console.ReadLine());

            var product = new Product { Name = name, StoreCode = storeCode, Quantity = quantity, Price = price };
            _storeService.CreateProduct(product);

            Console.WriteLine("Product created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public void DeliverBatchToStoreFromConsole()
    {
        Console.WriteLine("Enter the store code to deliver to:");
        int storeCode = int.Parse(Console.ReadLine());

        var productsToImport = new List<Product>();
        bool addingMore = true;

        while (addingMore)
        {
            Console.WriteLine("Enter product name:");
            string name = Console.ReadLine();

            Console.WriteLine("Enter quantity of the product:");
            int quantity = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter price of the product:");
            decimal price = decimal.Parse(Console.ReadLine());

            productsToImport.Add(new Product { Name = name, StoreCode = storeCode, Quantity = quantity, Price = price });

            Console.WriteLine("Add another product? (yes/no)");
            addingMore = Console.ReadLine().ToLower() == "yes";
        }

        _storeService.ImportGoodsToStore(storeCode, productsToImport);

        Console.WriteLine("Batch of products delivered successfully.");
    }

    public void FindCheapestStoreForProductFromConsole()
    {
        Console.WriteLine("Enter the product name to find the cheapest store:");
        string productName = Console.ReadLine();

        var cheapestStore = _storeService.FindCheapestStoreForProduct(productName);

        if (cheapestStore != null)
        {
            Console.WriteLine($"Cheapest store for {productName} is {cheapestStore.Name} at address {cheapestStore.Address}");
        }
        else
        {
            Console.WriteLine($"No store found selling {productName}.");
        }
    }

    public void FindAffordableProductsFromConsole()
    {
        Console.WriteLine("Enter the store code:");
        int storeCode = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter your budget:");
        decimal budget = decimal.Parse(Console.ReadLine());

        var affordableProducts = _storeService.GetAffordableProductsInStore(storeCode, budget);

        if (affordableProducts.Any())
        {
            Console.WriteLine($"Products you can buy with {budget} rubles:");
            foreach (var product in affordableProducts)
            {
                Console.WriteLine($"{product.Quantity} {product.Name} - {product.Price} rubles each");
            }
        }
        else
        {
            Console.WriteLine("No products are affordable within your budget.");
        }
    }

    public void PurchaseBatchFromConsole()
    {
        Console.WriteLine("Enter the store code:");
        int storeCode = int.Parse(Console.ReadLine());

        var goodsToBuy = new Dictionary<string, int>();
        bool addingMore = true;

        while (addingMore)
        {
            Console.WriteLine("Enter product name:");
            string name = Console.ReadLine();

            Console.WriteLine("Enter quantity to buy:");
            int quantity = int.Parse(Console.ReadLine());

            goodsToBuy[name] = quantity;

            Console.WriteLine("Add another product? (yes/no)");
            addingMore = Console.ReadLine().ToLower() == "yes";
        }

        decimal totalCost = _storeService.PurchaseGoods(storeCode, goodsToBuy);

        if (totalCost >= 0)
        {
            Console.WriteLine($"Total cost of the purchase: {totalCost} rubles");
        }
        else
        {
            Console.WriteLine("Purchase could not be completed due to insufficient stock.");
        }
    }

    public void FindCheapestStoreForBatchFromConsole()
    {
        var goodsToBuy = new Dictionary<string, int>();
        bool addingMore = true;

        while (addingMore)
        {
            Console.WriteLine("Enter product name:");
            string name = Console.ReadLine();

            Console.WriteLine("Enter quantity:");
            int quantity = int.Parse(Console.ReadLine());

            goodsToBuy[name] = quantity;

            Console.WriteLine("Add another product? (yes/no)");
            addingMore = Console.ReadLine().ToLower() == "yes";
        }

        var cheapestStore = _storeService.FindCheapestStoreForBatch(goodsToBuy);

        if (cheapestStore != null)
        {
            Console.WriteLine($"Cheapest store for the batch is {cheapestStore.Name} at address {cheapestStore.Address}");
        }
        else
        {
            Console.WriteLine("No store found that can fulfill the batch at the lowest cost.");
        }
    }
}
