namespace OrderFlow.Console.Models;

public class Customer
{
    public string Name { get; set; }
    public string City { get; set; }
    public bool IsVIP { get; set; }

    public Customer(string name, string city, bool isVIP)
    {
        Name = name;
        City = city;
        IsVIP = isVIP;
    }

    public override string ToString() => $"{Name} [{City}]{(IsVIP ? " *VIP*" : "")}";
}
