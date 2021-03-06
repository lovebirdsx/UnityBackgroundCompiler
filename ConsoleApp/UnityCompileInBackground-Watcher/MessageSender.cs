using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnityCompileInBackground_Watcher {
    public class MessageSender {
        UdpClient udpClient;
        IPEndPoint remote;

        public MessageSender(int dstPort) {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, dstPort);
            udpClient = new UdpClient(endpoint);
            Console.WriteLine($"Listen at {endpoint}");
        }

        public void Listen() {
            var sender = new IPEndPoint(IPAddress.Any, 0);
            while (true) {
                try {
                    var data = udpClient.Receive(ref sender);
                    var msg = Encoding.ASCII.GetString(data, 0, data.Length);
                    if (msg == "Request") {
                        remote = sender;
                        Console.WriteLine($"{remote} Connected");
                    }
                } catch (SocketException) {
                    // Ignore socket exception
                } catch (System.Exception e) {
                    Console.WriteLine(e);
                }
            }
        }

        public void Send(string msg) {
            if (remote == null) {
                return;
            }

            var bytes = Encoding.ASCII.GetBytes(msg);
            udpClient.Send(bytes, bytes.Length, remote);
            Console.WriteLine($"Send {remote}: {msg}");
        }

        public void Dispose() {
            udpClient.Dispose();
        }
    }
}