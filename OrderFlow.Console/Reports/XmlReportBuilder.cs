using System.Globalization;
using System.Xml.Linq;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Reports;

public class XmlReportBuilder
{
    public XDocument BuildReport(IEnumerable<Order> orders)
    {
        var list = orders.Select((o, i) => (Order: o, Id: i + 1)).ToList();

        var byStatus = list
            .GroupBy(x => x.Order.Status)
            .OrderBy(g => g.Key.ToString())
            .Select(g => new XElement("status",
                new XAttribute("name", g.Key.ToString()),
                new XAttribute("count", g.Count()),
                new XAttribute("revenue", g.Sum(x => x.Order.TotalAmount).ToString("F2", CultureInfo.InvariantCulture))));

        var byCustomer = list
            .GroupBy(x => x.Order.Customer.Name)
            .Select((g, ci) => new XElement("customer",
                new XAttribute("id", ci + 1),
                new XAttribute("name", g.Key),
                new XAttribute("isVip", g.First().Order.Customer.IsVIP.ToString().ToLower()),
                new XElement("orderCount", g.Count()),
                new XElement("totalSpent", g.Sum(x => x.Order.TotalAmount).ToString("F2", CultureInfo.InvariantCulture)),
                new XElement("orders",
                    g.Select(x => new XElement("orderRef",
                        new XAttribute("id", x.Id),
                        new XAttribute("total", x.Order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)))))));

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("report",
                new XAttribute("generated", DateTime.Now.ToString("s")),
                new XElement("summary",
                    new XAttribute("totalOrders", list.Count),
                    new XAttribute("totalRevenue", list.Sum(x => x.Order.TotalAmount).ToString("F2", CultureInfo.InvariantCulture))),
                new XElement("byStatus", byStatus),
                new XElement("byCustomer", byCustomer)));
    }

    public async Task SaveReportAsync(XDocument report, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await Task.Run(() => report.Save(stream));
    }

    public async Task<IEnumerable<int>> FindHighValueOrderIdsAsync(string reportPath, decimal threshold)
    {
        var text = await File.ReadAllTextAsync(reportPath);
        var doc  = XDocument.Parse(text);

        return doc.Descendants("orderRef")
            .Where(e => decimal.Parse(e.Attribute("total")!.Value, CultureInfo.InvariantCulture) > threshold)
            .Select(e => int.Parse(e.Attribute("id")!.Value))
            .ToList();
    }
}
