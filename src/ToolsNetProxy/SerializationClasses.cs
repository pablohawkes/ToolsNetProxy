using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
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
        public List<ReplaceString> replaceString { get; set; }

        Thread t;
        private Socket _mainSocket;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void StartForwarder()
        {
            t = new Thread(new ThreadStart(ThreadWorker));
            t.Start();
        }

        private void ThreadWorker()
        {
            /* TODO
            var LocalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"))
            _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); ;

            _mainSocket.Bind(local);
            _mainSocket.Listen(10);

            while (true)
            {
                var source = _mainSocket.Accept();
                var destination = new TcpForwarderSlim();
                var state = new State(source, destination._mainSocket);
                destination.Connect(remote, source);
                source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
            */
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
