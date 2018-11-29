using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MarketScanner.Types;

namespace HistoryOptions
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var data = File.ReadAllLines(textBox1.Text + ".csv");

            var optionData = new List<Option>();

            foreach (var row in data.Skip(1))
            {
                var rawData = row.Split(',');
                var option = new Option()
                {
                    Underlaying = rawData[0],
                    Optionsymbol = rawData[3],
                    Optiontype = rawData[5],
                    ExpirationDate = DateTime.ParseExact(rawData[6], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    QuoteDate = DateTime.ParseExact(rawData[7], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    Strike = double.Parse(rawData[8], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),
                    Last = rawData[9],
                    Bid = rawData[10],
                    Ask = rawData[11],
                    Volume = rawData[12],
                    OpenInterest = rawData[13]
                };

                optionData.Add(option);
            }

            var expirationDates = optionData.Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();

            tabControl1.TabPages.Clear();
            foreach (var expirationDate in expirationDates)
            {
                TabPage tp = new TabPage(expirationDate.ToShortDateString());
                tabControl1.TabPages.Add(tp);
            }

            var curEpirationDate = tabControl1.SelectedTab.Text;
            FillTabPage(optionData.Where(x => x.QuoteDate == optionData[1].QuoteDate && x.ExpirationDate.ToShortDateString() == curEpirationDate).OrderBy(x => x.Strike).ToList());
        }

        private void FillTabPage(List<Option> optionData)
        {
            FillListViewHeader();
            var ee = new List<ListViewItem>();
            foreach (var option in optionData.Where(x => x.QuoteDate == optionData[1].QuoteDate && x.Optiontype.ToUpper() == "CALL"))
            {
                var e = optionData.Where(x => x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").ToList();
                string[] row =
                    {
                        option.OpenInterest,
                        option.Volume,
                        option.Bid,
                        option.Ask,
                        option.Strike.ToString(),
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").Bid,
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").Ask,
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").Volume,
                        optionData.Single(x => x.QuoteDate == option.QuoteDate && x.Strike == option.Strike && x.Optiontype.ToUpper() == "PUT").OpenInterest
                    };

                
                var listViewItem = new ListViewItem(row);
                ee.Add(listViewItem);
                //listView1.Items.Add(listViewItem);

            }
            //listView1.ListViewItemSorter = new ListViewItemComparer(e.Column);
            listView1.Sort();
            listView1.Items.AddRange(ee.ToArray());
            listView1.Refresh();
            //  tabPage1.Controls.Add(listView1);
        }

        private void FillListViewHeader()
        {
            listView1.Columns.Clear();
            listView1.Columns.Add("Open Int");
            listView1.Columns.Add("Volume");
            listView1.Columns.Add("Bid");
            listView1.Columns.Add("Ask");
            listView1.Columns.Add("Strike");
            listView1.Columns.Add("Bid");
            listView1.Columns.Add("Ask");
            listView1.Columns.Add("Volume");
            listView1.Columns.Add("Open Int");
        }
    }
}
