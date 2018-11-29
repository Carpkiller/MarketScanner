namespace MarketScanner.Types
{
    public class Statistics
    {
        public int PocetPod05 { get; set; }
        public int Pocet05Az1 { get; set; }
        public int Pocet1Az15 { get; set; }
        public int Pocet15Az25 { get; set; }
        public int Pocetnad25 { get; set; }

        public int PocetPod05Perc { get; set; }
        public int Pocet05Az1Perc { get; set; }
        public int Pocet1Az15Perc { get; set; }
        public int Pocet15Az25Perc { get; set; }
        public int Pocetnad25Perc { get; set; }

        public double Cena { get; set; }

        public Statistics()
        {
            PocetPod05 = 0;
            Pocet05Az1 = 0;
            Pocet1Az15 = 0;
            Pocet15Az25 = 0;
            Pocetnad25 = 0;
            PocetPod05Perc = 0;
            Pocet05Az1Perc = 0;
            Pocet1Az15Perc = 0;
            Pocet15Az25Perc = 0;
            Pocetnad25Perc = 0;
        }
    }
}
