using System;
using System.Collections.Generic;   // Add this for TagValue pairs
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms;
using IBApi;

namespace IB
{
    public partial class Form1 : Form
    {
        private Samples.EWrapperImpl ibClient;
        private bool connected = false;

        public Form1()
        {
            InitializeComponent();

            // Create the ibClient object to represent the connection
            ibClient = new Samples.EWrapperImpl();

            connected = false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Create a new contract to specify the security we are searching for
            Contract contract = new Contract();
            // Fill in the Contract properties
            contract.Symbol = "IBM";
            contract.SecType = "STK";
            contract.Exchange = "SMART";
            contract.Currency = "USD";
            // Create a new TagValue List object (for API version 9.71) 
            List<TagValue> mktDataOptions = new List<TagValue>();

            // Kick off the request for market data for this
            // contract.  reqMktData Parameters are:
            // tickerId           - A unique id to represent this request
            // contract           - The contract object specifying the financial instrument
            // genericTickList    - A string representing special tick values
            // snapshot           - When true obtains only the latest price tick
            //                      When false, obtains data in a stream
            // regulatory snapshot - API version 9.72 and higher. Remove for earlier versions of API
            // mktDataOptions   - TagValueList of options 
            ibClient.ClientSocket.reqMktData(1, contract, "", false, false, mktDataOptions);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (connected == false)
            {
                // Connect to the IB Server through TWS. Parameters are:
                // host       - Host name or IP address of the host running TWS
                // port       - The port TWS listens through for connections
                // clientId   - The identifier of the client application
                ibClient.ClientSocket.eConnect("", 7496, 0);

                // For IB TWS API version 9.72 and higher, implement this
                // signal-handling code. Otherwise comment it out.

                var reader = new EReader(ibClient.ClientSocket, ibClient.Signal);
                reader.Start();
                new Thread(() =>
                    {
                        while (ibClient.ClientSocket.IsConnected())
                        {
                            ibClient.Signal.waitForSignal();
                            reader.processMsgs();
                        }
                    })
                    {IsBackground = true}.Start();

                connected = true;
            }
            else
            {
                // Disconnect from TWS
                ibClient.ClientSocket.eDisconnect();

                connected = false;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            // Cancel the subscription/request. Parameter is:
            // tickerId         - A unique id to represent the request 
            ibClient.ClientSocket.cancelMktData(1);
        }

        public virtual void tickPrice(int tickerId, int field, double price, int canAutoExecute)
        {
            lbPrice.Text = price.ToString();
            Console.WriteLine("Tick Price. Ticker Id:" + tickerId + ", Field: " + field +
                              ", Price: " + price + ", CanAutoExecute: " + canAutoExecute + "\n");
        }

        public virtual void tickSize(int tickerId, int field, int size)
        {
            Console.WriteLine("Tick Size. Ticker Id:" + tickerId +
                              ", Field: " + field + ", Size: " + size + "\n");
        }

        private void lbPrice_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
