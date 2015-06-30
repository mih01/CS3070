using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;

namespace BTScanner
{
    public class Logger
    {
        private static Logger INSTANCE;

        private object mLock = new object();

        private string mLogDirectory = @"C:\FS_Log";

        private string mLogfileName = "cs3070_log.txt";

        private Logger()
        {
        }

        public static Logger Instance
        {
            get
            {
                if (INSTANCE == null)
                {
                    INSTANCE = new Logger();
                }

                try
                {
                    var logDir = GetGlobals("LogTo");
                    if (logDir != null && logDir.Trim().Length > 0)
                    {
                        INSTANCE.mLogDirectory = logDir;
                    }
                }
                catch { }

                return INSTANCE;
            }
        }

        private static string GetGlobals(string key)
        {
            string value = "";
            XmlDocument myDoc = null;
            const string _globalconfig_ = @"C:\FS_Globals\App.config";

            try
            {
                if (File.Exists(_globalconfig_) == true)
                {
                    myDoc = new XmlDocument();
                    myDoc.Load(_globalconfig_);

                    XmlNodeList e = myDoc.GetElementsByTagName("add");

                    foreach (XmlElement el in e)
                    {
                        if (el.Attributes["key"].Value == key)
                        {
                            value = el.Attributes["value"].Value;
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                //cLogWriter.LogWriter("FehlerDir beim Zugriff auf die " + _globalconfig_, ex);
            }

            return value;
        }

        public void Log(string msg)
        {
            WriteToLog(msg, null);
        }

        public void Log(Exception e)
        {
            if (e != null)
            {
                WriteToLog(e.Message, e);
            }
        }

        private void WriteToLog(String s, Exception e)
        {
            try
            {
                lock (mLock)
                {
                    var location = Path.Combine(mLogDirectory, mLogfileName);

                    try
                    {
                        if (!Directory.Exists(mLogDirectory))
                        {
                            Directory.CreateDirectory(mLogDirectory);
                        }

                        using (var w = new StreamWriter(location, true))
                        {
                            if (s != null)
                            {
                                w.WriteLine(DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + " " + s);
                            }

                            if (e != null)
                            {
                                var now = DateTime.Now;
                                w.WriteLine(now.ToString("yyyyMMdd hh:mm:ss") + " " + e.GetType().ToString());
                                w.WriteLine(now.ToString("yyyyMMdd hh:mm:ss") + " " + e.StackTrace);
                            }
                        }
                    }
                    finally
                    {
                        if (File.Exists(location) && new FileInfo(location).Length > (1024 * 1024 * 2))
                        {
                            File.Move(location, location + ".a");
                        }
                    }
                }
            }
            catch { }
        }
    }
}
