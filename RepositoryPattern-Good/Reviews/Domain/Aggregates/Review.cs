using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Domain.Aggregates;

// ProductId is a reference into the Catalog bounded context by id only - Review does not
// navigate to Catalog.Product. ReviewUserId is nullable: anonymous reviews are allowed.
public class Review
{
    private string _type = null!;
    private decimal? _score;

    public ReviewId Id { get; private set; }
    public ProductId ProductId { get; private set; }

    public string Type
    {
        get => _type;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Review type is required.", nameof(value));

            _type = value;
        }
    }

    public string? Title { get; set; }
    public string? Text { get; set; }

    // 0-10, not 0-5: confirmed against the real data (min 1.00, max 10.00 across every scored
    // review) - a review score and a shop's star rating are different scales that happen to
    // share this project's history of being capped together; they don't share a real invariant.
    public decimal? Score
    {
        get => _score;
        set
        {
            if (value is < 0 or > 10)
                throw new ArgumentOutOfRangeException(nameof(value), "Score must be between 0 and 10.");

            _score = value;
        }
    }

    public ReviewUserId? ReviewUserId { get; private set; }
    public DateOnly CreationDate { get; private set; }

    private Review()
    {
    }

    public static Review Create(
        ReviewId id,
        ProductId productId,
        string type,
        DateOnly creationDate,
        string? title = null,
        string? text = null,
        decimal? score = null,
        ReviewUserId? reviewUserId = null) =>
        new()
        {
            Id = id,
            ProductId = productId,
            Type = type,
            CreationDate = creationDate,
            ReviewUserId = reviewUserId,
            Title = title,
            Text = text,
            Score = score
        };
}
