using Microsoft.EntityFrameworkCore;

namespace DemoComplexTypes;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ComplexDb;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";

    static void Main(string[] args)
    {
        //DemoComplex();
        DemoPrimitiveCollections();
    }

    private static void DemoPrimitiveCollections()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new MyContext(optionsBuilder.Options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var r1 = new Reviewer
        {
            Email = "hans@blah.nl",
            Name = "Hans",
            Credentials = new ReviewerCredential("Hans", "SHA1", "Salty"),
            AssignedNumbers = [1,2,3,4,5,6,7,8,9]
        };

        context.Reviewers.Add(r1);
        context.SaveChanges();

        var q1 = context.Reviewers.Where(x => x.AssignedNumbers.Contains(6));
        ShowData(q1.ToList());
    }

    private static void DemoComplex()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new MyContext(optionsBuilder.Options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var r1 = new Reviewer
        {
            Email = "hans@blah.nl",
            Name = "Hans",
            Credentials = new ReviewerCredential("Hans", "SHA1", "Salty")
        };
        var r2 = new Reviewer
        {
            Email = "mary@blah.nl",
            Name = "Mary",
            Credentials = new ReviewerCredential("Mary", "SHA1", "Salty")
        };
        var r3 = new Reviewer
        {
            Email = "justin@blah.nl",
            Name = "Justin",
            Credentials = new ReviewerCredential("Justin", "SHA1", "Salty")
        };

        context.Reviewers.AddRange(r1, r2, r3);
        context.SaveChanges();
        ShowData(context.Reviewers.ToList());
        
        // Modify
        var reviewer = context.Reviewers.FirstOrDefault();
        reviewer!.Credentials = reviewer.Credentials! with { PasswordHash = "SHA256" }; // Records are immutable by default so replace here
        context.SaveChanges();

        ShowData(context.Reviewers.ToList());
    }

    private static void ShowData(List<Reviewer> reviewers)
    {
        Console.WriteLine(new string('=', 40) );
        foreach (var r in reviewers)
        {
            Console.WriteLine($"[{r.Id}] {r.Name} ({r.Credentials?.UserName}, {r.Credentials?.PasswordHash} {r.Credentials?.PasswordSalt})");
        }
    }
}