﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarketScanner.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarketScanner
{
    public class Jadro
    {
        private string url;
        private List<Symbols> ListSymbolov; 

        public Jadro(string url)
        {
            this.url = url;
        }

        public List<Symbols> LoadSymbols()
        {
            ListSymbolov = new List<Symbols>();
            var command = "ref-data/symbols";

            var result = CallApi(command);

            ListSymbolov = JsonConvert.DeserializeObject<List<Symbols>>(result);

            return ListSymbolov;
        }

        public void NacitajVsetkyDividendoveSpolocnosti()
        {
            int i = 0;
            var symbolsToLoad = "";
            foreach (var symbolse in ListSymbolov)
            {
                if (symbolse.symbol.Contains("#") || symbolse.symbol.Contains("-"))
                {
                    continue;
                }

                symbolsToLoad += symbolse.symbol;
                i++;

                if (i == 100)
                {
                    NacitajDividendoveSpolocnosti(symbolsToLoad);
                    i = 0;
                    symbolsToLoad = String.Empty;

                    Thread.Sleep(1100);
                }
                else
                {
                    symbolsToLoad += ",";
                }
            }
        }

        private void NacitajDividendoveSpolocnosti(string symbolsToLoad)
        {
            var command = string.Format("stock/market/batch?symbols={0}&types=quote,stats,dividends&range=3m&filter=symbol,changePercent,dividendRate,dividendYield,amount,exDate", symbolsToLoad);

            var result = CallApi(command);

            var jsonobj = JObject.Parse(result);
            foreach (var json in jsonobj.Values())
            {
                var stock = JsonConvert.DeserializeObject<DividendovyReport>(json.ToString());
                if (stock.Quote.changePercent != null)
                {
                    //if (
                    //    //Math.Abs((double)(stock.Quote.changePercent * 100)) > 5 
                    //    //&& 
                    //    stock.Dividends.Count > 0 && stock.Dividends.Last().amount > 1)
                    //{
                    //    Console.WriteLine("{0} : {1} , div. vynos - {2} , dividenda - {3}", stock.Quote.symbol, stock.Quote.changePercent * 100, stock.Stats.dividendYield, stock.Dividends.Count > 0 ? stock.Dividends.First().amount : 0.0);
                    //}

                    if (stock.Dividends.Any() && stock.Dividends.Last().exDate != null)
                    {
                        var predExDate = DateTime.ParseExact(stock.Dividends.Last().exDate, "yyyy-MM-dd",
                            CultureInfo.InvariantCulture);

                        var nasledExDate1 = predExDate.AddMonths(3);
                        var nasledExDate2 = predExDate.AddMonths(6);
                        var nasledExDate3 = predExDate.AddMonths(9);

                        Console.WriteLine("{0} : {1}  ->  {2}  ->  {3}", stock.Quote.symbol, nasledExDate1.ToLongDateString(), nasledExDate2.ToLongDateString(), nasledExDate3.ToLongDateString());
                    }
                }
            }
        }

        public void NacitajVsetky()
        {
            int i = 0;
            var symbolsToLoad = "";
            foreach (var symbolse in ListSymbolov)
            {
                if (symbolse.symbol.Contains("#") || symbolse.symbol.Contains("-"))
                {
                    continue;
                }

                symbolsToLoad += symbolse.symbol;
                i++;

                if (i == 100)
                {
                    NacitajSpolocnosti(symbolsToLoad);
                    i = 0;
                    symbolsToLoad = String.Empty;

                    Thread.Sleep(1100);
                }
                else
                {
                    symbolsToLoad += ",";
                }
            }
        }

        private void NacitajSpolocnosti(string symbolsToLoad)
        {
            var command = string.Format("stock/market/batch?symbols={0}&types=quote&filter=symbol,changePercent", symbolsToLoad);

            var result = CallApi(command);

            var jsonobj = JObject.Parse(result);
            foreach (var json in jsonobj.Values())
            {
                var stock = JsonConvert.DeserializeObject<Stock>(json.ToString());
                if (stock.Quote.changePercent != null)
                {
                    if (Math.Abs((double) (stock.Quote.changePercent * 100)) > 5)
                    {
                        Console.WriteLine(stock.Quote.symbol + " : " + stock.Quote.changePercent * 100);
                    }
                }
            }
        }

        private string CallApi(string command)
        {
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                //For IP-API
                client.BaseAddress = new Uri(url + command);
                HttpResponseMessage response = client.GetAsync(url + command).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }

                return null;
            }
        }

        public List<HistoryStockPrice> GetPiatkoveCeny(string symbol, bool vixVacsi, int hodnotaVix, DayOfWeek dayOfWeek = DayOfWeek.Friday)
        {
            var vixCeny = LoadVixHistoryStockPrice();

            var res = new List<HistoryStockPrice>();

            var command = string.Format($"stock/{symbol}/chart/1y");

            var result = CallApi(command);
            double stvrtokClose = 0;
            var jsonobj = JArray.Parse(result);
            for (int i = 0; i < jsonobj.Count; i++)
            {
                var stockCeny = JsonConvert.DeserializeObject<HistoryStockPrice>(jsonobj[i].ToString());
                if (DateTime.ParseExact(stockCeny.date, "yyyy-MM-dd", CultureInfo.InvariantCulture).DayOfWeek == dayOfWeek
                    && ((vixVacsi && vixCeny.Single(x => x.date == stockCeny.date).open > hodnotaVix) 
                    || (!vixVacsi && vixCeny.Single(x => x.date == stockCeny.date).open < hodnotaVix))
                    )  // 2018-11-19
                {
                    var stockCenyPredchadzajuci = JsonConvert.DeserializeObject<HistoryStockPrice>(jsonobj[i].ToString());
                    stockCeny.open = stockCenyPredchadzajuci.open;
                    res.Add(stockCeny);
                }
            }

            return res;
        }

        public Statistics GetStatistics(List<HistoryStockPrice> res, ListView lv)
        {
            var stats = new Statistics();

            foreach (var historyStockPrice in res)
            {
                var zmena = historyStockPrice.close - historyStockPrice.open;
                var zmenaPerc = Math.Abs(zmena) / historyStockPrice.open * 100;
                string[] row =
                {
                    historyStockPrice.date, historyStockPrice.open.ToString("F"),
                    historyStockPrice.close.ToString("F"), historyStockPrice.high.ToString("F"),
                    historyStockPrice.low.ToString("F"),
                    zmena.ToString("F"),
                    zmenaPerc.ToString("F") + " %"
                };
                var listViewItem = new ListViewItem(row);
                lv.Items.Add(listViewItem);

                var rozdiel = Math.Abs(historyStockPrice.close - historyStockPrice.open);

                if (rozdiel < 0.5)
                {
                    stats.PocetPod05++;
                }
                if (rozdiel >= 0.5 && rozdiel < 1)
                {
                    stats.Pocet05Az1++;
                }
                if (rozdiel >= 1 && rozdiel < 1.5)
                {
                    stats.Pocet1Az15++;
                }
                if (rozdiel >= 1.5 && rozdiel < 2.5)
                {
                    stats.Pocet15Az25++;
                }
                if (rozdiel >= 2.5)
                {
                    stats.Pocetnad25++;
                }

                if (zmenaPerc < 0.5)
                {
                    stats.PocetPod05Perc++;
                }
                if (zmenaPerc >= 0.5 && zmenaPerc < 1)
                {
                    stats.Pocet05Az1Perc++;
                }
                if (zmenaPerc >= 1 && zmenaPerc < 1.5)
                {
                    stats.Pocet1Az15Perc++;
                }
                if (zmenaPerc >= 1.5 && zmenaPerc < 2.5)
                {
                    stats.Pocet15Az25Perc++;
                }
                if (zmenaPerc >= 2.5)
                {
                    stats.Pocetnad25Perc++;
                }
            }

            return stats;
        }

        public Statistics GetStatistics(List<HistoryStockPrice> res)
        {
            var stats = new Statistics();

            foreach (var historyStockPrice in res)
            {
                var zmena = historyStockPrice.close - historyStockPrice.open;
                var zmenaPerc = Math.Abs(zmena) / historyStockPrice.open * 100;
                string[] row =
                {
                    historyStockPrice.date, historyStockPrice.open.ToString("F"),
                    historyStockPrice.close.ToString("F"), historyStockPrice.high.ToString("F"),
                    historyStockPrice.low.ToString("F"),
                    zmena.ToString("F"),
                    zmenaPerc.ToString("F") + " %"
                };

                var rozdiel = Math.Abs(historyStockPrice.close - historyStockPrice.open);

                if (rozdiel < 0.5)
                {
                    stats.PocetPod05++;
                }
                if (rozdiel >= 0.5 && rozdiel < 1)
                {
                    stats.Pocet05Az1++;
                }
                if (rozdiel >= 1 && rozdiel < 1.5)
                {
                    stats.Pocet1Az15++;
                }
                if (rozdiel >= 1.5 && rozdiel < 2.5)
                {
                    stats.Pocet15Az25++;
                }
                if (rozdiel >= 2.5)
                {
                    stats.Pocetnad25++;
                }

                if (zmenaPerc < 0.5)
                {
                    stats.PocetPod05Perc++;
                }
                if (zmenaPerc >= 0.5 && zmenaPerc < 1)
                {
                    stats.Pocet05Az1Perc++;
                }
                if (zmenaPerc >= 1 && zmenaPerc < 1.5)
                {
                    stats.Pocet1Az15Perc++;
                }
                if (zmenaPerc >= 1.5 && zmenaPerc < 2.5)
                {
                    stats.Pocet15Az25Perc++;
                }
                if (zmenaPerc >= 2.5)
                {
                    stats.Pocetnad25Perc++;
                }

                stats.Cena = historyStockPrice.close;
            }

            return stats;
        }

        public List<HistoryStockPrice> LoadVixHistoryStockPrice()
        {
            var result = new List<HistoryStockPrice>();

            var data = File.ReadAllLines("VIX.csv");
            foreach (var row in data.Skip(1))
            {
                var record = row.Split(',');
                var day = new HistoryStockPrice()
                {

                    date = record[0],
                    open = Convert.ToDouble(record[1], new CultureInfo("en-US")),
                    high = Convert.ToDouble(record[2], new CultureInfo("en-US")),
                    low = Convert.ToDouble(record[3], new CultureInfo("en-US")),
                    close = Convert.ToDouble(record[4], new CultureInfo("en-US"))
                };

                result.Add(day);
            }

            return result;
        }
    }
}
