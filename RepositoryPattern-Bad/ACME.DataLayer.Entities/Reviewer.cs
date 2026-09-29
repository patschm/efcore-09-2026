namespace ACME.DataLayer.Entities;

public class Reviewer : Entity
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new HashSet<Review>();
}
