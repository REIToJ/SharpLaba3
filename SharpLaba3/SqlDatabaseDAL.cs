using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

public class SqliteDatabaseDAL : IDataAccessLayer
{
    private readonly string _connectionString;

    public SqliteDatabaseDAL(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void CreateStore(Store store)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand("INSERT INTO Stores (Code, Name, Address) VALUES (@Code, @Name, @Address)", connection);
        command.Parameters.AddWithValue("@Code", store.Code);
        command.Parameters.AddWithValue("@Name", store.Name);
        command.Parameters.AddWithValue("@Address", store.Address);

        command.ExecuteNonQuery();
    }

    public void CreateProduct(Product product)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand("INSERT INTO Products (Name, StoreCode, Quantity, Price) VALUES (@Name, @StoreCode, @Quantity, @Price)", connection);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@StoreCode", product.StoreCode);
        command.Parameters.AddWithValue("@Quantity", product.Quantity);
        command.Parameters.AddWithValue("@Price", product.Price);

        command.ExecuteNonQuery();
    }

    public void ImportGoodsToStore(int storeCode, List<Product> products)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (var product in products)
        {
            using var checkCommand = new SqliteCommand("SELECT COUNT(*) FROM Products WHERE Name = @Name AND StoreCode = @StoreCode", connection);
            checkCommand.Parameters.AddWithValue("@Name", product.Name);
            checkCommand.Parameters.AddWithValue("@StoreCode", storeCode);

            int exists = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (exists > 0)
            {
                using var updateCommand = new SqliteCommand("UPDATE Products SET Quantity = Quantity + @Quantity WHERE Name = @Name AND StoreCode = @StoreCode", connection);
                updateCommand.Parameters.AddWithValue("@Quantity", product.Quantity);
                updateCommand.Parameters.AddWithValue("@Name", product.Name);
                updateCommand.Parameters.AddWithValue("@StoreCode", storeCode);
                updateCommand.ExecuteNonQuery();
            }
            else
            {
                using var insertCommand = new SqliteCommand("INSERT INTO Products (Name, StoreCode, Quantity, Price) VALUES (@Name, @StoreCode, @Quantity, @Price)", connection);
                insertCommand.Parameters.AddWithValue("@Name", product.Name);
                insertCommand.Parameters.AddWithValue("@StoreCode", storeCode);
                insertCommand.Parameters.AddWithValue("@Quantity", product.Quantity);
                insertCommand.Parameters.AddWithValue("@Price", product.Price);
                insertCommand.ExecuteNonQuery();
            }
        }
    }

    public Store FindCheapestStoreForProduct(string productName)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Adjusted SQL query to use LIMIT instead of TOP
        using var command = new SqliteCommand(
            "SELECT S.* FROM Stores S " +
            "INNER JOIN Products P ON S.Code = P.StoreCode " +
            "WHERE P.Name = @ProductName " +
            "ORDER BY P.Price ASC LIMIT 1", connection);

        command.Parameters.AddWithValue("@ProductName", productName);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Store
            {
                Code = Convert.ToInt32(reader["Code"]),
                Name = reader["Name"].ToString(),
                Address = reader["Address"].ToString()
            };
        }

        return null;
    }


    public List<Product> GetAffordableProductsInStore(int storeCode, decimal budget)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand("SELECT Name, StoreCode, Price, Quantity FROM Products WHERE StoreCode = @StoreCode AND Price > 0", connection);
        command.Parameters.AddWithValue("@StoreCode", storeCode);

        using var reader = command.ExecuteReader();
        var affordableProducts = new List<Product>();

        while (reader.Read())
        {
            var price = Convert.ToDecimal(reader["Price"]);
            var affordableQuantity = price > 0 ? Math.Min(Convert.ToInt32(reader["Quantity"]), (int)(budget / price)) : 0;

            if (affordableQuantity > 0)
            {
                affordableProducts.Add(new Product
                {
                    Name = reader["Name"].ToString(),
                    StoreCode = Convert.ToInt32(reader["StoreCode"]),
                    Quantity = affordableQuantity,
                    Price = price
                });
            }
        }

        return affordableProducts;
    }




    public decimal PurchaseGoods(int storeCode, Dictionary<string, int> goodsToBuy)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        decimal totalCost = 0;

        try
        {
            foreach (var entry in goodsToBuy)
            {
                using var command = new SqliteCommand("SELECT * FROM Products WHERE StoreCode = @StoreCode AND Name = @ProductName AND Quantity >= @Quantity", connection, transaction);
                command.Parameters.AddWithValue("@StoreCode", storeCode);
                command.Parameters.AddWithValue("@ProductName", entry.Key);
                command.Parameters.AddWithValue("@Quantity", entry.Value);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    totalCost += Convert.ToDecimal(reader["Price"]) * entry.Value;
                    reader.Close();

                    using var updateCommand = new SqliteCommand("UPDATE Products SET Quantity = Quantity - @Quantity WHERE StoreCode = @StoreCode AND Name = @ProductName", connection, transaction);
                    updateCommand.Parameters.AddWithValue("@StoreCode", storeCode);
                    updateCommand.Parameters.AddWithValue("@ProductName", entry.Key);
                    updateCommand.Parameters.AddWithValue("@Quantity", entry.Value);
                    updateCommand.ExecuteNonQuery();
                }
                else
                {
                    throw new InvalidOperationException("Insufficient stock for product.");
                }
            }

            transaction.Commit();
            return totalCost;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public Store FindCheapestStoreForBatch(Dictionary<string, int> goodsToBuy)
    {
        var stores = GetAllStores();
        Store cheapestStore = null;
        decimal lowestTotalCost = decimal.MaxValue;

        foreach (var store in stores)
        {
            decimal totalCostForStore = CalculateTotalCostForStore(store.Code, goodsToBuy);
            if (totalCostForStore >= 0 && totalCostForStore < lowestTotalCost)
            {
                lowestTotalCost = totalCostForStore;
                cheapestStore = store;
            }
        }

        return cheapestStore;
    }

    private List<Store> GetAllStores()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = new SqliteCommand("SELECT * FROM Stores", connection);
        using var reader = command.ExecuteReader();

        var stores = new List<Store>();
        while (reader.Read())
        {
            stores.Add(new Store
            {
                Code = Convert.ToInt32(reader["Code"]),
                Name = reader["Name"].ToString(),
                Address = reader["Address"].ToString()
            });
        }

        return stores;
    }

    private decimal CalculateTotalCostForStore(int storeCode, Dictionary<string, int> goodsToBuy)
    {
        decimal totalCost = 0;

        foreach (var item in goodsToBuy)
        {
            var productCost = GetCostForProduct(storeCode, item.Key, item.Value);
            if (productCost < 0) // Product not available in required quantity
            {
                return -1;
            }

            totalCost += productCost;
        }

        return totalCost;
    }

    private decimal GetCostForProduct(int storeCode, string productName, int quantity)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = new SqliteCommand("SELECT Price, Quantity FROM Products WHERE StoreCode = @StoreCode AND Name = @Name", connection);
        command.Parameters.AddWithValue("@StoreCode", storeCode);
        command.Parameters.AddWithValue("@Name", productName);

        using var reader = command.ExecuteReader();
        if (reader.Read() && Convert.ToInt32(reader["Quantity"]) >= quantity)
        {
            return Convert.ToDecimal(reader["Price"]) * quantity;
        }

        return -1; // Product not available or not enough quantity
    }

}
