using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace lovebird {

    public static class UnityCompileInBackground {
        const string ProcessName = "UnityCompileInBackground-Watcher";
        const string ConsoleAppPath = @"Plugins/UnityCompileInBackground/Editor/" + ProcessName + ".exe";
        public const int Port = 8998;

        static ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        static double lastCompileTime;

        [InitializeOnLoadMethod]
        static void Start() {
            RunWatcher();
            Listen();
        }

        [MenuItem("Tools/" + nameof(UnityCompileInBackground) + "/" + nameof(RestartWatcherProcess))]
        static void RestartWatcherProcess() {
            KillExistWatcherProcess();
            RunWatcher();
        }

        static Process CreateWatcherProcess() {
            var process = Process.Start(new ProcessStartInfo {
                FileName = Application.dataPath + "/" + ConsoleAppPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = @$"-path ""{Application.dataPath}"" -port ""{Port}"" -w 0",
            });
            return process;
        }

        static void KillExistWatcherProcess() {
            var processes = Process.GetProcessesByName(ProcessName);
            foreach (var p in processes) {
                p.Kill();
            }
        }

        static bool IsExistWatcherProcess() {
            var processes = Process.GetProcessesByName(ProcessName);
            return processes.Length > 0;
        }

        static void RunWatcher() {
            if (IsExistWatcherProcess()) {
                return;
            }

            var process = CreateWatcherProcess();
            Debug.Log($"Watcher process started [{process.Id}]");
            EditorApplication.quitting += () => {
                process.Kill();
                process.Dispose();
            };
        }

        static void Listen() {
            var thread = new Thread(Run);
            thread.Start();
            // Debug.Log($"UnityCompileInBackground running on port {Port}");
            EditorApplication.update += OnEditorUpdate;
        }

        static void TryRecompile(string msg) {
            if (EditorApplication.isCompiling) {
				return;
			}

            if (EditorApplication.isUpdating) {
				return;
			}

            if (EditorApplication.timeSinceStartup - lastCompileTime > 5) {
                lastCompileTime = EditorApplication.timeSinceStartup;
                AssetDatabase.Refresh();
                Debug.Log($"Compiling: [{msg}]");
            }
        }

        static void OnEditorUpdate() {
            if (messages.TryDequeue(out var msg)) {
                if (msg.Contains("OnChanged") || msg.Contains("OnRenamed")) {
                    TryRecompile(msg);
                } else {
                    Debug.LogWarning($"Unsuporrted msg {msg}");
                }
            }
        }

        static void Run() {
            byte[] data = new byte[1024];
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Port);
            UdpClient newsock = new UdpClient(endpoint);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            while (true) {
                data = newsock.Receive(ref sender);
                var msg = Encoding.ASCII.GetString(data, 0, data.Length);
                messages.Enqueue(msg);
            }
        }
    }
}
