using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using System.Transactions;

namespace DemoTransactions;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ShopDatabase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=False";
    public static string backupConnectionString = @"Server=.\SQLEXPRESS;Database=ShopDatabaseBackup;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=False";
    
    static void Main(string[] args)
    {
        //LocalTransactions();
        //MultiContexts();
        //MixedLocalTransactions();
       //DistributedTransactions();
        //OutboxPattern();
        //SavePoints();
        Isolations();
    }

    private static void LocalTransactions()
    {
        // By default SaveChanges wraps all changes in a transaction
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;
        var context = new ProductContext(options);
        
        var brand1 = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
        var brand2 = new Brand { Name = "Brand 2", Website = "https://www.brand_2.nl" };//, Id=1 };

        using (var tran = context.Database.BeginTransaction())
        {
            context.Brands.Add(brand1);
            context.SaveChanges();
            context.Brands.Add(brand2);
            context.SaveChanges();

            tran.Commit(); // By not calling Commit() everything will be rolled back (Dispose())
        }

        // Clean up
        Console.WriteLine("Press enter to clean up");
        Console.ReadLine();
        context.RemoveRange(brand1);
        context.SaveChanges();
        context.RemoveRange(brand2);
        context.SaveChanges();
    }
    private static void MultiContexts()
    {
        // Every Context has it's own connection share the connection!
        var connection = new SqlConnection(connectionString);
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connection); // !!!!
        var options = optionsBuilder.Options;
        var context = new ProductContext(options);
        var context2 = new ProductContext(options);

        var brand1 = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
        var brand2 = new Brand { Name = "Brand 2", Website = "https://www.brand_2.nl" };//, Id=1 };

        using (var tran = context.Database.BeginTransaction())
        {
            context.Brands.Add(brand1);
            context.SaveChanges();

            context2.Database.UseTransaction(tran.GetDbTransaction());
            context2.Brands.Add(brand2);
            context2.SaveChanges();
            tran.Commit(); 
        }

        // Clean up
        Console.WriteLine("Press enter to clean up");
        Console.ReadLine();
        context.RemoveRange(brand1);
        context.SaveChanges();
        context.RemoveRange(brand2);
        context.SaveChanges();
    }
    private static void MixedLocalTransactions()
    {
        var connection = new SqlConnection(connectionString);
        connection.Open();
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connection); // !!!
        var context = new ProductContext(optionsBuilder.Options);

        var brand1 = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
        var brand2 = new Brand { Name = "Brand 2", Website = "https://www.brand_2.nl" };

        using (var tran = connection.BeginTransaction())
        {
            var command = new SqlCommand();
            command.Connection = connection;
            command.Transaction = tran;
            command.CommandText = "INSERT INTO Core.Brands (Name, WebSite) VALUES (@name, @web)";
            command.Parameters.AddWithValue("name", brand1.Name);
            command.Parameters.AddWithValue("web", brand1.Website);
            command.ExecuteNonQuery();

            context.Database.UseTransaction(tran);
            {
                context.Brands.Add(brand2);
                context.SaveChanges();
            }

            tran.Commit();
        }
        // Clean up
        Console.WriteLine("Press enter to clean up");
        Console.ReadLine();
        brand1 = context.Brands.First(b => b.Name == "Brand 1");
        context.RemoveRange(brand1);
        context.SaveChanges();
        context.RemoveRange(brand2);
        context.SaveChanges();
    }
    // MSDTC coordinates an atomic commit across the two independent databases below using two-phase
    // commit: 1) Prepare: MSDTC asks every enlisted database to do the work and durably promise it
    // can commit or roll back; each replies yes/no without actually committing yet. 2) Commit: only
    // if every participant voted yes does MSDTC tell them all to commit otherwise everyone rolls
    // back. This guarantees atomicity, but it's a blocking protocol: if MSDTC or a participant goes
    // down between the two phases, the others are left holding locks, "in doubt", until it recovers.
    // That blocking/availability risk is why most modern distributed systems avoid 2PC in favor of:
    // - Outbox pattern: write your business change AND an "event to publish" row in the SAME local
    //   transaction/database (a single, ordinary ACID commit), then a separate background process
    //   reads that outbox table and publishes the event elsewhere, retrying until it succeeds. No
    //   cross-database transaction is ever needed. Atomicity only has to hold locally.
    // - Saga pattern: split the overall operation into a sequence of local transactions, each in its
    //   own database/service, with a compensating action defined for each step (e.g. "cancel order"
    //   undoes "place order"). If a later step fails, previously completed steps are undone by running
    //   their compensations instead of a single all-or-nothing rollback.
    private static void DistributedTransactions()
    {
        // Cross-platform .NET originally lacked Distributed Transaction support since it would require
        // a different transaction manager on each platform. Since .NET 7, TransactionScope-based
        // distributed transactions (via MSDTC promotion) are supported again, but Windows-only -
        // which is why this project targets net10.0-windows in DemoTransactions.csproj.

        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new ProductContext(optionsBuilder.Options);

        var optionsBuilder2 = new DbContextOptionsBuilder();
        optionsBuilder2.UseSqlServer(backupConnectionString);
        var context2 = new ProductHistoryContext(optionsBuilder2.Options);
        context2.Database.EnsureCreated();

        var brand1 = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
        var brand2 = new Brand { Name = "Brand 2", Website = "https://www.brand_2.nl" };

        using (var transaction = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
        {
            context.Brands.Add(brand1);
            context.Brands.Add(brand2);
            context.SaveChanges();

            context2.Brands.Add(brand1);
            context2.Brands.Add(brand2);
            context2.SaveChanges();

            transaction.Complete();
        }

        // Clean up
        Console.WriteLine("Press enter to clean up");
        Console.ReadLine();
        context.RemoveRange(brand1);
        context.SaveChanges();
        context.RemoveRange(brand2);
        context.SaveChanges();
        context2.Database.EnsureDeleted();
    }
    // Outbox pattern demo, as an alternative to the 2PC-based DistributedTransactions() above: only
    // ONE database is ever touched inside a real transaction (ShopDatabase). The "please replicate
    // this to the backup database" intent is captured as a row in an outbox table, committed in the
    // SAME local transaction as the business data - an ordinary, single-database ACID commit. A
    // separate step (ProcessOutboxMessages, standing in for a background worker/relay) then reads
    // pending outbox rows and does the actual work against the second database, marking each row
    // processed only once that succeeds. No MSDTC, no cross-database transaction, no blocking - just
    // a local commit plus an at-least-once, retryable follow-up step. That follow-up step is itself
    // two separate local transactions (destination write, then mark-source-processed), so it pairs
    // Outbox with an "inbox" dedup marker on the destination (see ProcessOutboxMessages) to make a
    // replayed message a safe no-op instead of a duplicate write.
    private static void OutboxPattern()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new ProductContext(optionsBuilder.Options);

        var backupOptionsBuilder = new DbContextOptionsBuilder();
        backupOptionsBuilder.UseSqlServer(backupConnectionString);
        var backupContext = new ProductHistoryContext(backupOptionsBuilder.Options);
        backupContext.Database.EnsureCreated();

        // EnsureCreated() is a no-op on a database that already exists, so it won't add a new table
        // to ShopDatabase - create the outbox table directly instead, if it isn't there yet.
        context.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID('Core.OutboxMessages') IS NULL
            CREATE TABLE Core.OutboxMessages (
                Id BIGINT IDENTITY PRIMARY KEY,
                Type NVARCHAR(200) NOT NULL,
                Payload NVARCHAR(MAX) NOT NULL,
                CreatedAt DATETIME2 NOT NULL,
                ProcessedAt DATETIME2 NULL
            );");

        var brand1 = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
        var brand2 = new Brand { Name = "Brand 2", Website = "https://www.brand_2.nl" };

        // Step 1: the business change AND the "replicate this" event go into the SAME local
        // transaction, against the SAME database - an ordinary single-database ACID commit.
        context.Brands.AddRange(brand1, brand2);
        context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "BrandsCreated",
            Payload = JsonSerializer.Serialize(new[]
            {
                new BrandPayload(brand1.Name, brand1.Website),
                new BrandPayload(brand2.Name, brand2.Website)
            }),
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
        Console.WriteLine("Committed Brand 1/2 + outbox message to ShopDatabase in a single local transaction.");

        // Step 2: simulate the background relay. Reads unprocessed outbox rows and performs the
        // actual cross-database work. If this crashes before marking a row processed, it is simply
        // retried later; the source-of-truth write in Step 1 already safely committed regardless.
        ProcessOutboxMessages(context, backupContext);

        // Clean up
        Console.WriteLine("Press enter to clean up");
        Console.ReadLine();
        context.RemoveRange(brand1, brand2);
        context.SaveChanges();
        context.Database.ExecuteSqlRaw("DELETE FROM Core.OutboxMessages;");
        backupContext.Database.EnsureDeleted();
    }

    private static void ProcessOutboxMessages(ProductContext context, ProductHistoryContext backupContext)
    {
        var pending = context.OutboxMessages.Where(m => m.ProcessedAt == null).ToList();
        if (pending.Count == 0)
        {
            Console.WriteLine("No pending outbox messages.");
            return;
        }

        foreach (var message in pending)
        {
            // Writing the destination data and marking the SOURCE row processed are still two
            // separate local transactions on two separate databases. A crash between them would
            // replay this message. Guard against that by checking/recording an inbox marker in the
            // SAME local transaction as the destination write, so a replay of the same message
            // becomes a no-op instead of inserting a duplicate Brand.
            if (!backupContext.ProcessedOutboxMessages.Any(p => p.Id == message.Id))
            {
                var brands = JsonSerializer.Deserialize<BrandPayload[]>(message.Payload)!;
                foreach (var b in brands)
                {
                    backupContext.Brands.Add(new Brand { Name = b.Name, Website = b.Website });
                }
                backupContext.ProcessedOutboxMessages.Add(new ProcessedOutboxMessage { Id = message.Id, ProcessedAt = DateTime.UtcNow });
                backupContext.SaveChanges(); // destination write + inbox marker committed together, atomically, on ShopDatabaseBackup only
            }

            // Mark processed on the source side last. If THIS step fails/crashes, the message is
            // retried but the inbox check above now makes that retry safe rather than duplicating data.
            message.ProcessedAt = DateTime.UtcNow;
            context.SaveChanges();

            Console.WriteLine($"Processed outbox message {message.Id} ({message.Type}).");
        }
    }

    private record BrandPayload(string? Name, string? Website);

    private static void SavePoints()
    {
        // Savepoints are incompatible with SQL Server's Multiple Active Result Sets, and are not used.
        // If an error occurs during SaveChanges, the transaction may be left in an unknown state.
        string conString = @"Server=.\SQLEXPRESS;Database=ShopDatabase;Trusted_Connection=True;";
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(conString);
        var context = new ProductContext(optionsBuilder.Options);

        var brand1 = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
        var brand2 = new Brand { Name = "Brand 2", Website = "https://www.brand_2.nl", Id=1 };

        using var tran = context.Database.BeginTransaction();
        try
        {
            context.Brands.Add(brand1);
            context.SaveChanges();
            tran.CreateSavepoint("After_Brand_1");
            context.Brands.Add(brand2);
            context.SaveChanges();

            tran.Commit(); // By not calling Commit() everything will be rolled back (Dispose())
        }
        catch(Exception)
        {
            tran.RollbackToSavepoint("After_Brand_1");
            // Fix problem entity
            context.Remove(brand2);
            context.SaveChanges();
            tran.Commit();
        }

        // Clean up
        Console.WriteLine("Press enter to clean up");
        Console.ReadLine();
        context.RemoveRange(brand1);
        context.SaveChanges();
    }

    // Additional note:
    // SQL Server also has two versioning-based (non-locking) mechanisms worth knowing, on top of
    // the classic isolation levels:
    // - RCSI (Read Committed Snapshot Isolation): a database option that changes ReadCommitted to
    //   use row versioning instead of locks. Avoids dirty reads and reader/writer blocking, but
    //   only gives a consistent snapshot per statement, not per transaction. 
    // - SNAPSHOT isolation: a stronger, explicit isolation level giving one consistent snapshot for
    //   the WHOLE transaction, similar to Serializable's read guarantees without its locking cost. 
    //   Conflicts surface as an update-conflict error (3960) at write time instead of blocking.
    private static void Isolations()
    {
        // There might be 3 problems
        // 1) Dirty Reads. Reading uncommited data
        // 2) Non-repeatable reads. Same query returns different results
        // 3) Phantom Reads. New rows are added or removed by another transaction to the records being read.
        
        //DirtyReads();
        //NonRepeatableReads();
        PhantomReads();

        Console.ReadLine();
    }
    // Uses ReadUncommitted, which does NOT solve dirty reads. It's the isolation level that allows
    // them, used here to demonstrate the problem: reading another transaction's uncommitted (and
    // possibly-to-be-rolled-back) changes. ReadCommitted or higher prevents this.
    private static void DirtyReads()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context1 = new ProductContext(optionsBuilder.Options);
        var context2 = new ProductContext(optionsBuilder.Options);

        // Set the isolation level
        var isoLevel = System.Data.IsolationLevel.ReadUncommitted;

        using var readTransaction = context1.Database.BeginTransaction(isoLevel);
        using var writeTransaction = context2.Database.BeginTransaction();

        var writeAction = new Task(() => {
            var brand = new Brand { Name = "Brand 1", Website = "https://www.brand_2.nl" };
            context2.Brands.Add(brand);
            context2.SaveChanges();

            Console.WriteLine("Waiting 1 seconds for commit");
            Task.Delay(1000).Wait();

            writeTransaction.Commit();
        });

        var readData = Task.Run(() => {
            var query = context1.Brands;

            foreach (var b in query)
            {
                Console.WriteLine(b.Name);
            }
            
            writeAction.Start();
            Task.Delay(200).Wait();
            
            Console.WriteLine("==== Read again");
            foreach (var b in query)
            {
                Console.WriteLine(b.Name);
            }
            readTransaction.Commit();
        });

        Task.Delay(10000).Wait();
        var brand = context1.Brands.First(b => b.Name == "Brand 1");
        context1.Remove(brand);
        context1.SaveChanges();
    }
    // Uses ReadCommitted, which only guarantees you never read uncommitted data. It doesn't lock
    // already-read rows against being changed by another transaction before yours ends. Demonstrates
    // the non-repeatable read problem: the same query returns different values on a second read.
    // RepeatableRead or higher prevents this.
    private static void NonRepeatableReads()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context1 = new ProductContext(optionsBuilder.Options);
        var brand = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
        
        var context2 = new ProductContext(optionsBuilder.Options);
        context2.Brands.Add(brand);
        context2.SaveChanges();

        var modifyData = new Task(() => {
            using var writeTransaction = context2.Database.BeginTransaction();
            brand.Name = "Brand 2";
            context2.SaveChanges();
            writeTransaction.Commit();
        });

        var readData = Task.Run(() => {
            var isoLevel = System.Data.IsolationLevel.ReadCommitted;
            using var readTransaction = context1.Database.BeginTransaction(isoLevel);
            var query = context1.Brands.AsNoTracking();

            foreach (var b in query)
            {
                Console.WriteLine(b.Name);
            }
            modifyData.Start();

            Task.Delay(1000).Wait();
            Console.WriteLine("==== Read again");
            foreach (var b in query)
            {
                Console.WriteLine(b.Name);
            }
            readTransaction.Commit();
        });

        Task.Delay(5000).Wait();
        context2.Remove(brand);
        context2.SaveChanges();
    }
    // Uses RepeatableRead, which locks rows already read so they can't be changed mid-transaction 
    // but it doesn't take a range lock, so new rows matching the query's predicate can still be
    // inserted by another transaction and show up on a later read. Demonstrates the phantom read
    // problem. Only Serializable prevents this.
    private static void PhantomReads()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context1 = new ProductContext(optionsBuilder.Options);
        var context2 = new ProductContext(optionsBuilder.Options);
        
        var insertData = new Task(() => {
            using var writeTransaction = context2.Database.BeginTransaction();
            var brand = new Brand { Name = "Brand 1", Website = "https://www.brand_1.nl" };
            context2.Brands.Add(brand);
            context2.SaveChanges();
            writeTransaction.Commit();

            Task.Delay(5000).Wait();
            context2.Remove(brand);
            context2.SaveChanges();
        });

        var readData = Task.Run(() => {
            var isoLevel = System.Data.IsolationLevel.RepeatableRead;
            using var readTransaction = context1.Database.BeginTransaction(isoLevel);
            var query = context1.Brands.Where(b => b.Name!.StartsWith("Brand ")).AsNoTracking();

            foreach (var b in query)
            {
                Console.WriteLine(b.Name);
            }
            insertData.Start();

            Task.Delay(1000).Wait();
            Console.WriteLine("==== Read again");
            foreach (var b in query)
            {
                Console.WriteLine(b.Name);
            }
            readTransaction.Commit();
        });
    }
}