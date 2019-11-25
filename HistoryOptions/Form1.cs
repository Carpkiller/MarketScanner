using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Windows.Forms;
using MarketScanner;
using MarketScanner.Types;

namespace HistoryOptions
{
    public partial class Form1 : Form
    {
        private bool loading;

        public List<Option> OptionData { get; private set; }

        private Core jadro;

        public Form1()
        {
            InitializeComponent();
            comboBox1.DataSource = LoadSymboly();
            loading = false;
            jadro = new Core();
        }

        private List<string> LoadSymboly()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csv");

            return files.Select(file => file.Split('\\').Last().Split('.').First()).ToList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            loading = true;
            OptionData = LoadOptionData();

            var minDate = OptionData.Min(x => x.QuoteDate);
            var maxDate = OptionData.Max(x => x.QuoteDate);
            SetDateTimePicker(minDate, maxDate);

            lblPrice.Text = OptionData.Where(x => x.QuoteDate == maxDate).Max(x => x.StockPrice).ToString();
            tabControl1.TabPages.Clear();

            var expirationDates = OptionData.Where(x => x.QuoteDate == maxDate).Select(x => x.ExpirationDate).Distinct()
                .OrderBy(x => x.Date).ToList();
            foreach (var expirationDate in expirationDates)
            {
                TabPage tp = new TabPage(expirationDate.ToShortDateString());
                tabControl1.TabPages.Add(tp);
            }

            var curEpirationDate = tabControl1.SelectedTab.Text;
            FillTabPage(OptionData
                .Where(x => x.QuoteDate == maxDate && x.ExpirationDate.ToShortDateString() == curEpirationDate)
                .OrderBy(x => x.Strike).ToList());
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
            foreach (var option in optionData.Where(x =>
                x.QuoteDate == optionData[1].QuoteDate && x.Optiontype.ToUpper() == "CALL"))
            {
                string[] row =
                {
                    option.OpenInterest,
                    option.Volume,
                    option.Delta.ToString(),
                    option.Bid.ToString(),
                    option.Ask.ToString(),
                    option.Strike.ToString(),
                    optionData.Single(x =>
                            x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                            x.Optiontype.ToUpper() == "PUT")
                        .Bid.ToString(),
                    optionData.Single(x =>
                            x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                            x.Optiontype.ToUpper() == "PUT")
                        .Ask.ToString(),
                    optionData.Single(x =>
                            x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                            x.Optiontype.ToUpper() == "PUT")
                        .Delta.ToString(),
                    optionData.Single(x =>
                            x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                            x.Optiontype.ToUpper() == "PUT")
                        .Volume,
                    optionData.Single(x =>
                            x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                            x.Optiontype.ToUpper() == "PUT")
                        .OpenInterest
                };

                var listViewItem = new ListViewItem(row);
                ee.Add(listViewItem);

                if (Math.Abs(option.Delta - 0.5) < previousDelta)
                {
                    index = i;
                    previousDelta = Math.Abs(option.Delta - 0.5);
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

            dateTimePicker2.Value = dateTimePicker1.Value;
        }

        private void FillListView(DateTime date, bool reLoadTabs = true)
        {
            var optionData = OptionData;

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
            var data = File.ReadAllLines(comboBox1.SelectedItem + ".csv");

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
                    Bid = decimal.Parse(rawData[10], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),
                    Ask = decimal.Parse(rawData[11], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),
                    Volume = rawData[12],
                    OpenInterest = rawData[13],
                    Delta = double.Parse(rawData[15], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us")),

                    StockPrice = decimal.Parse(rawData[1], NumberStyles.Number, CultureInfo.GetCultureInfo("en-us"))
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

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiu2(LoadOptionData());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiu3(LoadOptionData());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiu4(LoadOptionData());
        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiu5(LoadOptionData());
        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiuBuyCallAtm(LoadOptionData());
        }

        private void button8_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiuBuyCallOtm3(LoadOptionData());
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiuBuyCallAtm3(LoadOptionData());
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(this, e.X, e.Y);
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            dateTimePicker1.Value = dateTimePicker2.Value.Date;

            NacitajObchody();
        }

        private void NacitajObchody()
        {
            if (jadro.GetObchody() == null)
            {
                return;
            }

            decimal profit = 0;

            if (dateTimePicker2.Value.DayOfWeek == DayOfWeek.Saturday || dateTimePicker2.Value.DayOfWeek == DayOfWeek.Sunday)
            {
                return;
            }

            ltvTester.Clear();
            FillTesterListViewHeader();
            var ee = new List<ListViewItem>();
            foreach (var obchod in jadro.GetObchody())
            {
                if (obchod.ExpirationDate > dateTimePicker2.Value)
                {
                    obchod.Ukonceny = false;
                }

                string[] row =
                {
                    obchod.StartDate.ToShortDateString(),
                    obchod.Optiontype,
                    obchod.ExpirationDate?.ToShortDateString(),
                    obchod.Strike.ToString(),
                    obchod.Price.ToString(),
                    obchod.Ukonceny ? obchod.Profit.ToString() : jadro.GetZiskStrata(OptionData, obchod, 
                        dateTimePicker1.Value, lblPrice.Text).ToString(),
                    JeItm(obchod),
                    obchod.Ukonceny.ToString()
                 };


                var listViewItem = new ListViewItem(row);
                ee.Add(listViewItem);
                profit += obchod.Profit;
            }

            //ltvTester.Sort();
            ltvTester.Items.AddRange(ee.ToArray());
            //ltvTester.Items[index].BackColor = Color.LightBlue;
            ltvTester.Refresh();

            lblProfit.Text = profit.ToString();
        }

        private string JeItm(BackTest obchod)
        {
            if (obchod.Optiontype == "AKCIE")
            {
                return "";
            }

            if ((obchod.Optiontype == "CALL" && obchod.Strike < double.Parse(lblPrice.Text)) ||
                (obchod.Optiontype == "PUT" && obchod.Strike > double.Parse(lblPrice.Text)))
            {
                return true.ToString();
            }

            return false.ToString();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            dateTimePicker2.Value = dateTimePicker2.Value.AddDays(-1);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            dateTimePicker2.Value = dateTimePicker2.Value.AddDays(1);
        }

        private void FillTesterListViewHeader()
        {
            ltvTester.Columns.Clear();
            ltvTester.Columns.Add("Otvorenie pozicie");
            ltvTester.Columns.Add("Opcia");
            ltvTester.Columns.Add("Expiracny datum");
            ltvTester.Columns.Add("Strike");
            ltvTester.Columns.Add("Cena");
            ltvTester.Columns.Add("P/L");
            ltvTester.Columns.Add("ITM");
            ltvTester.Columns.Add("Ukonceny");
        }

        private void kupitCALLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jadro.PridajObchod("CALL", "BUY", dateTimePicker1.Value, tabControl1.SelectedTab.Text,
                listView1.SelectedItems[0].SubItems);
            NacitajObchody();
        }

