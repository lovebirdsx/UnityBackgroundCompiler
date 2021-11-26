using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEditor.Compilation;

namespace lovebird {

    public static class UnityCompileInBackground {
        const string ProcessName = "UnityCompileInBackground-Watcher";
        const string ConsoleAppPath = @"Plugins/UnityCompileInBackground/Editor/" + ProcessName + ".exe";
        public const int Port = 8998;

        static ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        static double lastCompileTime;
        static Thread listenThread;
        static UdpClient udpClient;
        static double watcherCheckTime;
        static bool watcherChecked;

        [InitializeOnLoadMethod]
        static void Start() {
            RunWatcher();
            StopListen();
            StartListen();
        }

        [MenuItem("Tools/" + nameof(UnityCompileInBackground) + "/" + nameof(TestSendMessage))]
        static void TestSendMessage() {
            var udpClient = new UdpClient();
            udpClient.Connect("127.0.0.1", Port);
            var data = Encoding.ASCII.GetBytes("Test from UnityCompileInBackground");
            udpClient.Send(data, data.Length);
        }

        [MenuItem("Tools/" + nameof(UnityCompileInBackground) + "/" + nameof(RestartListen))]
        static void RestartListen() {
            StopListen();
            StartListen();
            Debug.Log($"UnityCompileInBackground running on port {Port}");
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
            watcherCheckTime = EditorApplication.timeSinceStartup + 2;
            Debug.Log($"Watcher process started [{process.Id}]");
        }

        static void StartListen() {
            listenThread = new Thread(Run);
            listenThread.Start();
            // Debug.Log($"UnityCompileInBackground running on port {Port}");

            EditorApplication.update += OnEditorUpdate;
            EditorApplication.quitting += KillExistWatcherProcess;
            CompilationPipeline.assemblyCompilationStarted += OnCompilationStarted;
        }

        static void StopListen() {
            if (udpClient != null) {
                if (udpClient.Client.Connected) {
                    udpClient.Client.Disconnect(true);
                    udpClient.Client.Shutdown(SocketShutdown.Both);
                }
                udpClient.Close();
                udpClient.Dispose();
                udpClient = null;
            }

            if (listenThread != null) {
                listenThread.Abort();
                listenThread.Join();
                listenThread = null;
            }

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.quitting -= KillExistWatcherProcess;
            CompilationPipeline.assemblyCompilationStarted -= OnCompilationStarted;
        }

        static void OnCompilationStarted(string obj) {
            StopListen();
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
            if (!watcherChecked) {
                if (EditorApplication.timeSinceStartup > watcherCheckTime) {
                    CheckWatcherExist();
                    watcherChecked = true;
                }
            }

            if (messages.TryDequeue(out var msg)) {
                if (msg.Contains("OnChanged") || msg.Contains("OnRenamed")) {
                    TryRecompile(msg);
                } else if (msg.Contains("Test")) {
                    Debug.Log(msg);
                } else {
                    Debug.LogWarning($"Unsuporrted msg {msg}");
                }
            }
        }

        static void CheckWatcherExist() {
            if (!IsExistWatcherProcess()) {
                Debug.LogError($"Watcher process start failed, please check your .net runtime is 6.0");
            }
        }

        static void Run() {
            byte[] data = new byte[1024];
            var endpoint = new IPEndPoint(IPAddress.Any, Port);
            udpClient = new UdpClient(endpoint);
            var sender = new IPEndPoint(IPAddress.Any, 0);

            while (true) {
                data = udpClient.Receive(ref sender);
                var msg = Encoding.ASCII.GetString(data, 0, data.Length);
                messages.Enqueue(msg);
            }
        }
    }
}
