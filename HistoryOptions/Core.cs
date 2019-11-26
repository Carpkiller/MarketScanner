using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MarketScanner.Types;

namespace HistoryOptions
{
    public class Core
    {
        private List<BackTest> ListObchodov;

        public Core()
        {
        }

        public string PocitajStrategiu1(List<Option> optionData)
        {
            string result = "";
            
            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            foreach (var obchDen in obchodneDni)
            {   
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                //var najblizsiaExpiracia = optionData.Where(x => x.ExpirationDate >= obchDen.AddDays(10)).Min(x => x.ExpirationDate);
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[1]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any())
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((obchod.OpenPrice * 100) + (hodnota*100)) > obchod.OpenPrice * 100* (decimal)0.1)
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaStraddleBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade()
                        {
                            OpenDate = obchDen,
                            Strike = atmRow.Strike,
                            OpenPrice = hodnota,
                            Contract = atmRow.ExpirationDate.ToShortDateString(),
                            OpenStockPrice = atmRow.StockPrice,
                            ExpirationDate = atmRow.ExpirationDate
                        };

                        obchody.Add(obchodNew);
                    }
                }
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        private int GetDeltaStrike(List<Option> optionMatrix, double hodota = 0.5)
        {
            double previousDelta = 1;
            int index = 0;
            int i = 0;

            foreach (var option in optionMatrix.Where(x => x.Optiontype.ToUpper() == "CALL"))
            {
                //Console.WriteLine(Math.Abs(option.Delta));
                if (Math.Abs(Math.Abs(option.Delta) - hodota) < previousDelta)
                {
                    index = i;
                    previousDelta = Math.Abs(Math.Abs(option.Delta) - hodota);
                }

                i++;
            }

            return index;
        }

        public decimal GetCena(List<Option> optionMatrix, DateTime datum, string typ, DateTime? expDatum, double? strike, string cenaAkcie)
        {
            if (typ == "AKCIE")
            {
                return decimal.Parse(cenaAkcie);
            }

            return (optionMatrix.Where(x => x.Optiontype.ToUpper() == typ).Where(y => y.ExpirationDate == expDatum).
                Where(c => c.Strike == strike).Where(d => d.QuoteDate <= datum).OrderByDescending(w => w.QuoteDate).First().Bid +
                    optionMatrix.Where(x => x.Optiontype.ToUpper() == typ).Where(y => y.ExpirationDate == expDatum).
                        Where(c => c.Strike == strike).Where(d => d.QuoteDate <= datum).OrderByDescending(w => w.QuoteDate).First().Ask) / 2;
        }

        private List<OptionMatrixRow> GetOptionMatrix(List<Option> data)
        {
            var res = new List<OptionMatrixRow>();
            foreach (var option in data.Where(x => x.Optiontype.ToUpper() == "CALL"))
            {
                OptionMatrixRow row = new OptionMatrixRow()
                {
                    QuoteDate = option.QuoteDate,
                    ExpirationDate = option.ExpirationDate,
                    Optionsymbol = option.Optionsymbol,
                    StockPrice = option.StockPrice,
                    Call = new Option
                    {
                        OpenInterest = option.OpenInterest,
                        Volume = option.Volume,
                        Delta = option.Delta,
                        Bid = option.Bid,
                        Ask = option.Ask,
                    },
                    Strike = option.Strike,
                    Put = new Option
                    {
                        Bid =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Bid,
                        Ask =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Ask,
                        Delta =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Delta,
                        Volume =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Volume,
                        OpenInterest =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").OpenInterest
                    }
                };

                res.Add(row);
            }

            return res;
        }

        // vo stvrtok predat ATM straddle a v piatok obchod ukoncit
        public string PocitajStrategiu2(List<Option> optionData)
        {
            string result = "";
            //var vixHodnoty = LoadVixHistoryStockPrice();

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            for (int i = 0; i < obchodneDni.Count-1; i++)
            {
                var obchDen = obchodneDni[i];
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[0]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (obchDen.DayOfWeek == DayOfWeek.Thursday && obchodneDni[i+1].DayOfWeek == DayOfWeek.Friday 
                 //   && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < 25
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
                    var obchod = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Any() && obchDen.DayOfWeek == DayOfWeek.Friday && obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    // var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(optionMatrix, obchod.Strike);

                    obchod.CloseDate = obchDen;
                    obchod.ClosePrice = hodnota;
                    obchod.CloseStockPrice = atmRow.StockPrice;
                }
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        public string PocitajStrategiu3(List<Option> optionData)
        {
            string result = "";
            var vixHodnoty = LoadVixHistoryStockPrice();

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            for (int i = 0; i < obchodneDni.Count; i++)
            {
                var obchDen = obchodneDni[i];
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[1]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (obchDen.DayOfWeek == DayOfWeek.Friday 
                    //&& vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < 13
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
                    var obchod = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Any() && obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(obchodOptionMatrix, obchod.Strike);

                    obchod.CloseDate = obchDen;
                    obchod.ClosePrice = hodnota;
                    obchod.CloseStockPrice = atmRow.StockPrice;
                }
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        internal IEnumerable<BackTest> GetObchody()
        {
            return ListObchodov;
        }

        internal void PridajObchod(string typ, string strana, DateTime startDate, string expiracia,
            ListViewItem.ListViewSubItemCollection subItems)
        {
            if (ListObchodov == null)
            {
                ListObchodov = new List<BackTest>();
            }

            var obchod = new BackTest()
            {
                Strike = double.Parse(subItems[5].Text),
                Delta = double.Parse(subItems[2].Text),
                Price = typ == "CALL"
                    ? (decimal.Parse(subItems[3].Text) +
                       decimal.Parse(subItems[4].Text)) / 2
                    : (decimal.Parse(subItems[6].Text) +
                       decimal.Parse(subItems[7].Text)) / 2,
                StartDate = startDate,
                ExpirationDate = DateTime.Parse(expiracia),
                Optiontype = typ,
                Ukonceny = false
            };

            if (strana == "BUY")
            {
                obchod.Price *= -1;
            }

            ListObchodov.Add(obchod);
        }

        internal void PridajObchod(string typ, string strana, DateTime startDate, string cena)
        {
            if (ListObchodov == null)
            {
                ListObchodov = new List<BackTest>();
            }

            if (strana == "SELL" && ListObchodov.Any(x => x.Optiontype == "AKCIE" && x.Price < 0 && x.Ukonceny == false))
            {
                var aktObchod = ListObchodov.Single(x => x.Optiontype == "AKCIE" && x.Price < 0 && x.Ukonceny == false);
                aktObchod.Profit = GetZiskStrata(aktObchod, cena);
                aktObchod.Ukonceny = true;

                return;
            }

            if (strana == "BUY" && ListObchodov.Any(x => x.Optiontype == "AKCIE" && x.Price > 0 && x.Ukonceny == false))
            {
                var aktObchod = ListObchodov.Single(x => x.Optiontype == "AKCIE" && x.Price > 0 && x.Ukonceny == false);
                aktObchod.Profit = GetZiskStrata(aktObchod, cena);
                aktObchod.Ukonceny = true;

                return;
            }

            var obchod = new BackTest()
            {
                Price = decimal.Parse(cena),
                StartDate = startDate,
                Optiontype = typ,
                Ukonceny = false,
                Strike = null
            };

            if (strana == "BUY")
            {
                obchod.Price *= -1;
            }

            ListObchodov.Add(obchod);
        }

        internal void UkonciOpcnyObchod(string typOpcie, string strike, string cena)
        {
            var opcia = ListObchodov.Where(x => x.Optiontype == typOpcie && x.Strike == double.Parse(strike) && x.Price == decimal.Parse(cena))
                .Single();

            opcia.Ukonceny = true;

            opcia.Profit = opcia.Price * 100;

        }

        private decimal GetZiskStrata(BackTest obchod, string cena)
        {
            if (obchod.Price > 0)
            {
                obchod.Profit = (obchod.Price - decimal.Parse(cena)) * 100;
            }
            else
            {
                obchod.Profit = (obchod.Price + decimal.Parse(cena)) * 100;
            }

            return obchod.Profit;
        }

        public decimal GetZiskStrata(List<Option> optionData, BackTest obchod, DateTime datum, string cenaAkcie)
        {
            if (obchod.Price > 0)
            {
                obchod.Profit = (obchod.Price - GetCena(optionData, datum,
                                     obchod.Optiontype,
                                     obchod.ExpirationDate, obchod.Strike, cenaAkcie)) * 100;
            }
            else
            {
                obchod.Profit = (obchod.Price + GetCena(optionData, datum, obchod.Optiontype,
                                     obchod.ExpirationDate, obchod.Strike, cenaAkcie)) * 100;
            }

            if (obchod.Strike != null & obchod.ExpirationDate <= datum)
            {
                obchod.Ukonceny = true;
            }

            return obchod.Profit;
        }

        internal void UlozObchody(string nazovSuboru, DateTime aktualnyDatum)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream($"{nazovSuboru}", FileMode.Create, FileAccess.Write);

            formatter.Serialize(stream, new UlozeneObchody { aktualnyDatum = aktualnyDatum, ListObchodov = ListObchodov});
            stream.Close();
        }

        internal DateTime NacitajObchody(string nazovSuboru)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream($"{nazovSuboru}", FileMode.Open, FileAccess.Read);
            var ulozeneObchody = (UlozeneObchody)formatter.Deserialize(stream);

            ListObchodov = ulozeneObchody.ListObchodov;

            return ulozeneObchody.aktualnyDatum;
        }

        internal void VymazObchody()
        {
            ListObchodov.Clear();
        }

        public List<HistoryStockPrice> LoadVixHistoryStockPrice()
        {
            var result = new List<HistoryStockPrice>();

            var data = File.ReadAllLines("VIX.txt");
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

        internal string PocitajStrategiuDeltaNeutral(List<Option> optionData)
        {
            string result = "";

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            bool otvoreny = false;
            double prvyStrike = 100;
            double pocetKusov = 0;
            double pocetOpcii = 3;
            DateTime? expiracnyDen = null;

            for (int i = 0; i < obchodneDni.Count; i++)
            {
                var obchDen = obchodneDni[i];
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();

                if (!otvoreny)
                {
                    expiracnyDen = expirations[2];
                }

                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expiracnyDen).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                //Console.WriteLine(otvoreny ? $"Aktualna delta portfolia {Math.Abs(GetDelta(data, prvyStrike, "PUT") * 300 + (double)pocetKusov)}" : null);

                if (!otvoreny)
                {
                    prvyStrike = atmRow.Strike;
                    otvoreny = true;
                    expiracnyDen = atmRow.ExpirationDate;

                    var hodnotaPut = MarketStrategies.GetHodnotaOptionPutBuy(optionMatrix, atmRow.Strike);
                    var obchodPutStrana = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = prvyStrike,
                        OpenPrice = hodnotaPut,
                        Contract = "PUT",
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate,
                        PocetKontraktov = (decimal)pocetOpcii
                    };

                    var delta = GetDelta(data, atmRow.Strike, "PUT") * (double)obchodPutStrana.PocetKontraktov;
                    pocetKusov = Math.Round(delta * 100)*(-1);
                    var obchodAkcia = new Trade()
                    {
                        OpenDate = obchDen,
                        OpenPrice = atmRow.StockPrice,
                        Contract = "AKCIE",
                        ExpirationDate = atmRow.ExpirationDate,
                        OpenStockPrice = atmRow.StockPrice,
                        PocetKontraktov = (decimal)pocetKusov
                    };


                    result += $"{obchDen.ToShortDateString()}  Nakupenie PUT na Strike {prvyStrike} , cena Put {hodnotaPut}";
                    result += Environment.NewLine;
                    result += $"{obchDen.ToShortDateString()}  Nakupenie akcii na cene {atmRow.StockPrice} pocet kusov {pocetKusov}";
                    result += Environment.NewLine;
                    
                    obchody.Add(obchodAkcia);
                    obchody.Add(obchodPutStrana);
                }
                else if (otvoreny && Math.Abs(GetDelta(data, prvyStrike, "PUT")*100* pocetOpcii + (double)pocetKusov) > 10)
                {
                    var obchod = obchody.Last();

                    if (GetDelta(data, prvyStrike, "PUT")*100* pocetOpcii + (double)pocetKusov < 0)
                    {
                        var prikupitAkcii =
                            (Math.Round(GetDelta(data, prvyStrike, "PUT") * 100 * pocetOpcii + (double) pocetKusov)) * (-1);

                        result += $"{obchDen.ToShortDateString()}  Prikupenie akcii({prikupitAkcii}) pri cene {atmRow.StockPrice} " +
                                  $"aktualna delta {(Math.Round(GetDelta(data, prvyStrike, "PUT") * 100 + (double)pocetKusov))}";
                        result += Environment.NewLine;

                        pocetKusov += prikupitAkcii;

                        var obchodAkcia = new Trade()
                        {
                            OpenDate = obchDen,
                            OpenPrice = atmRow.StockPrice,
                            Contract = "AKCIE",
                            ExpirationDate = atmRow.ExpirationDate,
                            OpenStockPrice = atmRow.StockPrice,
                            PocetKontraktov = (decimal)prikupitAkcii
                        };

                        obchody.Add(obchodAkcia);
                        

                    }
                    else if (GetDelta(data, prvyStrike, "PUT") * 100 * pocetOpcii + (double)pocetKusov > 0)
                    {
                        var predatAkcii =
                            (Math.Round(GetDelta(data, prvyStrike, "PUT") * 100 * pocetOpcii + (double)pocetKusov)) * (-1);

                        result += $"{obchDen.ToShortDateString()}  Predanie akcii({predatAkcii}) pri cene {atmRow.StockPrice} " +
                                  $"aktualna delta {(Math.Round(GetDelta(data, prvyStrike, "PUT") * 100 + (double)pocetKusov))}";
                        result += Environment.NewLine;

                        pocetKusov += predatAkcii;

                        var obchodAkcia = new Trade()
                        {
                            OpenDate = obchDen,
                            OpenPrice = atmRow.StockPrice,
                            Contract = "AKCIE",
                            ExpirationDate = atmRow.ExpirationDate,
                            OpenStockPrice = atmRow.StockPrice,
                            PocetKontraktov = (decimal)predatAkcii
                        };

                        obchody.Add(obchodAkcia);
                    }
                }

                if (otvoreny && obchDen == expiracnyDen || obchDen == obchodneDni.Last() || obchodneDni[i+1] > expiracnyDen)
                {
                    result += $"{obchDen.ToShortDateString()}  Ukoncenie obchodu , cena akcie {atmRow.StockPrice}";
                    result += Environment.NewLine;

                    decimal totalProfit = 0;

                    foreach (var obchod in obchody.Where(x => x.CloseDate == null))
                    {
                        if (obchod.Contract == "PUT")
                        {
                            var hodnotaPut = MarketStrategies.GetHodnotaOptionPutSell(optionMatrix, obchod.Strike);

                            totalProfit += (obchod.OpenPrice + hodnotaPut)*100* (decimal)pocetOpcii*(-1);
                            obchod.ClosePrice = hodnotaPut;
                        }
                        else
                        {
                            totalProfit += (obchod.OpenPrice - atmRow.StockPrice) * obchod.PocetKontraktov*(-1);
                            obchod.ClosePrice = atmRow.StockPrice;
                        }

                        obchod.CloseDate = obchDen;
                        obchod.CloseStockPrice = atmRow.StockPrice;
                    }

                    result += $"Total profit {totalProfit}";
                    result += Environment.NewLine;
                    result += $"-----------------------------";
                    result += Environment.NewLine;

                    otvoreny = false;
                }

                result += $"{obchDen.ToShortDateString()}  Priebezny P/L({atmRow.StockPrice}({PocitajPocetAkcii(obchody)}))   -  {PocitajPriebeznyProfit(obchody, optionMatrix, (decimal)pocetOpcii)}";
                result += Environment.NewLine;
            }

            //result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        private decimal PocitajPocetAkcii(List<Trade> obchody)
        {
            decimal pocetAkcii = 0;

            foreach (var trade in obchody.Where(x => x.CloseDate == null))
            {
                pocetAkcii += trade.PocetKontraktov;
            }

            return pocetAkcii;
        }

        private decimal PocitajPriebeznyProfit(List<Trade> obchody, List<OptionMatrixRow> optionMatrix, decimal pocetOpcii)
        {
            decimal totalProfit = 0;

            foreach (var obchod in obchody.Where(x => x.CloseDate == null))
            {
                if (obchod.Contract == "PUT")
                {
                    var hodnotaPut = MarketStrategies.GetHodnotaOptionPutSell(optionMatrix, obchod.Strike);

                    totalProfit += (obchod.OpenPrice + hodnotaPut) * 100 * pocetOpcii * (-1);
                }
                else
                {
                    totalProfit += (obchod.OpenPrice - optionMatrix.First().StockPrice) * obchod.PocetKontraktov * (-1);
                }
            }

            return totalProfit;
        }

        private double GetDelta(List<Option> data, double strike, string type)
        {
            return data.Single(x => x.Optiontype.ToUpper() == type && x.Strike == strike).Delta;
        }

        internal string PocitajStrategiuButterfly(List<Option> optionData)
        {
            string result = "";

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            bool otvoreny = false;
            double prvyStrike = 100;

            short[] poleHodnotCall = new short[] { 2, 3, 5, -1, -1, -2 };
            short[] poleHodnotPut = new short[] { -2, -3, -5, 1, 1, 2 };

            for (int i = 0; i < obchodneDni.Count; i++)
            {
                var obchDen = obchodneDni[i];
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[1]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (!otvoreny && obchDen.DayOfWeek == DayOfWeek.Monday && (!obchody.Any() || obchody.Last().CloseDate != null))
                {
                    prvyStrike = atmRow.Strike;
                    var hodnotaCall = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                    var obchodCallStrana = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = prvyStrike,
                        OpenPrice = hodnotaCall,
                        Contract = "CALL",
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    var hodnotaPut = MarketStrategies.GetHodnotaOptionPutBuy(optionMatrix, atmRow.Strike);
                    var obchodPutStrana = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = prvyStrike,
                        OpenPrice = hodnotaPut,
                        Contract = "PUT",
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    result += $"{obchDen.ToShortDateString()}  Otvorenie obchodu na Strike {prvyStrike} , cena akcie {atmRow.StockPrice} za hodnotu Call {hodnotaCall} a Put {hodnotaPut}";
                    result += Environment.NewLine;
                    obchody.Add(obchodCallStrana);
                    obchody.Add(obchodPutStrana);

                }
                else if (!otvoreny && obchody.Any() && obchody.Last().CloseDate == null && Math.Abs(prvyStrike - double.Parse(atmRow.StockPrice.ToString())) > 6)
                {
                    result += $"{obchDen.ToShortDateString()}  Dotvorenie butterfly kombinacie , cena akcie {atmRow.StockPrice}";
                    result += Environment.NewLine;

                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());

                    var striky = obchodOptionMatrix.Select(x => x.Strike).Distinct().ToList();

                    if (double.Parse(atmRow.StockPrice.ToString()) > prvyStrike)
                    {
                        result += $"{obchDen.ToShortDateString()}  Dokupujem CALL opcie na tychto strikov {striky.ElementAt(striky.IndexOf(prvyStrike)+ poleHodnotCall[0])}, " +
                            $"{striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotCall[1])} a {striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotCall[2])}";
                        result += Environment.NewLine;
                        PridajObchody(obchody, obchodOptionMatrix, "CALL", striky.ElementAt(striky.IndexOf(prvyStrike)+ poleHodnotCall[0]), 
                            striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotCall[1]), striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotCall[2]), obchDen);

                        result += $"{obchDen.ToShortDateString()}  Dokupujem PUT opcie na tychto strikov {striky.ElementAt(striky.IndexOf(prvyStrike) - poleHodnotCall[3])}, " +
                            $"{striky.ElementAt(striky.IndexOf(prvyStrike) - poleHodnotCall[4])} a {striky.ElementAt(striky.IndexOf(prvyStrike) - poleHodnotCall[5])}";
                        result += Environment.NewLine;

                        PridajObchody(obchody, obchodOptionMatrix, "PUT", striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotCall[3]),
                            striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotCall[4]), striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotCall[5]), obchDen);
                    }

                    if (double.Parse(atmRow.StockPrice.ToString()) < prvyStrike)
                    {
                        result += $"{obchDen.ToShortDateString()}  Dokupujem PUT opcie na tychto strikov {striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[0])}, " +
                            $"{striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[1])} a {striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[2])}";
                        result += Environment.NewLine;
                        PridajObchody(obchody, obchodOptionMatrix, "PUT", striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[0]),
                            striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[1]), striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[2]), obchDen);

                        result += $"{obchDen.ToShortDateString()}  Dokupujem CALL opcie na tychto strikov {striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[3])}, " +
                            $"{striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[4])} a {striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[5])}";
                        result += Environment.NewLine;
                        PridajObchody(obchody, obchodOptionMatrix, "CALL", striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[3]),
                            striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[4]), striky.ElementAt(striky.IndexOf(prvyStrike) + poleHodnotPut[5]), obchDen);
                    }

                    otvoreny = true;
                }
                if (obchody.Any() && obchody.Last().CloseDate == null && obchody.Last().ExpirationDate <= obchDen)
                {
                    result += $"{obchDen.ToShortDateString()}  Ukoncenie obchodu";
                    result += Environment.NewLine;

                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchody.Last().ExpirationDate).ToList());
                    

                    foreach (var obchod in obchody.Where(x => x.CloseDate == null))
                    {
                        var hodnota = obchod.Contract == "CALL" 
                            ? MarketStrategies.GetHodnotaOptionCallBuy(obchodOptionMatrix, obchod.Strike) 
                            : MarketStrategies.GetHodnotaOptionPutBuy(obchodOptionMatrix, obchod.Strike);

                        if (obchod.OpenPrice > 0)
                        {
                            hodnota *= -1;
                        }

                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        result += $"{obchDen.ToShortDateString()}  Ukoncenie {obchod.Contract} opcie na striku {obchod.Strike} otvaracia cena {obchod.OpenPrice} a zatvaracia {obchod.ClosePrice}";
                        result += Environment.NewLine;
                    }
                    
                    otvoreny = false;
                }
            }

            //result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        private void PridajObchody(List<Trade> obchody, List<OptionMatrixRow> obchodOptionMatrix, string typ, double v1, double v2, double v3, DateTime obchDen)
        {
            var hodnotav1 = typ == "CALL" ? MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, v1) : MarketStrategies.GetHodnotaOptionPutSell(obchodOptionMatrix, v1);
            var obchodV1 = new Trade()
            {
                OpenDate = obchDen,
                Strike = v1,
                OpenPrice = hodnotav1,
                Contract = typ,
                //   OpenStockPrice = atmRow.StockPrice,
                ExpirationDate = obchodOptionMatrix.First().ExpirationDate
            };
            obchody.Add(obchodV1);

            var hodnotav2 = typ == "CALL" ? MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, v2) : MarketStrategies.GetHodnotaOptionPutSell(obchodOptionMatrix, v2);
            var obchodV2 = new Trade()
            {
                OpenDate = obchDen,
                Strike = v2,
                OpenPrice = hodnotav2,
                Contract = typ,
                //   OpenStockPrice = atmRow.StockPrice,
                ExpirationDate = obchodOptionMatrix.First().ExpirationDate
            };
            obchody.Add(obchodV2);

            var hodnotav3 = typ == "CALL" ? MarketStrategies.GetHodnotaOptionCallBuy(obchodOptionMatrix, v3) : MarketStrategies.GetHodnotaOptionPutBuy(obchodOptionMatrix, v3);
            var obchodV3 = new Trade()
            {
                OpenDate = obchDen,
                Strike = v3,
                OpenPrice = hodnotav3,
                Contract = typ,
                //   OpenStockPrice = atmRow.StockPrice,
                ExpirationDate = obchodOptionMatrix.First().ExpirationDate
            };
            obchody.Add(obchodV3);
        }

        internal void OdstranObchod(int index)
        {
            ListObchodov.RemoveAt(index);
        }

        internal void OdstranObchod(string typOpcie, string strike, string cena)
        {
            var opcia = ListObchodov.Where(x => x.Optiontype == typOpcie && x.Strike == double.Parse(strike) && x.Price == decimal.Parse(cena))
                .First();

            ListObchodov.Remove(opcia);
        }

        public string PocitajStrategiu4(List<Option> optionData)
        {
            string result = "";
            var vixHodnoty = LoadVixHistoryStockPrice();
            int VixMax = 14;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[0]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any() || obchody.Last().CloseDate != null
                    && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
                    var obchod = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Last().CloseDate == null)
                {
                    if (obchDen.Date.ToShortDateString() == new DateTime(2018, 08, 20).ToShortDateString())
                    {
                        
                    }
                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((obchod.OpenPrice * 100) + (hodnota * 100)) > obchod.OpenPrice * 100 * (decimal)0.1 
                        || IsLastTradingDayOfContract(optionData, obchod.ExpirationDate, obchDen))
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        if (obchDen.DayOfWeek != DayOfWeek.Friday
                            && !IsLastTradingDayOfContract(optionData, obchod.ExpirationDate, obchDen)
                          //  && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax
                            )
                        {
                            hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
                            var obchodNew = new Trade
                            {
                                OpenDate = obchDen,
                                Strike = atmRow.Strike,
                                OpenPrice = hodnota,
                                Contract = atmRow.ExpirationDate.ToShortDateString(),
                                OpenStockPrice = atmRow.StockPrice,
                                ExpirationDate = atmRow.ExpirationDate
                            };

                            obchody.Add(obchodNew);
                        }
                        else
                        {
                            
                        }
                    }
                }
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        private bool IsLastTradingDayOfContract(List<Option> optionData, DateTime expirationDate, DateTime obchDen)
        {
            if (obchDen.Date.ToShortDateString() == new DateTime(2018, 08, 17,0,0,0).ToShortDateString())
            {

            }
            var lastTradingDate = optionData.Where(x => x.ExpirationDate == expirationDate).Select(x => x.QuoteDate).Max();

            return lastTradingDate == obchDen;
        }

        public string PocitajStrategiu5(List<Option> optionData)
        {
            string result = "";
            var vixHodnoty = LoadVixHistoryStockPrice();
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[6]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x =>  x.Optiontype.ToUpper() == "PUT").ToList(), 0.25);
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any() || obchody.Last().CloseDate != null
                 //   && RozdielCeny(obchDen, predDen, optionData)
                    && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionPutSell(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaOptionPutBuy(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((obchod.OpenPrice * 100) + (hodnota * 100)) > obchod.OpenPrice * 100 * (decimal)0.5 *(-1))
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        if (
                            //RozdielCeny(obchDen, predDen, optionData)
                            //&& 
                            vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax)
                        {
                            hodnota = MarketStrategies.GetHodnotaOptionPutSell(optionMatrix, atmRow.Strike);
                            var obchodNew = new Trade
                            {
                                OpenDate = obchDen,
                                Strike = atmRow.Strike,
                                OpenPrice = hodnota,
                                Contract = atmRow.ExpirationDate.ToShortDateString(),
                                OpenStockPrice = atmRow.StockPrice,
                                ExpirationDate = atmRow.ExpirationDate
                            };

                            obchody.Add(obchodNew);
                        }
                    }
                }

                predDen = obchDen;
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        private bool RozdielCeny(DateTime obchDen, DateTime? predchDen, List<Option> optionData)
        {
            if (predchDen == null)
            {
                return false;
            }

            var predDen = optionData.Where(x => x.QuoteDate == predchDen).Select(x => x.StockPrice).Distinct().Single();

            var sucDen = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.StockPrice).Distinct().Single();

            if (Math.Abs((predDen - sucDen) / sucDen) > (decimal)0.015)
            {
                return true;
            }

            return false;
        }

        public string PocitajStrategiuBuyCallAtm(List<Option> optionData)
        {
            string result = "";
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[2]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x => x.Optiontype.ToUpper() == "CALL").ToList());
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any() || obchody.Last().CloseDate != null)
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix =
                        GetOptionMatrix(
                            optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate)
                                .ToList());
                    var hodnota = MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((hodnota*100)) < obchod.OpenPrice*100* (decimal)0.5 ||
                        Math.Abs((hodnota * 100)) > obchod.OpenPrice * 100 * (decimal)1.5)
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade
                        {
                            OpenDate = obchDen,
                            Strike = atmRow.Strike,
                            OpenPrice = hodnota,
                            Contract = atmRow.ExpirationDate.ToShortDateString(),
                            OpenStockPrice = atmRow.StockPrice,
                            ExpirationDate = atmRow.ExpirationDate
                        };

                        obchody.Add(obchodNew);

                    }
                }
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        public string PocitajStrategiuBuyCallOtm3(List<Option> optionData)
        {
            string result = "";
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[2]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x => x.Optiontype.ToUpper() == "CALL").ToList());
                var atmRow = optionMatrix[deltaStrike + 2];

                if (!obchody.Any() || obchody.Last().CloseDate != null)
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix =
                        GetOptionMatrix(
                            optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate)
                                .ToList());
                    var hodnota = MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs(hodnota * 100) < obchod.OpenPrice * 100 * (decimal)0.3 ||
                        Math.Abs(hodnota * 100) > obchod.OpenPrice * 100 * (decimal)1.5)
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade
                        {
                            OpenDate = obchDen,
                            Strike = atmRow.Strike,
                            OpenPrice = hodnota,
                            Contract = atmRow.ExpirationDate.ToShortDateString(),
                            OpenStockPrice = atmRow.StockPrice,
                            ExpirationDate = atmRow.ExpirationDate
                        };

                        obchody.Add(obchodNew);

                    }
                }
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        public string PocitajStrategiuBuyCallAtm3(List<Option> optionData)
        {
            string result = "";
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[2]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x => x.Optiontype.ToUpper() == "CALL").ToList());
                var atmRow = optionMatrix[deltaStrike - 2];

                if (!obchody.Any() || obchody.Last().CloseDate != null)
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Last().CloseDate == null || obchDen.AddDays(3) >= obchody.Last().ExpirationDate)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix =
                        GetOptionMatrix(
                            optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate)
                                .ToList());
                    var hodnota = MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((hodnota * 100)) < obchod.OpenPrice * 100 * (decimal)0.3 ||
                        Math.Abs((hodnota * 100)) > obchod.OpenPrice * 100 * (decimal)1.5 || 
                        obchDen.AddDays(3) >= obchody.Last().ExpirationDate
                        )
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade
                        {
                            OpenDate = obchDen,
                            Strike = atmRow.Strike,
                            OpenPrice = hodnota,
                            Contract = atmRow.ExpirationDate.ToShortDateString(),
                            OpenStockPrice = atmRow.StockPrice,
                            ExpirationDate = atmRow.ExpirationDate
                        };

                        obchody.Add(obchodNew);

                    }
                }

                Console.WriteLine(obchDen.AddDays(4) >= obchody.Last().ExpirationDate);
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }
    }
}
