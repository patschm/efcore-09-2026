using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Domain.Aggregates;

public class ReviewUser
{
    private string _name = null!;

    public ReviewUserId Id { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Reviewer name is required.", nameof(value));

            _name = value;
        }
    }

    public string? Email { get; set; }

    private ReviewUser()
    {
    }

    public static ReviewUser Create(ReviewUserId id, string name, string? email = null) =>
        new()
        {
            Id = id,
            Name = name,
            Email = email
        };
}
