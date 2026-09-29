namespace DemoInheritance;

public abstract class Review
{
    public long Id { get; set; }
    public string? Text { get; set; }
    public byte Score { get; set; }
    public ReviewType ReviewType { get; set; }

    public long ReviewerId { get; set; }
    public Reviewer? Reviewer { get; set; }
}
