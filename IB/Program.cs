﻿using System;
using System.Collections.Generic;   // Add this for TagValue pairs
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

// Use the Interactive Brokers "IBApi" 
// Make sure add the reference to the CSharpAPI.dll or TwsLib.dll in the Project
using IBApi;
using Samples;

namespace IB
{
    //static class Program
    //{
    //    /// <summary>
    //    /// The main entry point for the application.
    //    /// </summary>
    //    [STAThread]
    //    static void Main()
    //    {
    //        Application.EnableVisualStyles();
    //        Application.SetCompatibleTextRenderingDefault(false);
    //        Application.Run(new Form1());
    //    }
    //}

    //class Program
    //{
    //    static void Main(string[] args)
    //    {

    //        // Create the ibClient object to represent the connection
    //        // Note that the EWrapperImpl was imported previously and uses the
    //        // samples namespace. If you changed this Namespace name, 
    //        // use your new name here in place of "Samples".
    //        Samples.EWrapperImpl ibClient = new Samples.EWrapperImpl();
    //        Console.WriteLine("Vytvorenie clienta");

    //        // Connect to the IB Server through TWS. Parameters are:
    //        // host       - Host name or IP address of the host running TWS
    //        // port       - The port TWS listens through for connections
    //        // clientId   - The identifier of the client application
    //        ibClient.ClientSocket.eConnect("", 7496, 1);
    //        Console.WriteLine("Pripojenie");

    //        // For IB TWS API version 9.72 and higher, implement this
    //        // signal-handling code. Otherwise comment it out.

    //        var reader = new EReader(ibClient.ClientSocket, ibClient.Signal);
    //        reader.Start();
    //        new Thread(() => {
    //                while (ibClient.ClientSocket.IsConnected())
    //                {
    //                    ibClient.Signal.waitForSignal();
    //                    reader.processMsgs();
    //                }
    //            })
    //            { IsBackground = true }.Start();

    //        Console.WriteLine("Reader");

    //        // Pause here until the connection is complete 
    //        while (ibClient.NextOrderId <= 0) { }

    //        // Create a new contract to specify the security we are searching for
    //        // IBM 06/21/2014 190 Call (use 20140620 as the expiry) 
    //        Contract contract = new Contract();
    //        contract.Symbol = "IBM";
    //        contract.SecType = "OPT";
    //       // contract.Expiry = "20140620";
    //        contract.Strike = 190.00;
    //        contract.Right = "CALL";
    //        contract.Exchange = "SMART";
    //        contract.Currency = "USD";

    //        // Create a new TagValue List object (for API version 9.71) 
    //        List<TagValue> mktDataOptions = new List<TagValue>();

    //        // Kick off the request for market data for this
    //        // contract.  reqMktData Parameters are:
    //        // tickerId           - A unique id to represent this request
    //        // contract           - The contract object specifying the financial instrument
    //        // genericTickList    - A string representing special tick values
    //        // snapshot           - When true obtains only the latest price tick
    //        //                      When false, obtains data in a stream
    //        // regulatory snapshot - API version 9.72 and higher. Remove for earlier versions of API
    //        // mktDataOptions   - TagValueList of options 
    //        //ibClient.ClientSocket.reqMktData(1, contract, "", false, false, mktDataOptions);
    //        double dblVolatility = 0.21;
    //        double dblUnderlyingPrice = 192.00;
    //        // Call the method to price the option
    //        ibClient.ClientSocket.calculateOptionPrice(1, contract, dblVolatility, dblUnderlyingPrice);

    //        // Pause so we can view the output
    //        Console.ReadKey();

    //        // Cancel the subscription/request. Parameter is:
    //        // tickerId         - A unique id to represent the request 
    //        ibClient.ClientSocket.cancelMktData(1);

    //        // Disconnect from TWS
    //        ibClient.ClientSocket.eDisconnect();
    //    }
    //}

    class Program
    {
        public static Dictionary<int, Contract> contractId;
        public static int tickerId_Option_Price = 300;

