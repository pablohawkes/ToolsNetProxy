using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;

namespace ToolsNetProxy
{
    public class Configuration
    {
        public Forwarders forwarders { get; set; }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Configuration()
        {

            //Get configuration file location:
            var directoryName = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var path = directoryName + @"\Configuration.xml";

            XmlSerializer serializer = new XmlSerializer(typeof(Forwarders));

            using (StreamReader reader = new StreamReader(path))
            {
                forwarders = (Forwarders)serializer.Deserialize(reader);
                reader.Close();
            }
        }
    }

    [XmlRoot("Forwarders")]
    public class Forwarders
    {
        [XmlElement("Forwarder")]
        public List<Forwarder> Forwarder { get; set; }

        public void StartActiveForwarders()
        {
            foreach (var forw in Forwarder)
            {

                if (forw.Active)

                    forw.StartForwarder();
            }
        }
    }

    [XmlRoot("Forwarder")]
    public class Forwarder
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Active")]
        public bool Active { get; set; }

        [XmlAttribute("SourceIp")]
        public string SourceIp { get; set; }

        [XmlAttribute("SourcePort")]
        public int SourcePort { get; set; }

        [XmlAttribute("DestinationIp")]
        public string DestinationIp { get; set; }

        [XmlAttribute("DestinationPort")]
        public int DestinationPort { get; set; }

        [XmlElement("ReplaceString")]
        public List<ReplaceString> ReplaceString { get; set; }

        Thread t;
        TcpPortForwarder TcpForwarder;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void StartForwarder()
        {
            try
            {
                log.Debug("Starting Forwarder " + Name + "...");
                t = new Thread(new ThreadStart(ThreadWorker));
                t.Start();
            }
            catch (Exception exc)
            {
                log.Error("Error on Start Forwarder " + Name, exc);
            }
        }

        public void StopForwarder()
        {
            try
            {
                //TODO, how to stop thread cleanly?
                t.Abort();
                log.Debug("Stopping Forwarder " + Name + "...");
            }
            catch (Exception exc)
            {
                log.Error("Error on Stop Forwarder " + Name, exc);
            }
        }

        private void ThreadWorker()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Parse(SourceIp), SourcePort);
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(DestinationIp), DestinationPort);

            log.Debug("Starting Threadworker: Source: " + SourceIp + ":" + SourcePort.ToString() + " - Destination: " + DestinationIp + ":" + DestinationPort.ToString());
            TcpForwarder = new TcpPortForwarder();
            TcpForwarder.Start(localEndPoint, remoteEndPoint, ReplaceString, Name);
        }
    }

    [XmlRoot("ReplaceString")]
    public class ReplaceString
    {
        [XmlAttribute("OriginalString")]
        public string OriginalString { get; set; }
        [XmlAttribute("NewString")]
        public string NewString { get; set; }
    }
}
