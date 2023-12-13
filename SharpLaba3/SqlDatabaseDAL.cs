using System;
using System.Data.SqlClient;
using System.Collections.Generic;

public class SqlDatabaseDAL : IDataAccessLayer
{
    private readonly string connectionString;

    public SqlDatabaseDAL(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public void CreateStore(Store store)
    {
        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        using SqlCommand command = new SqlCommand("INSERT INTO Stores (Code, Name, Address) VALUES (@Code, @Name, @Address)", connection);
        command.Parameters.AddWithValue("@Code", store.Code);
        command.Parameters.AddWithValue("@Name", store.Name);
        command.Parameters.AddWithValue("@Address", store.Address);

        command.ExecuteNonQuery();
    }

    public void CreateProduct(Product product)
    {
        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        using SqlCommand command = new SqlCommand("INSERT INTO Products (Name, StoreCode, Quantity, Price) VALUES (@Name, @StoreCode, @Quantity, @Price)", connection);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue("@StoreCode", product.StoreCode);
        command.Parameters.AddWithValue("@Quantity", product.Quantity);
        command.Parameters.AddWithValue("@Price", product.Price);

        command.ExecuteNonQuery();
    }

    public void ImportGoodsToStore(int storeCode, List<Product> products)
    {
        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        foreach (var product in products)
        {
            using SqlCommand checkCommand = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Name = @Name AND StoreCode = @StoreCode", connection);
            checkCommand.Parameters.AddWithValue("@Name", product.Name);
            checkCommand.Parameters.AddWithValue("@StoreCode", storeCode);

            int exists = (int)checkCommand.ExecuteScalar();

            if (exists > 0)
            {
                using SqlCommand updateCommand = new SqlCommand("UPDATE Products SET Quantity = Quantity + @Quantity WHERE Name = @Name AND StoreCode = @StoreCode", connection);
                updateCommand.Parameters.AddWithValue("@Quantity", product.Quantity);
                updateCommand.Parameters.AddWithValue("@Name", product.Name);
                updateCommand.Parameters.AddWithValue("@StoreCode", storeCode);
                updateCommand.ExecuteNonQuery();
            }
            else
            {
                using SqlCommand insertCommand = new SqlCommand("INSERT INTO Products (Name, StoreCode, Quantity, Price) VALUES (@Name, @StoreCode, @Quantity, @Price)", connection);
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
        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        using SqlCommand command = new SqlCommand("SELECT TOP 1 S.* FROM Stores S INNER JOIN Products P ON S.Code = P.StoreCode WHERE P.Name = @ProductName ORDER BY P.Price ASC", connection);
        command.Parameters.AddWithValue("@ProductName", productName);

        using SqlDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Store
            {
                Code = (int)reader["Code"],
                Name = reader["Name"].ToString(),
                Address = reader["Address"].ToString()
            };
        }

        return null;
    }

    public List<Product> GetAffordableProductsInStore(int storeCode, decimal budget)
    {
        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        using SqlCommand command = new SqlCommand("SELECT * FROM Products WHERE StoreCode = @StoreCode AND Price <= @Budget", connection);
        command.Parameters.AddWithValue("@StoreCode", storeCode);
        command.Parameters.AddWithValue("@Budget", budget);

        using SqlDataReader reader = command.ExecuteReader();
        List<Product> affordableProducts = new List<Product>();

        while (reader.Read())
        {
            affordableProducts.Add(new Product
            {
                Name = reader["Name"].ToString(),
                StoreCode = (int)reader["StoreCode"],
                Quantity = (int)reader["Quantity"],
                Price = (decimal)reader["Price"]
            });
        }

        return affordableProducts;
    }

    public decimal PurchaseGoods(int storeCode, Dictionary<string, int> goodsToBuy)
    {
        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();
        SqlTransaction transaction = connection.BeginTransaction();

        try
        {
            decimal totalCost = 0;

            foreach (var entry in goodsToBuy)
            {
                using SqlCommand command = new SqlCommand("SELECT * FROM Products WHERE StoreCode = @StoreCode AND Name = @ProductName AND Quantity >= @Quantity", connection, transaction);
                command.Parameters.AddWithValue("@StoreCode", storeCode);
                command.Parameters.AddWithValue("@ProductName", entry.Key);
                command.Parameters.AddWithValue("@Quantity", entry.Value);

                using SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    totalCost += (decimal)reader["Price"] * entry.Value;
                    reader.Close();

                    using SqlCommand updateCommand = new SqlCommand("UPDATE Products SET Quantity = Quantity - @Quantity WHERE StoreCode = @StoreCode AND Name = @ProductName", connection, transaction);
                    updateCommand.Parameters.AddWithValue("@StoreCode", storeCode);
                    updateCommand.Parameters.AddWithValue("@ProductName", entry.Key);
                    updateCommand.Parameters.AddWithValue("@Quantity", entry.Value);
                    updateCommand.ExecuteNonQuery();
                }
                else
                {
                    transaction.Rollback();
                    return -1;
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
        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        string query = "SELECT TOP 1 S.* FROM Stores S INNER JOIN Products P ON S.Code = P.StoreCode WHERE (";

        foreach (var entry in goodsToBuy)
        {
            query += $"(P.Name = '{entry.Key}' AND P.Quantity >= {entry.Value}) OR ";
        }

        query = query.Remove(query.Length - 4) + ") ORDER BY (";

        foreach (var entry in goodsToBuy)
        {
            query += $"P.Price * {entry.Value} + ";
        }

        query = query.Remove(query.Length - 2) + ") ASC";

        using SqlCommand command = new SqlCommand(query, connection);
        using SqlDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new Store
            {
                Code = (int)reader["Code"],
                Name = reader["Name"].ToString(),
                Address = reader["Address"].ToString()
            };
        }

        return null;
    }
}
