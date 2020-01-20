using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MarketScanner;
using MarketScanner.Types;

namespace Analyzer
{
    public partial class Form1 : Form
    {
        private int pocetPod05 = 0;
        private int pocet05az1 = 0;
        private int pocet1az15 = 0;
        private int pocet15az25 = 0;
        private int pocetnad25 = 0;

        private int pocetPod05perc = 0;
        private int pocet05az1perc = 0;
        private int pocet1az15perc = 0;
        private int pocet15az25perc = 0;
        private int pocetnad25perc = 0;

        private string IEXTrading_API_URL = "https://cloud.iexapis.com/v1/";
        private string Token = "pk_76cf12829196474ab3994a0fc19cf39f";

        public Form1()
        {
            InitializeComponent();
            InitColumnStock();
        }

        public void InitColumnStock()
        {
            listView1.Columns.Clear();
            listView1.Columns.Add("Datum");
            listView1.Columns.Add("Open");
            listView1.Columns.Add("Close");
            listView1.Columns.Add("High");
            listView1.Columns.Add("Low");
            listView1.Columns.Add("Change");
            listView1.Columns.Add("Change %");
        }

        public void InitColumnCompletScan()
        {
            listView1.Columns.Clear();
            listView1.Columns.Add("Symbol");
            listView1.Columns.Add("Cena");
            listView1.Columns.Add("Pod 0.5");
            listView1.Columns.Add("Medzi 0.5 - 1.0");
            listView1.Columns.Add("Medzi 1.0 - 1.5");
            listView1.Columns.Add("Medzi 1.5 - 2.5");
            listView1.Columns.Add("Nad 2.5");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            pocetPod05 = 0;
            pocet05az1 = 0;
            pocet1az15 = 0;
            pocet15az25 = 0;
            pocetnad25 = 0;

            pocetPod05perc = 0;
            pocet05az1perc = 0;
            pocet1az15perc = 0;
            pocet15az25perc = 0;
            pocetnad25perc = 0;

            Jadro jadro = new Jadro(IEXTrading_API_URL, Token);
            List<HistoryStockPrice> res = jadro.GetPiatkoveCeny(textBox1.Text, checkBox1.Checked, int.Parse(textBox3.Text));

            Statistics stats = jadro.GetStatistics(res, listView1);

            textBox2.Text = string.Empty;
            textBox2.Text = "Pocet pod 0.5          - " + stats.PocetPod05 + Environment.NewLine +
                            "Pocet medzi 0.5 - 1.0  - " + stats.Pocet05Az1 + Environment.NewLine +
                            "Pocet medzi 1 - 1.5    - " + stats.Pocet1Az15 + Environment.NewLine +
                            "Pocet medzi 1.5 - 2.5  - " + stats.Pocet15Az25 + Environment.NewLine +
                            "Pocet nad 2.5          - " + stats.Pocetnad25 +
                            Environment.NewLine + " ================================ " + Environment.NewLine +
                            "Pocet pod 0.5 %         - " + stats.PocetPod05Perc + Environment.NewLine +
                            "Pocet medzi 0.5 - 1.0 % - " + stats.Pocet05Az1Perc + Environment.NewLine +
                            "Pocet medzi 1 - 1.5 %   - " + stats.Pocet1Az15Perc + Environment.NewLine +
                            "Pocet medzi 1.5 - 2.5 % - " + stats.Pocet15Az25Perc + Environment.NewLine +
                            "Pocet nad 2.5 %         - " + stats.Pocetnad25Perc;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            InitColumnCompletScan();

            Jadro jadro = new Jadro(IEXTrading_API_URL, Token);

            var symbols = File.ReadAllLines("SP500.txt");
            //jadro.LoadSymbols();

            foreach (var symbol in symbols)
            {
                if (!symbol.Contains("-"))
                {
                    List<HistoryStockPrice> res = jadro.GetPiatkoveCeny(symbol, checkBox1.Checked, int.Parse(textBox3.Text));
                    Statistics stats = jadro.GetStatistics(res);

                    string[] row =
                    {
                        symbol,
                        stats.Cena.ToString("F"),
                        stats.PocetPod05Perc.ToString(),
                        stats.Pocet05Az1Perc.ToString(),
                        stats.Pocet1Az15Perc.ToString(),
                        stats.Pocet15Az25Perc.ToString(),
                        stats.Pocetnad25Perc.ToString()
                    };
                    var listViewItem = new ListViewItem(row);
                    listView1.Items.Add(listViewItem);
                    listView1.Refresh();

                    Thread.Sleep(1100);
                }
            }

            MessageBox.Show("Koniec");
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (listView1.Sorting == SortOrder.Descending)
            {
                listView1.Sorting = SortOrder.Ascending;
            }
            else
            {
                listView1.Sorting = SortOrder.Descending;
            }

            listView1.Sort();
            listView1.Refresh();
        }

