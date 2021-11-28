using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

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
        }

        [MenuItem("Tools/" + nameof(UnityCompileInBackground) + "/" + nameof(RestartWatcherProcess))]
        static void RestartWatcherProcess() {
            KillExistWatcherProcess();
            RunWatcher();
        }

        static Process CreateWatcherProcess() {
            var process = Process.Start(new ProcessStartInfo {
                FileName = Application.dataPath + "/" + ConsoleAppPath,
                UseShellExecute = true,
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
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.quitting += OnEditorQuitting;
            AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnload;
        }

        static void StopListen() {
            if (udpClient != null) {
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
            EditorApplication.quitting -= OnEditorQuitting;
            AppDomain.CurrentDomain.DomainUnload -= OnAppDomainUnload;
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
                Debug.Log($"Compiling for: [{msg}]");
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

            if (listenThread == null || !listenThread.IsAlive) {
                listenThread = new Thread(RecvMessageThread);
                listenThread.Start();
            }
        }

        static void OnAppDomainUnload(object sender, EventArgs e) {
            StopListen();
        }

        static void OnEditorQuitting() {
            StopListen();
            KillExistWatcherProcess();
        }

        static void CheckWatcherExist() {
            if (!IsExistWatcherProcess()) {
                Debug.LogError($"Watcher process start failed, please check your .net runtime is 6.0");
            }
        }

        static void RecvMessageThread() {
            // Maybe the watcher process is not ready, wait a bit
            Thread.Sleep(100);

            var endpoint = new IPEndPoint(IPAddress.Loopback, Port);
            udpClient = new UdpClient();
            
            var data = Encoding.ASCII.GetBytes("Request");
            udpClient.Send(data, data.Length, endpoint);

            var sender = new IPEndPoint(IPAddress.Any, 0);
            
            // ListenThread can be abort by StopListen(), then exit the loop
            try {
                data = udpClient.Receive(ref sender);
                var msg = Encoding.ASCII.GetString(data, 0, data.Length);
                messages.Enqueue(msg);
            } catch (System.Threading.ThreadAbortException) {
                
            }
            udpClient.Close();
            udpClient.Dispose();
            udpClient = null;
        }
    }
}
