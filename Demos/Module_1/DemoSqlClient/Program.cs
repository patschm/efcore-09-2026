using Microsoft.Data.SqlClient;
using System.Data;

namespace DemoSqlClient;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ShopDatabase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";
    
    static void Main(string[] args)
    {
        ReadData();
        //InsertData();
        //UpdateData();
        //DeleteData();
    }

    private static void ReadData()
    {
        var connection = new SqlConnection(connectionString);
        connection.Open();
        Console.WriteLine($"{connection.State}");
        foreach(Brand brand in ReadBrandData(connection))
        {
            Console.WriteLine($"{brand.Name}");
            foreach(Product product in brand.Products.Take(5))
            {
                Console.WriteLine($"\t{product.Name}");
            }
        }   
    }

    private static IEnumerable<Brand> ReadBrandData(SqlConnection connection)
    {
        var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "SELECT * FROM Core.Brands";
        command.CommandType = CommandType.Text;
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var brand = new Brand
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Website = reader.GetString(2)
            };
            ReadProducts(connection, brand);
            yield return brand;
        }
    }
    private static void ReadProducts(SqlConnection connection, Brand brand)
    {
        var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "SELECT Id, Name, BrandId, Image FROM Core.Products WHERE Id=@id";
        command.CommandType = CommandType.Text;
        command.Parameters.AddWithValue("id", brand.Id);
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var product = new Product
            {
                Id = reader.GetInt64("Id"),
                Name = reader.GetString("Name"),
                Brand = brand, 
                BrandId = reader.GetInt64("BrandId"),
                Image = reader.GetString("Image")
            };
            brand.Products.Add(product);
        }
    }
    private static void InsertData()
    {
        Product p1 = new Product { BrandId = 1, Name = "Test", Image = "test.jpg" };
        var connection = new SqlConnection(connectionString);
        connection.Open();
        var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "INSERT INTO Core.Products (Name, BrandId, Image, ProductGroupId) VALUES (@name, @brandid, @image, 1)";
        command.Parameters.AddWithValue("name", p1.Name);
        command.Parameters.AddWithValue("brandid", p1.BrandId);
        command.Parameters.AddWithValue("image", p1.Image);
        command.ExecuteNonQuery();
    }
    private static void UpdateData()
    {
        Product p1 = new Product { BrandId = 1, Name = "Testig", Image = "test.jpg" };
        var connection = new SqlConnection(connectionString);
        connection.Open();
        var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "UPDATE Core.Products SET Name=@name WHERE Image=@image";
        command.Parameters.AddWithValue("name", p1.Name);
        command.Parameters.AddWithValue("image", p1.Image);
        command.ExecuteNonQuery();
    }
    private static void DeleteData()
    {
        Product p1 = new Product { BrandId = 1, Name = "Testig", Image = "test.jpg" };
        var connection = new SqlConnection(connectionString);
        connection.Open();
        var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "DELETE FROM Core.Products WHERE Image=@image";
        command.Parameters.AddWithValue("image", p1.Image);
        command.ExecuteNonQuery();
    }
}