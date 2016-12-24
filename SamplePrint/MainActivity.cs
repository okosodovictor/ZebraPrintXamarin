using Android.App;
using Android.Widget;
using Android.OS;
using LinkOS.Plugin.Abstractions;
using LinkOS.Plugin;
using Android.Media;
using System.Threading.Tasks;
using Android.Util;
using System;
using System.Text;
using System.IO;

namespace SamplePrint
{
    [Activity(Label = "SamplePrint", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        EditText edtMessage = null;
        Button btnSend = null;
        IConnection connection;

        IFileUtil file;

        const string tag = "BasicPrintApp";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            edtMessage = FindViewById<EditText>(Resource.Id.edtMessage);
            btnSend = FindViewById<Button>(Resource.Id.btnSend);

            btnSend.Click += btnSend_Click;
        }

        void btnSend_Click(object sender, System.EventArgs e)
        {
            Start_Print();
        }

        private void Start_Print()
        {
            btnSend.Enabled = false;
            string address = "AC3FA4757D2F";

            // Bluetooth communications must be handled on a separate thread and it's
            //    best practice to handle network coms on it as well 
            new Task(() =>
            {
                Print(address);
            }).Start();
        }

        private void Print(string address)
        {
            // string zpl = "^XA^LL200^FO20,20^A0N,20,20^FDHello World^FS^XZ";
            ///string zpl = "^XA^LL200^FO30,20^A0N,30,30^FDHello World^FS^XZ";  //original
           string zpl = "^XA^POI^MNN^LL90^PW400^FO20,20^A0N,50,50^FDTEST^FS^XZ";     // try this out incase
            //string zpl = "^XA^LL1200^FS^FO20,20^AE^FDTesting^FS^XZ";

            try
            {
                if ((connection == null) || (!connection.IsConnected))
                {
                    connection = ConnectionBuilder.Current.Build(address);
                    connection.Open();
                }
                if ((SetPrintLanguage(connection)) && (CheckPrinterStatus(connection)))
                {

                    IZebraPrinter printer = ZebraPrinterFactory.Current.GetInstance(connection);

                    printer.SendFileContents(LoadFile("Put the name of the content you want to print here"));
                    PostPrintCheckStatus(connection);

                    connection.Write(System.Text.Encoding.ASCII.GetBytes(zpl));
                }



            }
            catch (Exception e)
            {
                //if the device is unable to connect, an exception is thrown
                Log.Debug(tag, e.ToString());
            }
            finally
            {
                this.RunOnUiThread(() =>
                {
                    btnSend.Enabled = true;
                });
                
            }
        }

        private bool SetPrintLanguage(IConnection connection)
        {
            string setLanguage = "! U1 setvar \"device.languages\" \"zpl\"\r\n\r\n! U1 getvar \"device.languages\"\r\n\r\n";
            byte[] response = connection.SendAndWaitForResponse(System.Text.Encoding.ASCII.GetBytes(setLanguage), 500, 500);
            string s = System.Text.Encoding.ASCII.GetString(response);
            if (!s.Contains("zpl"))
            {
                Log.Debug(tag, "Not a ZPL printer.");
                return false;
            }
            return true;
        }

        private bool CheckPrinterStatus(IConnection connection)
        {
            IZebraPrinter printer = ZebraPrinterFactory.Current.GetInstance(PrinterLanguage.ZPL, connection);
           
            IPrinterStatus status = printer.CurrentStatus;
            if (!status.IsReadyToPrint)
            {
                Log.Debug(tag, "Printer in Error: " + status.ToString());
                
            }
            return true;
        }

        protected override void OnPause()
        {
            base.OnPause();

            Log.Debug(tag, "Closing connection on inactive app");
            if ((connection != null) && (connection.IsConnected))
            {
                connection.Close();
            }
        }
        public string LoadFile(string filename)
        {
            var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(documentsPath, filename);
            return System.IO.File.ReadAllText(filePath);
        }
 
        public bool PostPrintCheckStatus(IConnection connection)
        {
            // Check the status again to verify print happened successfully
            IZebraPrinter printer = ZebraPrinterFactory.Current.GetInstance(connection);
            IPrinterStatus status = printer.CurrentStatus;
            // Wait while the printer is printing
            while ((status.NumberOfFormatsInReceiveBuffer > 0) && (status.IsReadyToPrint))
            {
                status = printer.CurrentStatus;
            }
            // verify the print didn't have errors like running out of paper
            if (!status.IsReadyToPrint)
            {
                System.Diagnostics.Debug.WriteLine("Error durring print. Printer is " + status.Status);
                return false;
            }
            return true;
        }

        //public void SendFile()
        //{
        //    IConnection connection = ConnectionBuilder.Current.Build("TCP:192.168.1.100:9100");
        //    try
        //    {
        //        connection.Open();
        //        if (!CheckPrinterLanguage(connection))
        //            return;
        //        if (!PreCheckPrinterStatus(connection))
        //            return;
        //        IZebraPrinter printer = ZebraPrinterFactory.Current.GetInstance(connection);
        //        printer.SendFileContents(@"/Documents/SAMPLE.FMT");
        //        PostPrintCheckStatus(connection);
        //    }
        //    catch (Exception e)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Exception:" + e.Message);
        //    }
        //    finally
        //    {
        //        if (connection.IsConnected)
        //            connection.Close();
        //    }
        //}
    }
}

