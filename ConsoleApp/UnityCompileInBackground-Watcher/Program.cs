using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnityCompileInBackground_Watcher {
    public static class Program {
        static int m_waitTime;
        static bool hasSendMessage;
        static MessageSender sender;

        static void Main(string[] args) {
            var options = new string[] { "-path", "-w", "-port" };
            var result = options.ToDictionary(c => c.Substring(1), c => args.SkipWhile(a => a != c).Skip(1).FirstOrDefault());
            var path = result["path"];

            m_waitTime = int.Parse(result["w"]);

            var notifyFilter =
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName;

            var watcher = new FileSystemWatcher(path, "*.cs") {
                NotifyFilter = notifyFilter,
                IncludeSubdirectories = true,
            };

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            watcher.EnableRaisingEvents = true;

            var port = int.Parse(result["port"]);
            sender = new MessageSender(port);
            sender.Listen();
        }

        static string Format(FileSystemEventArgs e) {
            return $"{e.Name}";
        }

        static async void OnChanged(object who, FileSystemEventArgs e) {
            await Task.Delay(m_waitTime);
            sender.Send("OnChanged " + Format(e));
            hasSendMessage = true;
        }

        static async void OnRenamed(object who, RenamedEventArgs e) {
            await Task.Delay(m_waitTime);
            sender.Send("OnRenamed " + Format(e));
            hasSendMessage = true;
        }
    }
}
