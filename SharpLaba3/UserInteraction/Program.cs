using Microsoft.Data.Sqlite;
using System;
using System.IO;

class Program
{

    private static (string dalType, string connectionString) GetConfiguration(string filePath)
    {
        string dalType = null;
        string connectionString = null;
        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            if (line.StartsWith("DALType:"))
            {
                dalType = line.Split(':')[1];
            }
            if (dalType == "SQLite" && line.StartsWith("SQLiteConnectionString:"))
            {
                connectionString = line.Split(':')[1];
            }
        }
        return (dalType, connectionString);
    }

    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                EnsureTableExists(connection, "Stores", GetStoresTableSchema());
                EnsureTableExists(connection, "Products", GetProductsTableSchema());
            }
        }

        private void EnsureTableExists(SqliteConnection connection, string tableName, string tableSchema)
        {
            var tableExists = TableExists(connection, tableName);
            if (!tableExists)
            {
                CreateTable(connection, tableSchema);
            }
        }

        private bool TableExists(SqliteConnection connection, string tableName)
        {
            string checkTableQuery = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
            using (var command = new SqliteCommand(checkTableQuery, connection))
            {
                var result = command.ExecuteScalar();
                return result != null && result.ToString() == tableName;
            }
        }

        private void CreateTable(SqliteConnection connection, string tableSchema)
        {
            using (var command = new SqliteCommand(tableSchema, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private string GetStoresTableSchema()
        {
            return @"
            CREATE TABLE Stores (
                Code INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Address TEXT NOT NULL
            );";
        }

        private string GetProductsTableSchema()
        {
            return @"
            CREATE TABLE Products (
                Name TEXT NOT NULL,
                StoreCode INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                Price REAL NOT NULL,
                PRIMARY KEY (Name, StoreCode),
                FOREIGN KEY (StoreCode) REFERENCES Stores (Code)
            );";
        }
    }

    static void Main()
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string propertyFilePath = Path.Combine(baseDirectory, "app.property");
        var (dalType, connectionString) = GetConfiguration(propertyFilePath);
        Console.WriteLine($"DAL Type: {dalType}");
        Console.WriteLine($"Connection String: {connectionString}");
        IDataAccessLayer dataAccessLayer;

        if (dalType == "CSV")
        {
            dataAccessLayer = new CsvFileDAL();
        }
        else if (dalType == "SQLite")
        {
            var databaseInitializer = new DatabaseInitializer(connectionString);
            databaseInitializer.InitializeDatabase();
            dataAccessLayer = new SqliteDatabaseDAL(connectionString);
        }
        else
        {
            throw new InvalidOperationException("Invalid DAL type specified in the property file.");
        }

        var storeService = new StoreService(dataAccessLayer);
        var consoleOperations = new ConsoleOperations(storeService);

        bool running = true;

        while (running)
        {
            Console.WriteLine("\nChoose an operation:");
            Console.WriteLine("1. Create a store");
            Console.WriteLine("2. Create a product");
            Console.WriteLine("3. Deliver a batch of products to the store");
            Console.WriteLine("4. Find the cheapest store for a product");
            Console.WriteLine("5. Find affordable products in a store");
            Console.WriteLine("6. Buy a batch of products in a store");
            Console.WriteLine("7. Find the cheapest store for a batch of products");
            Console.WriteLine("0. Exit");
            Console.Write("Enter your choice: ");

            int choice = Convert.ToInt32(Console.ReadLine());

            switch (choice)
            {
                case 1:
                    consoleOperations.CreateStoreFromConsole();
                    break;
                case 2:
                    consoleOperations.CreateProductFromConsole();
                    break;
                case 3:
                    consoleOperations.DeliverBatchToStoreFromConsole();
                    break;
                case 4:
                    consoleOperations.FindCheapestStoreForProductFromConsole();
                    break;
                case 5:
                    consoleOperations.FindAffordableProductsFromConsole();
                    break;
                case 6:
                    consoleOperations.PurchaseBatchFromConsole();
                    break;
                case 7:
                    consoleOperations.FindCheapestStoreForBatchFromConsole();
                    break;
                case 0:
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
    }
}
