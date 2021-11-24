using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace UnityCompileInBackground_Watcher {
    public static class Program {
        static int m_waitTime;
        static bool hasSendMessage;

        static void Main(string[] args) {
            var options = new string[] { "-p", "-w" };
            var result = options.ToDictionary(c => c.Substring(1), c => args.SkipWhile(a => a != c).Skip(1).FirstOrDefault());
            var path = result["p"];

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

            while (true) {
                Thread.Sleep(1000);
            }
        }

        static string Format(FileSystemEventArgs e) {
            return $"{e.Name}";
        }

        static async void OnChanged(object sender, FileSystemEventArgs e) {
            await Task.Delay(m_waitTime);
            Console.WriteLine("OnChanged " + Format(e));
            hasSendMessage = true;
        }

        static async void OnRenamed(object sender, RenamedEventArgs e) {
            await Task.Delay(m_waitTime);
            Console.WriteLine("OnRenamed " + Format(e));
            hasSendMessage = true;
        }
    }
}
