using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using System.Threading;

using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;

namespace BTScanner
{
    public enum ScannerStatus
    {
        Connected,
        Disconnected
    };

    public delegate void ScannerHandler(ScannerStatus s);

    public delegate void BarcodeHandler(string barcode);

    public class CS3070
    {
        private static CS3070 INSTANCE = null;

        private static int MAX_DEVICES = 128;

        public event BarcodeHandler Barcode;

        public event ScannerHandler Scanner;

        private Thread mThread;

        private Stream mStream;

        private bool mStop;

        public bool Connected;

        public string ScannerID;

        private CS3070() {}

        private bool mDebug = false;

        public static CS3070 Instance
        {
            get
            {
                if (INSTANCE == null)
                {
                    INSTANCE = new CS3070();
                }
                return INSTANCE;
            }
        }

        public void SetDebug(bool val)
        {
            mDebug = val;
        }

        public bool IsBtEnabled
        {
            get
            {
                if (BluetoothRadio.PrimaryRadio == null)
                {
                    return false;
                }
                return true;
            }
        }

        public bool IsActive
        {
            get
            {
                return mThread != null ? true : false;
            }
        }

        public bool Start()
        {
            Logger.Instance.Log("### " + System.Reflection.MethodBase.GetCurrentMethod().Name);

            mStream = null;

            mStop = false;

            mThread = new Thread(new ThreadStart(PairAndConnect));
            mThread.IsBackground = true;
            mThread.Start();

            return true;
        }

        public void Stop()
        {
            Logger.Instance.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

            mStop = true;

            if (mThread != null && mStream != null)
            {
                mStream.Close();
                mThread = null;
            }
        }

        private void HandleConnectionEvent(BluetoothDeviceInfo device, bool connected)
        {
            try
            {
                Logger.Instance.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

                Connected = connected;
                ScannerID = (device != null && connected) ? device.DeviceName : string.Empty;
            }
            finally
            {
                if (Scanner != null)
                {
                    Scanner(connected ? ScannerStatus.Connected : ScannerStatus.Disconnected);
                }
            }
        }

        private void PairAndConnect()
        {
            try
            {
                Logger.Instance.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

                HandleConnectionEvent(null, false);

                bool first = true;

                while (true)
                {
                    if (mStop)
                    {
                        return;
                    }

                    var l = new List<BluetoothDeviceInfo>();

                    var devices = new BluetoothClient().DiscoverDevices(MAX_DEVICES, true, true, false);
                    GetScanner(devices, ref l);

                    if (!first)
                    {
                        devices = new BluetoothClient().DiscoverDevices(MAX_DEVICES, false, false, true);
                        GetScanner(devices, ref l);
                    }

                    first = false;

                    if (l == null || l.Count == 0)
                    {
                        try
                        {
                            Thread.Sleep(100);
                        }
                        catch { }
                        continue;
                    }

                    Logger.Instance.Log("found:" + l.Count);

                    bool was_connected = false;
                    foreach (var scanner in l)
                    {
                        Logger.Instance.Log("try:" + scanner.DeviceName + ":" + scanner.DeviceAddress);
                        if (!scanner.Authenticated)
                        {
                            Logger.Instance.Log("pairing...");
                            if (!BluetoothSecurity.PairRequest(scanner.DeviceAddress, "1234"))
                            {
                                Logger.Instance.Log("pairing error");
                                continue;
                            }
                        }

                        var cl = new BluetoothClient();

                        try
                        {
                            var ep = new BluetoothEndPoint(scanner.DeviceAddress, BluetoothService.SerialPort);
                            cl.Connect(ep);
                        }
                        catch (Exception e)
                        {
                            HandleConnectionEvent(null, false);
                            Logger.Instance.Log(e);
                            continue;
                        }

                        HandleConnectionEvent(scanner, true);

                        was_connected = true;

                        mStream = cl.GetStream();
                        var buffer = new byte[128];
                        string barcode = string.Empty;

                        try
                        {
                            while (true)
                            {
                                if (mStop)
                                {
                                    return;
                                }

                                int count = mStream.Read(buffer, 0, buffer.Length);
                                if (count <= 0)
                                {
                                    HandleConnectionEvent(null, false);
                                    Logger.Instance.Log("connection closed");
                                    break;
                                }

                                for (int i = 0; i < count; i++)
                                {
                                    var b = buffer[i];
                                    switch (b)
                                    {
                                        case 13:
                                            if (Barcode != null && barcode != null && barcode.Trim().Length > 0)
                                            {
                                                Logger.Instance.Log("barcode:" + barcode);
                                                Barcode(barcode);
                                            }
                                            barcode = string.Empty;
                                            break;
                                        default:
                                            barcode += (char)b;
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Instance.Log(e);
                            HandleConnectionEvent(null, false);
                        }

                        if (mStop)
                        {
                            return;
                        }

                        if (was_connected)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                mThread = null;
            }
        }

        private bool GetScanner(BluetoothDeviceInfo[] devices, ref List<BluetoothDeviceInfo> l)
        {
            if (devices != null && devices.Length > 0)
            {
                if (mDebug)
                {
                    foreach (var d in devices)
                    {
                        Logger.Instance.Log("dev:" + d.DeviceName + ":" + d.DeviceAddress);
                    }
                }

                foreach (var d in devices)
                {
                    if (d.DeviceName.ToLower().Contains("cs3070"))
                    {
                        l.Add(d);
                    }
                }
            }

            return l.Count > 0 ? true : false;
        }
    }
}
