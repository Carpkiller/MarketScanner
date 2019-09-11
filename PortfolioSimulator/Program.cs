using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace PortfolioSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            double dividenda = 0.18;
            int pocetRokov = 10;
            double cash = 0;
            double vstup = 2000;
            double rocnyNarastDiv = 1.08;
            double cenaAkcie = 5.2;
            double hodnotaPortfolia = vstup;

            int pocetAkcii = (int) (hodnotaPortfolia / cenaAkcie);
            cash = (hodnotaPortfolia % cenaAkcie);

            Console.WriteLine("Zaciatok");
            Console.WriteLine($"Hodnota {hodnotaPortfolia} , pocet akcii {pocetAkcii}");

            for (int i = 0; i < pocetRokov; i++)
            {
                Console.WriteLine($"Rok {i} , cena akcie {cenaAkcie.ToString("F")}, dividenda {dividenda.ToString("F")}");
                for (int j = 0; j < 4; j++)
                {
                    double prijataDividenda = dividenda*pocetAkcii;
                    int prikupeneAkcie = (int)((cash + prijataDividenda )/ cenaAkcie);

                    hodnotaPortfolia += prijataDividenda;
                    //pocetAkcii = (int)(hodnotaPortfolia / cenaAkcie);
                    pocetAkcii += prikupeneAkcie;
                    cash = (cash + prijataDividenda) % cenaAkcie;
                    //hodnotaPortfolia += cash;
                    Console.WriteLine($"Hodnota {pocetAkcii*cenaAkcie + cash} , pocet akcii {pocetAkcii} , prijata dividenda {prijataDividenda.ToString("F")}");
                }

                dividenda = dividenda * rocnyNarastDiv;
                cenaAkcie = cenaAkcie*1.1;
            }

            Console.ReadLine();
        }
    }
}
