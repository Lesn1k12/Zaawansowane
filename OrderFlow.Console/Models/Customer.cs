using System.ComponentModel.DataAnnotations.Schema;

namespace OrderFlow.Console.Models;

public class Customer
{
    public int Id { get; set; }

    // Kolumna mapowana przez EF – pełne imię i nazwisko
    public string FullName { get; set; } = string.Empty;

    // Alias zachowujący kompatybilność z istniejącym kodem; EF ignoruje tę właściwość
    [NotMapped]
    public string Name => FullName;

    public string City { get; set; } = string.Empty;
    public bool IsVIP { get; set; }
    public string? Email { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public Customer() { }

    public Customer(string name, string city, bool isVIP, string? email = null)
    {
        FullName = name;
        City = city;
        IsVIP = isVIP;
        Email = email;
    }

    public override string ToString() => $"{FullName} [{City}]{(IsVIP ? " *VIP*" : "")}";
}
