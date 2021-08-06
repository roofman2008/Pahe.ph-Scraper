using System;

namespace PaheScrapper.Helpers
{
    using System.IO;
    using System.Reflection;


    public class LogWriter
    {
        private string m_exePath = string.Empty;
        public LogWriter(string logMessage)
        {
            LogWrite(logMessage);
        }
        public void LogWrite(string logMessage)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "log.txt"))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
                // ignored
            }
        }

        public void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("Log Entry: ");
                txtWriter.Write("{0} {1}", DateTime.Now.ToShortTimeString(),DateTime.Now.ToShortDateString());
                txtWriter.Write(" - {0}", logMessage);
                txtWriter.WriteLine();
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
    }
}