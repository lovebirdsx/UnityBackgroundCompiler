using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace lovebird {

    public static class UnityCompileInBackground {
        const string ProcessName = "UnityCompileInBackground-Watcher";
        const string ConsoleAppPath = @"Plugins/UnityCompileInBackground/Editor/" + ProcessName + ".exe";
        const int minRefreshInterval = 5;
        static Process process;
        static bool needRefresh;

        static void KillExistWatcherProcess() {
            var processes = Process.GetProcessesByName(ProcessName);
            for (int i = 0; i < processes.Length; i++) {
                processes[i].Kill();
            }
        }

        static Process CreateWatcherProcess() {
            var dataPath = Application.dataPath;
#if UNITY_EDITOR_OSX
            var filename = "/usr/local/bin/fswatch";
#else
            var filename = dataPath + "/" + ConsoleAppPath;
#endif
            var path = Application.dataPath;
#if UNITY_EDITOR_OSX
            var arguments = string.Format(@"-x ""{0}""", path);
#else
            var arguments = string.Format(@"-p ""{0}"" -w 0", path);
#endif
            var windowStyle = ProcessWindowStyle.Hidden;

            var info = new ProcessStartInfo {
                FileName = filename,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = windowStyle,
                Arguments = arguments,
            };

            var p = Process.Start(info);
            return p;
        }

        [InitializeOnLoadMethod]
        static void Init() {
            KillExistWatcherProcess();
            process = CreateWatcherProcess();
            process.OutputDataReceived += OnReceived;
            process.BeginOutputReadLine();

            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate() {
            if (!needRefresh) {
				return;
			}

            if (EditorApplication.isCompiling) {
				return;
			}

            if (EditorApplication.isUpdating) {
				return;
			}

            var now = EditorApplication.timeSinceStartup;
            AssetDatabase.Refresh();
            Debug.Log($"[UnityCompileInBackground] Compiling time = {EditorApplication.timeSinceStartup - now:0.##}s");
            needRefresh = false;
        }

        static void OnReceived(object sender, DataReceivedEventArgs e) {
            var message = e.Data;

#if UNITY_EDITOR_OSX
            if (message.Contains("Created") || message.Contains("Renamed") || message.Contains("Updated") || message.Contains("Removed")) {
                Debug.Log($"Receive message {e.Data}");
                m_isRefresh = true;
            }
#else
            if (message.Contains("OnChanged") || message.Contains("OnRenamed")) {
                Debug.Log($"Receive message {e.Data}");
                needRefresh = true;
            }
#endif
        }
    }
}
