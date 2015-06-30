using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BTScanner;

namespace WinSample
{
    public partial class Form2 : Form
    {
        private CS3070 mScanner;

        private delegate void BarcodeDelegate(string s);

        private delegate void ConnectedDelegate(BTScanner.ScannerStatus s);

        public Form2()
        {
            InitializeComponent();

            this.label1.Text = string.Empty;
            this.label2.Text = string.Empty;

            mScanner = CS3070.Instance;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!mScanner.IsBtEnabled)
            {
                MessageBox.Show("kein BT");
                return;
            }

            if (mScanner.IsActive)
            {
                MessageBox.Show("verbindung bereits aktiv");
                return;
            }

            mScanner.Barcode += mScanner_Barcode;
            mScanner.Scanner += mScanner_Scanner;

            mScanner.Start();

            this.label1.Text = string.Empty;
        }

        void mScanner_Scanner(ScannerStatus s)
        {
            try
            {
                Invoke(new ConnectedDelegate(SetStatus), s);
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e);
            }
        }

        void mScanner_Barcode(string barcode)
        {
            try
            {
                Invoke(new BarcodeDelegate(SetBarcode), barcode);
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            mScanner.Stop();
        }

        public void SetBarcode(string barcode)
        {
            this.label1.Text = barcode;
        }

        private void SetStatus(BTScanner.ScannerStatus s)
        {
            switch (s)
            {
                case BTScanner.ScannerStatus.Connected:
                    label2.Text = mScanner.ScannerID;
                    break;
                case BTScanner.ScannerStatus.Disconnected:
                    label2.Text = null;
                    break;
            }
        }
    }
}
