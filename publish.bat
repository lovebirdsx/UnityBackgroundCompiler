set ConsoleDir=ConsoleApp\UnityCompileInBackground-Watcher
set ConsoleExeDir=%ConsoleDir%\bin\Debug\net6.0
set UnityOutDir=UnityAsset\UnityCompileInBackground\Editor
set Root=%cd%

cd %ConsoleDir%
dotnet publish
cd %Root%
copy %ConsoleExeDir%\UnityCompileInBackground-Watcher.dll %UnityOutDir%\UnityCompileInBackground-Watcher.dll
copy %ConsoleExeDir%\UnityCompileInBackground-Watcher.exe %UnityOutDir%\UnityCompileInBackground-Watcher.exe
copy %ConsoleExeDir%\UnityCompileInBackground-Watcher.runtimeconfig.json %UnityOutDir%\UnityCompileInBackground-Watcher.runtimeconfig.json