        private void predatCALLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jadro.PridajObchod("CALL", "SELL", dateTimePicker1.Value, tabControl1.SelectedTab.Text,
                listView1.SelectedItems[0].SubItems);
            NacitajObchody();
        }

        private void predatPUTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jadro.PridajObchod("PUT", "SELL", dateTimePicker1.Value, tabControl1.SelectedTab.Text,
                listView1.SelectedItems[0].SubItems);
            NacitajObchody();
        }

        private void kupitPUTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jadro.PridajObchod("PUT", "BUY", dateTimePicker1.Value, tabControl1.SelectedTab.Text,
                listView1.SelectedItems[0].SubItems);
            NacitajObchody();
        }

        private void kupitAkcieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jadro.PridajObchod("AKCIE", "BUY", dateTimePicker1.Value, lblPrice.Text);
            NacitajObchody();
        }

        private void predatAkcieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jadro.PridajObchod("AKCIE", "SELL", dateTimePicker1.Value, lblPrice.Text);
            NacitajObchody();
        }

        private void ltvTester_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip2.Show(ltvTester, e.X, e.Y);
            }
        }

        private void exerciseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ltvTester.SelectedItems[0].SubItems[1].Text == "CALL")
            {
                jadro.PridajObchod("AKCIE", "BUY", dateTimePicker1.Value, ltvTester.SelectedItems[0].SubItems[3].Text);
                jadro.UkonciOpcnyObchod(ltvTester.SelectedItems[0].SubItems[1].Text, ltvTester.SelectedItems[0].SubItems[3].Text, 
                    ltvTester.SelectedItems[0].SubItems[4].Text);
                NacitajObchody();
                return;
            }

            if (ltvTester.SelectedItems[0].SubItems[1].Text == "PUT")
            {
                jadro.PridajObchod("AKCIE", "SELL", dateTimePicker1.Value, ltvTester.SelectedItems[0].SubItems[3].Text);
                jadro.UkonciOpcnyObchod(ltvTester.SelectedItems[0].SubItems[1].Text, ltvTester.SelectedItems[0].SubItems[3].Text,
                    ltvTester.SelectedItems[0].SubItems[4].Text);
                NacitajObchody();
            }
        }

        private void assigmentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ltvTester.SelectedItems[0].SubItems[1].Text == "CALL")
            {
                jadro.PridajObchod("AKCIE", "SELL", dateTimePicker1.Value, ltvTester.SelectedItems[0].SubItems[3].Text);
                jadro.UkonciOpcnyObchod(ltvTester.SelectedItems[0].SubItems[1].Text, ltvTester.SelectedItems[0].SubItems[3].Text,
                    ltvTester.SelectedItems[0].SubItems[4].Text);
                NacitajObchody();
                return;
            }

            if (ltvTester.SelectedItems[0].SubItems[1].Text == "PUT")
            {
                jadro.PridajObchod("AKCIE", "BUY", dateTimePicker1.Value, ltvTester.SelectedItems[0].SubItems[3].Text);
                jadro.UkonciOpcnyObchod(ltvTester.SelectedItems[0].SubItems[1].Text, ltvTester.SelectedItems[0].SubItems[3].Text,
                    ltvTester.SelectedItems[0].SubItems[4].Text);
                NacitajObchody();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            dateTimePicker1.Value = dateTimePicker1.MinDate;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            jadro.VymazObchody();
            NacitajObchody();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            //saveFileDialog1.InitialDirectory = @"C:\";      
            saveFileDialog1.Title = "Save";
            saveFileDialog1.DefaultExt = "txt";
            saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                jadro.UlozObchody(saveFileDialog1.FileName, dateTimePicker2.Value);
                NacitajObchody();
            }
            
        }

        private void button15_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dateTimePicker2.Value = jadro.NacitajObchody(openFileDialog1.FileName);
                NacitajObchody();
            }
        }

        private void zmazatObchodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jadro.OdstranObchod(ltvTester.SelectedItems[0].Index);

            NacitajObchody();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            textBox2.Text = jadro.PocitajStrategiuButterfly(OptionData);
        }
    }
}
