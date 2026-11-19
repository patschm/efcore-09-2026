using ACME.DataLayer.Entities;
using ACME.DataLayer.Repository.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;
using IsolationLevel = System.Data.IsolationLevel;

namespace ACME.Frontend.ConsoleClient;

internal class Program
{
    const string databaseName = "ProductCatalog";
    const string connectionString = @$"Server=.\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";

    static void Main()
    {
        Exercise1();
        //Exercise2();
    }
    private static void Exercise1()
    {
        var bld = new DbContextOptionsBuilder<ShopDatabaseContext>();
        bld.UseSqlServer(connectionString);
        bld.LogTo(Console.WriteLine, LogLevel.Information);
        var ctx = new ShopDatabaseContext(bld.Options);
        var id = 0L;
        var signal = new ManualResetEventSlim(false);
        Task.Run(() =>
        {
            var ctx2 = new ShopDatabaseContext(bld.Options);
            var brand = new Brand { Name = "SomeName", Website = "https://web.com" };
            ctx2.Brands.Add(brand);
            ctx2.SaveChanges();
            id = brand.Id;
            signal.Set();
            Task.Delay(400).Wait();
            brand = ctx2.Brands.First(p => p.Id == id);
            brand.Name = "Someone changed this";
            ctx2.SaveChanges();
        });

        signal.Wait();
        var brand = ctx.Brands.Find(id);
        Task.Delay(800).Wait();
        brand!.Name = "My Value";
        // TODO 1: Run this code and note that there's a concurrency conflict
        // TODO 2: Resolve the conflict by letting the user chose which one to keep: The database value or his value
        try
        {
            ctx.SaveChanges();
        }
        catch (DbUpdateConcurrencyException dbc)
        {
            foreach (var entry in dbc.Entries)
            {
                var curvalues = entry.CurrentValues;
                var dbvalues = entry.GetDatabaseValues()!;
                foreach (var prop in curvalues.Properties)
                {
                    if (curvalues[prop] != dbvalues[prop])
                    {
                        if (prop.Name == nameof(Entity.Id) || prop.Name == nameof(Entity.Timestamp)) continue;
                        Console.WriteLine($"Your value ({curvalues[prop]}) was changed by someone else ({dbvalues[prop]})");
                    }
                }
                Console.WriteLine("Override database values? (y/n)");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    entry.OriginalValues.SetValues(dbvalues);
                    ctx.SaveChanges();
                }
                else
                {
                    entry.OriginalValues.SetValues(dbvalues);
                    entry.CurrentValues.SetValues(dbvalues);
                }
            }
        }
    }
}