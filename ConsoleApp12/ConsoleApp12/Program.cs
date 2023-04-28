using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


class Program
{
    static async Task Main(string[] args)
    {
        // API'ye GET isteği gönderme ve başlama bitiş parametreleri belirlenmesi
        var url = "https://seffaflik.epias.com.tr/transparency/service/market/intra-day-trade-history";
        var start_date = "2022-01-26";
        var end_date = "2022-01-26";
        var query_params = new Dictionary<string, string>
        {
            { "startDate", start_date },
            { "endDate", end_date }
        };
        var query_string = string.Join("&", query_params.Select(p => $"{p.Key}={p.Value}"));
        var client = new HttpClient();
        var response = await client.GetAsync($"{url}?{query_string}");
        var response_string = await response.Content.ReadAsStringAsync();

        // API'den gelen verilerinin json formatını çevrilmesi
        var data = JsonConvert.DeserializeObject<dynamic>(response_string);

        // Conract değeri PH ile başlayan sınıfların listesi
        var ph_contracts = ((IEnumerable<dynamic>)data.body.intraDayTradeHistoryList)
            .Where(d => ((string)d.conract).StartsWith("PH"))
            .ToList();

        // Her bir conract değeri için istenilen değerlerin hesaplanması
        var results = new List<Dictionary<string, object>>();
        foreach (var contract in ph_contracts)
        {
            
            // Tarih değerini alma
            var conractString = (string)contract.conract;
            var year = conractString.Substring(2, 2);
            var month = conractString.Substring(4, 2);
            var day = conractString.Substring(6, 2);
            var hour = conractString.Substring(8, 2);
            var tarih = new DateTime(2000 + int.Parse(year), int.Parse(month), int.Parse(day), int.Parse(hour), 0, 0);

            // Toplam işlem tutarı hesaplama
            var total_trade_amount = ph_contracts
                .Where(d => ((string)d.conract) == ((string)contract.conract))
                .Sum(d => ((decimal)d.price * (decimal)d.quantity) / 10);

            // Toplam işlem miktarı hesaplama
            var total_trade_quantity = ph_contracts
                .Where(d => ((string)d.conract) == ((string)contract.conract))
                .Sum(d => (decimal)d.quantity / 10);

            // Ağırlıklı ortalama fiyat hesaplama
            var weighted_average_price = total_trade_amount / total_trade_quantity;

            // Sonuçları sözlüğe ekleme
            results.Add(new Dictionary<string, object>
            {
                { "Conract", (string)contract.conract },
                { "Tarih", tarih },
                { "Toplam İşlem Tutarı", total_trade_amount },
                { "Toplam İşlem Miktarı", total_trade_quantity },
                { "Ağırlıklı Ortalama Fiyat", weighted_average_price }
            });
        }


    var summary = results.GroupBy(r => r["Conract"])
    .Select(g => new Dictionary<string, object>
    {
        { "Conract", g.Key },
        { "Tarih", g.First()["Tarih"] },
        { "Toplam İşlem Tutarı", g.Sum(r => (decimal)r["Toplam İşlem Tutarı"]) },
        { "Toplam İşlem Miktarı", g.Sum(r => (decimal)r["Toplam İşlem Miktarı"]) },
        { "Ağırlıklı Ortalama Fiyat", g.Sum(r => (decimal)r["Toplam İşlem Tutarı"]) / g.Sum(r => (decimal)r["Toplam İşlem Miktarı"]) }
    })
    .ToList();

        Console.WriteLine("{0,-15} {1,-25} {2,-25} {3,-25} {4,-25}", "Conract", "Tarih", "Toplam İşlem Miktarı", "Toplam İşlem Tutarı", "Ağırlıklı Ortalama Fiyat");
        foreach (var s in summary)
        {
            Console.WriteLine("{0,-15} {1,-25} {2,-25:C} {3,-25} {4,-25:C}", s["Conract"], s["Tarih"], s["Toplam İşlem Miktarı"], s["Toplam İşlem Tutarı"], s["Ağırlıklı Ortalama Fiyat"]);

        }

    }
}