        public void CopyListBox(ListView list)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in list.Items)
            {
                ListViewItem l = item as ListViewItem;
                if (l != null)
                    foreach (ListViewItem.ListViewSubItem sub in l.SubItems)
                        sb.Append(sub.Text + "\t");
                sb.AppendLine();
            }
            Clipboard.SetDataObject(sb.ToString());

        }

        private void listView1_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyListBox(listView1);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            textBox2.Text = string.Empty;
            textBox2.AppendText("Pondelok" + Environment.NewLine);
            textBox2.AppendText(GetDenneLimity(DayOfWeek.Monday));
            textBox2.AppendText(Environment.NewLine + "=====================" + Environment.NewLine);
            textBox2.AppendText(Environment.NewLine + "Utorok" + Environment.NewLine);
            textBox2.AppendText(GetDenneLimity(DayOfWeek.Tuesday));
            textBox2.AppendText(Environment.NewLine + "=====================" + Environment.NewLine);
            textBox2.AppendText(Environment.NewLine + "Streda" + Environment.NewLine);
            textBox2.AppendText(GetDenneLimity(DayOfWeek.Wednesday));
            textBox2.AppendText(Environment.NewLine + "=====================" + Environment.NewLine);
            textBox2.AppendText(Environment.NewLine + "Stvrtok" + Environment.NewLine);
            textBox2.AppendText(GetDenneLimity(DayOfWeek.Thursday));
            textBox2.AppendText(Environment.NewLine + "=====================" + Environment.NewLine);
            textBox2.AppendText(Environment.NewLine + "Piatok" + Environment.NewLine);
            textBox2.AppendText(GetDenneLimity(DayOfWeek.Friday));
        }

        private string GetDenneLimity(System.DayOfWeek day)
        {
            Jadro jadro = new Jadro(IEXTrading_API_URL, Token);
            List<HistoryStockPrice> res = jadro.GetPiatkoveCeny(textBox1.Text, checkBox1.Checked, int.Parse(textBox3.Text), day);

            Statistics stats = jadro.GetStatistics(res);

            var result = string.Empty;
            result = //"Pocet pod 0.5          - " + stats.PocetPod05 + Environment.NewLine +
                     //"Pocet medzi 0.5 - 1.0  - " + stats.Pocet05Az1 + Environment.NewLine +
                     //"Pocet medzi 1 - 1.5    - " + stats.Pocet1Az15 + Environment.NewLine +
                     //"Pocet medzi 1.5 - 2.5  - " + stats.Pocet15Az25 + Environment.NewLine +
                     //"Pocet nad 2.5          - " + stats.Pocetnad25 +
                     Environment.NewLine + " ----------------------- " + Environment.NewLine +
                     "Pocet pod 0.5 %         - " + stats.PocetPod05Perc + Environment.NewLine +
                     "Pocet medzi 0.5 - 1.0 % - " + stats.Pocet05Az1Perc + Environment.NewLine +
                     "Pocet medzi 1 - 1.5 %   - " + stats.Pocet1Az15Perc + Environment.NewLine +
                     "Pocet medzi 1.5 - 2.5 % - " + stats.Pocet15Az25Perc + Environment.NewLine +
                     "Pocet nad 2.5 %         - " + stats.Pocetnad25Perc;

            return result;
        }
    }
}