        public static int Main(string[] args)
        {
            contractId = new Dictionary<int, Contract>();

            EWrapperImpl testImpl = new EWrapperImpl();
            EClientSocket clientSocket = testImpl.ClientSocket;
            EReaderSignal readerSignal = testImpl.Signal;
            //! [connect]
            //clientSocket.eConnect("127.0.0.1", 7496, 0);
            clientSocket.eConnect("", 7496, 1);
            //! [connect]
            //! [ereader]
            //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue
            var reader = new EReader(clientSocket, readerSignal);
            reader.Start();
            //Once the messages are in the queue, an additional thread need to fetch them
            new Thread(() => { while (clientSocket.IsConnected()) { readerSignal.waitForSignal(); reader.processMsgs(); } }) { IsBackground = true }.Start();

            clientSocket.reqMarketDataType(2);
            int tickerId = 100;
            getStockPrice(clientSocket, "NQ", tickerId);



            int tickerId_Option = 200;
            string expDate = "20200320";
            getContractDetails(clientSocket, "NQ", tickerId_Option, expDate);

            while (contractId.Count == 0)
            {
            }

            Thread.Sleep(500);

            Console.WriteLine("=====================================");
            Console.WriteLine("Contract Id set to: " + contractId);
            Console.WriteLine();
            

            foreach (var contId in contractId)
            {
                Console.WriteLine($"ContractId {contId.Value.ConId} , tickerId {contId.Key}");
                getOptionPrice(clientSocket, "NQ", contId.Key, contId.Value.ConId);
            }

            Console.ReadKey();
            clientSocket.cancelMktData(tickerId);
            clientSocket.cancelMktData(tickerId_Option);
            Console.WriteLine("Disconnecting...");
            clientSocket.eDisconnect();
            return 0;

        }

        internal static void optionsDetailHandler(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            if (field == 10 || field == 11)
            {
                Console.WriteLine("TickOptionComputation. TickerId: " + tickerId + ", field: " + field +
                                  ", ImpliedVolatility: " + impliedVolatility + ", Delta: " + delta
                                  + ", OptionPrice: " + optPrice +
                                  ", pvDividend: " + pvDividend + 
                                  ", Gamma: " + gamma +
                                  ", Vega: " + vega + ", Theta: " + theta + ", UnderlyingPrice: " + undPrice);

                File.AppendAllText($"{DateTime.Now.ToShortDateString()}_{contractId[tickerId].Symbol}_{contractId[tickerId].Right}_{contractId[tickerId].Strike}.txt", 
                    $"{DateTime.Now.ToLongTimeString()}_{contractId[tickerId].Symbol}_{contractId[tickerId].Right}_{contractId[tickerId].Strike} - {optPrice}-{impliedVolatility}-" +
                    $"{delta}-{gamma}-{vega}-{theta}-{undPrice} {Environment.NewLine}");
            }
        }

        private static void getStockPrice(EClientSocket client, string symbol, int tickerId)
        {
            List<TagValue> mktDataOptions = new List<TagValue>();
            Contract contract = new Contract();

            contract.Symbol = symbol;
            contract.SecType = "FUT";
            contract.Exchange = "GLOBEX";
            contract.Currency = "USD";
            contract.LastTradeDateOrContractMonth = "20200320";
            client.reqMktData(tickerId, contract, "", false, false, mktDataOptions);
            Thread.Sleep(10);
        }

        private static void getOptionPrice(EClientSocket client, string symbol, int tickerId, int conId)
        {
            List<TagValue> mktDataOptions = new List<TagValue>();
            Contract contract = new Contract();
            contract.Symbol = symbol;
            contract.SecType = "OPT";
            contract.Exchange = "GLOBEX";
            contract.Currency = "USD";
            contract.ConId = conId;
            contract.Strike = 9170;

            client.reqMktData(tickerId, contract, "", false, false, mktDataOptions);
        }

        private static void getContractDetails(EClientSocket client, string symbol, int tickerId, string expDate)
        {
            Contract contract = new Contract();
            contract.Symbol = symbol;
            contract.SecType = "FOP";
            contract.Exchange = "GLOBEX";
            contract.Currency = "USD";
            contract.Multiplier = "20";
            //contract.Exchange = "BOX";
            contract.LastTradeDateOrContractMonth = expDate;
            contract.Strike = 9170;
            //contract.Right = "C";

            client.reqContractDetails(tickerId, contract);
            Thread.Sleep(10);
        }

        public static void tickPriceHandler(int tickerId, int field, double price, int canAutoExecute)
        {
            Console.WriteLine("Tick Price in Main. Ticker Id:" + tickerId + ", Field: " + field + ", Price: " + price + ", CanAutoExecute: " + canAutoExecute);

        }

        public static void contractDetailHandler(int reqId, ContractDetails contractDetails)
        {
            Console.WriteLine("ContractDetails. ReqId: " + reqId + " - " + contractDetails.Contract.Symbol + ", " + contractDetails.Contract.SecType + ", ConId: " 
                              + contractDetails.Contract.ConId + " @ " + contractDetails.Contract.Exchange + ", Strike:  " + contractDetails.Contract.Strike 
                              + ", Right: " + contractDetails.Contract.Right);
            if (contractDetails.Contract.Strike == 9170)
            {
                contractId.Add(tickerId_Option_Price++,
                    new Contract()
                    {
                        Strike = contractDetails.Contract.Strike,
                        Symbol = contractDetails.Contract.Symbol,
                        Right = contractDetails.Contract.Right,
                        ConId = contractDetails.Contract.ConId
                    });
            }
        }

    }
}


