using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MarketScanner.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarketScanner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var IEXTrading_API_URL = "https://api.iextrading.com/1.0/";

            Jadro jadro = new Jadro(IEXTrading_API_URL);
            jadro.LoadSymbols();
          //  jadro.NacitajVsetky();
            jadro.NacitajVsetkyDividendoveSpolocnosti();

            Console.ReadLine();

            var symbol = "msft,t";
            var IEXTrading_API_PATH = "https://api.iextrading.com/1.0/stock/market/batch?symbols=aapl,fb,tsla,t,gme&types=quote&filter=symbol,changePercent";
            //while (true)
            //{
            //    IEXTrading_API_PATH = string.Format(IEXTrading_API_PATH, symbol);

            //    using (HttpClient client = new HttpClient())
            //    {

            //        client.DefaultRequestHeaders.Accept.Clear();
            //        client.DefaultRequestHeaders.Accept.Add(
            //            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            //        //For IP-API
            //        client.BaseAddress = new Uri(IEXTrading_API_PATH);
            //        HttpResponseMessage response = client.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            //        if (response.IsSuccessStatusCode)
            //        {
            //            var responseString = response.Content.ReadAsStringAsync().Result;
            //            var jsonobj = JObject.Parse(responseString);

            //            foreach (var json in jsonobj.Values())
            //            {
            //                var stock = JsonConvert.DeserializeObject<Stock>(json.ToString());
            //                Console.WriteLine(stock.Quote.symbol +" - "+stock.Quote.changePercent*100);
            //            }

            //            //Console.WriteLine(response.Content.ReadAsStreamAsync().Result);
            //        }
            //        else
            //        {
            //            Console.WriteLine(response.StatusCode);
            //        }

            //        Thread.Sleep(10000);
            //    }
            //}
        }
    }

    
}