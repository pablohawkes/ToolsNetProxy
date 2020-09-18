using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace ToolsNetProxy
{
    // Forked from Bruno García's blog. Credits to him:
    // https://blog.brunogarcia.com/2012/10/simple-tcp-forwarder-in-c.html


    public class TcpPortForwarder
    {
        private readonly Socket _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        List<ReplaceString> ReplaceString = new List<ReplaceString>();
        string forwarderName = "";

        public void Start(IPEndPoint local, IPEndPoint remote, List<ReplaceString> replaceString, string ForwarderName)
        {
            try
            {
                ReplaceString = replaceString;
                forwarderName = ForwarderName;

                _mainSocket.Bind(local);
                _mainSocket.Listen(10);

                while (true) //TODO: How to clean stop thread?
                {
                    var source = _mainSocket.Accept();

                    var destination = new TcpPortForwarder();
                    destination.forwarderName = forwarderName;
                    destination.ReplaceString = replaceString;

                    var state = new State(source, destination._mainSocket);

                    log.Debug("Connecting to destination...");
                    destination.Connect(remote, source);
                    log.Debug("Connected. Waiting for client...");

                    source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
                    log.Debug("Message received.");
                }
            }
            catch (Exception exc)
            {
                log.Error("Error on Forwarder " + forwarderName, exc);
            }
        }

        private void Connect(EndPoint remoteEndpoint, Socket destination)
        {
            //var ClientIpAddress = ((IPEndPoint)_mainSocket.RemoteEndPoint).Address.ToString();
            //var ClientPort = ((IPEndPoint)_mainSocket.RemoteEndPoint).Port.ToString();

            //var ToolsnetIpAddress = ((IPEndPoint)destination.RemoteEndPoint).Address.ToString();
            //var ToolsnetPort = ((IPEndPoint)destination.RemoteEndPoint).Port.ToString();

            //log.Info("Client connected: " + ClientIpAddress + ":" + ClientPort);

            var state = new State(_mainSocket, destination);

            _mainSocket.Connect(remoteEndpoint);

            //log.Info("Connected to Toolsnet: " + ToolsnetIpAddress + ":" + ToolsnetPort);

            _mainSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnDataReceive, state);
        }

        private void OnDataReceive(IAsyncResult result)
        {
            log.Debug("OnDataReceive: Message received");
            var state = (State)result.AsyncState;
            try
            {
                var bytesRead = state.SourceSocket.EndReceive(result);
                if (bytesRead > 0)
                {
                    var textoRecibido = System.Text.Encoding.Default.GetString(state.Buffer).Substring(0, bytesRead);
                    log.Debug(forwarderName + " - Source msg  : " + CleanStringForLog(textoRecibido));

                    foreach (var repl in ReplaceString)
                    {
                        textoRecibido = textoRecibido.Replace(repl.OriginalString, repl.NewString); //IP ToolsNet, IP privada Notebook
                    }

                    var textoSinLength = textoRecibido.Substring(4, textoRecibido.Length - 4);
                    byte[] intBytes = BitConverter.GetBytes(textoSinLength.Length);

                    //TODO: I don't know if is necessary:
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(intBytes);

                    byte[] buffer = Combine(intBytes, Encoding.ASCII.GetBytes(textoSinLength));

                    log.Debug(forwarderName + " - Modified msg: " + CleanStringForLog(textoRecibido));

                    state.DestinationSocket.Send(buffer, textoRecibido.Length, SocketFlags.None);

                    state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
                }
            }
            catch (Exception exc)
            {
                log.Error("Error on OnDataReceive", exc);
                state.DestinationSocket.Close();
                state.SourceSocket.Close();
            }
        }

        static string CleanStringForLog(string text)
        {
            Char[] ca = text.ToCharArray();
            text = "";
            foreach (Char c in ca)
            {
                try
                {
                    if ((int)c == 0)    //Null
                        text += "<00>";
                    else if ((int)c > 0 && (int)c <= 31) //Control chars
                        text += "<" + Convert.ToByte(c).ToString("X2") + ">";
                    else if ((int)c > 31 && (int)c <= 126) //Regular chars
                        text += c;
                    else // c > 126 //Extended chars
                        text += "<" + Convert.ToByte(c).ToString("X2") + ">";
                }
                catch { text += "<??>"; }
            }
            return text;
        }

        public byte[] Combine(byte[] first, byte[] second)
        {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }


        private class State
        {
            public Socket SourceSocket { get; private set; }
            public Socket DestinationSocket { get; private set; }
            public byte[] Buffer { get; private set; }

            public State(Socket source, Socket destination)
            {
                SourceSocket = source;
                DestinationSocket = destination;
                Buffer = new byte[8192];
            }
        }
    }
}