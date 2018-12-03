using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MarketScanner;
using MarketScanner.Types;

namespace HistoryOptions
{
    public partial class Form1 : Form
    {
        private bool loading;
        private Core jadro;

        public Form1()
        {
            InitializeComponent();
            loading = false;
            jadro = new Core();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            loading = true;
            var optionData = LoadOptionData();

            var minDate = optionData.Min(x => x.QuoteDate);
            var maxDate = optionData.Max(x => x.QuoteDate);
            SetDateTimePicker(minDate, maxDate);

            lblPrice.Text = optionData.Where(x => x.QuoteDate == maxDate).Max(x => x.StockPrice).ToString();
            tabControl1.TabPages.Clear();

            var expirationDates = optionData.Where(x => x.QuoteDate == maxDate).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
            foreach (var expirationDate in expirationDates)
            {
                TabPage tp = new TabPage(expirationDate.ToShortDateString());
                tabControl1.TabPages.Add(tp);
            }

            var curEpirationDate = tabControl1.SelectedTab.Text;
            FillTabPage(optionData.Where(x => x.QuoteDate == maxDate && x.ExpirationDate.ToShortDateString() == curEpirationDate).OrderBy(x => x.Strike).ToList());
            loading = false;
        }

        private void SetDateTimePicker(DateTime minDate, DateTime maxDate)
        {
            dateTimePicker1.MinDate = minDate;
            dateTimePicker1.MaxDate = maxDate;
            dateTimePicker1.Value = maxDate;
        }

        private void FillTabPage(List<Option> optionData)
        {
            int i = 0;
            int index = 0;
            double previousDelta = 1;
            listView1.Clear();
            FillListViewHeader();
            var ee = new List<ListViewItem>();
            foreach (var option in optionData.Where(x => x.QuoteDate == optionData[1].QuoteDate && x.Optiontype.ToUpper() == "CALL"))
            {
                string[] row =
                    {
                        option.OpenInterest,
                        option.Volume,
                        option.Delta.ToString(),
                        option.Bid.ToString(),
                        option.Ask.ToString(),
                        option.Strike.ToString(),
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").Bid.ToString(),
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").Ask.ToString(),
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").Delta.ToString(),
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").Volume,
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").OpenInterest
                    };
                
                var listViewItem = new ListViewItem(row);
                ee.Add(listViewItem);

                if (Math.Abs(option.Delta - 0.5) < previousDelta)
                {
                    index = i; previousDelta = Math.Abs(option.Delta - 0.5);
                }
                
                i++;
            }
            listView1.Sort(); 
            listView1.Items.AddRange(ee.ToArray());
            listView1.Items[index].BackColor = Color.LightBlue;
            listView1.Refresh();
        }

        private void FillListViewHeader()
        {
            listView1.Columns.Clear();
            listView1.Columns.Add("Open Int");
            listView1.Columns.Add("Volume");
            listView1.Columns.Add("Delta");
            listView1.Columns.Add("Bid");
            listView1.Columns.Add("Ask");
            listView1.Columns.Add("Strike");
            listView1.Columns.Add("Bid");
            listView1.Columns.Add("Ask");
            listView1.Columns.Add("Delta");
            listView1.Columns.Add("Volume");
            listView1.Columns.Add("Open Int");
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                FillListView((sender as DateTimePicker).Value);
                loading = false;
            }
        }

        private void FillListView(DateTime date, bool reLoadTabs = true)
        {
            var optionData = LoadOptionData();

            if (optionData.Any(x => x.QuoteDate == date))
            {
                lblPrice.Text = optionData.Where(x => x.QuoteDate == date).Max(x => x.StockPrice).ToString();
                
                if (reLoadTabs)
                {
                    tabControl1.TabPages.Clear();
                    var expirationDates =
                        optionData.Where(x => x.QuoteDate == date)
                            .Select(x => x.ExpirationDate)
                            .Distinct()
                            .OrderBy(x => x.Date)
                            .ToList();
                    foreach (var expirationDate in expirationDates)
                    {
                        TabPage tp = new TabPage(expirationDate.ToShortDateString());
                        tabControl1.TabPages.Add(tp);
                    }
                }

                var curEpirationDate = tabControl1.SelectedTab.Text;
                FillTabPage(
                    optionData.Where(
                        x => x.QuoteDate == date && x.ExpirationDate.ToShortDateString() == curEpirationDate)
                        .OrderBy(x => x.Strike)
                        .ToList());
            }
        }

        private List<Option> LoadOptionData()
        {
            var data = File.ReadAllLines(textBox1.Text + ".csv");

            var optionData = new List<Option>();

            foreach (var row in data.Skip(1))
            {
                var rawData = row.Split(',');
                var option = new Option
                {
                    Underlaying = rawData[0],
                    Optionsymbol = rawData[3],
                    Optiontype = rawData[5],
                    ExpirationDate = DateTime.ParseExact(rawData[6], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    QuoteDate = DateTime.ParseExact(rawData[7], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    Strike = double.Parse(rawData[8], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),
                    Last = rawData[9],
                    Bid = double.Parse(rawData[10], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),
                    Ask = double.Parse(rawData[11], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),
                    Volume = rawData[12],
                    OpenInterest = rawData[13],
                    Delta = double.Parse(rawData[15], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),

                    StockPrice = double.Parse(rawData[1], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us"))
                };

                optionData.Add(option);
            }

            return optionData;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                FillListView(dateTimePicker1.Value, false);
                loading = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiu1(LoadOptionData());
        }
    }
}
