using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnityCompileInBackground_Watcher {
    public class MessageSender {
        UdpClient client;

        public MessageSender(int dstPort) {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, dstPort);
            client = new UdpClient();
            client.Connect(endpoint);
        }

        public void Send(string msg) {
            var bytes = Encoding.ASCII.GetBytes(msg);
            client.Send(bytes, bytes.Length);
        }

        public void Dispose() {
            client.Dispose();
        }
    }
}