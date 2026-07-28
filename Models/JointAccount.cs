namespace GastosApi.Models;

public class JointAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}