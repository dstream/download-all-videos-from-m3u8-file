using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;

namespace DownloadBathFile
{
    class Program
    {
        static void Main(string[] args)
        {
            //To get the location the assembly normally resides on disk or the install directory
            string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;

            //once you have the path you get the directory with:
            var directory = Path.GetDirectoryName(path).Substring(6);

            var fileLink = Directory.GetFiles(directory).FirstOrDefault(e => Path.GetExtension(e) == ".m3u8");
            if (fileLink == null)
            {
                Console.WriteLine("No m3u8 file found!");
                Console.ReadKey();
                return;
            }
            var file = new StreamReader(fileLink);
            string line;
            var urlList = new List<string>();
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("http:"))
                {
                    urlList.Add(line);
                }
            }
            new DownloadHelper(directory, "mp4", 8).DownloadFiles(urlList);
            Console.WriteLine("Press q to quit");
            while (Console.ReadLine() != "q")
            {
                Console.WriteLine("Press q to quit");
            }
        }

        public class DownloadHelper
        {
            private string _targetDir;
            private string _fileExtension;
            private int _numberOfThread;

            public DownloadHelper() { }
            public DownloadHelper(string targetDir, string fileExtension, int numberOfThread)
            {
                _targetDir = targetDir;
                _fileExtension = fileExtension;
                if (numberOfThread < 1)
                {
                    _numberOfThread = 1;
                }
                else {
                    _numberOfThread = numberOfThread;
                }
            }

            public void DownloadFiles(List<string> urlList)
            {                                
                urlList = urlList.Distinct().ToList();
                _result = new Dictionary<string, string>();
                //build result list
                for (var i=0;i<urlList.Count;i++)
                {
                    var targetFileName = $"file-{GetFileNameIndex(i + 1, urlList.Count)}.{_fileExtension}";
                    var filePath = Path.Combine(_targetDir, targetFileName);
                    var fileInfo = new FileInfo(filePath);
                    if(fileInfo.Exists && fileInfo.Length != 0)
                    {
                        continue;
                    }
                    _result.Add(urlList[i], filePath);
                }

                Stack = new Stack<string>(_result.Keys);                

                if(Stack.Count < _numberOfThread)
                {
                    _numberOfThread = Stack.Count;
                }

                for (int i = 0; i < _numberOfThread; i++)
                {
                    var WC = new WebClient();
                    WC.DownloadFileCompleted += Wc_Complete;
                    string currFile = Stack.Pop();                                                                 
                    WC.DownloadFileAsync(new Uri(currFile), _result[currFile]);                    
                    _runningThread++;                    
                }
            }
            private int _runningThread;
            private Object thisLock = new Object();            
            private Dictionary<string, string> _result;
            private Stack<string> Stack;
            private int _numberOfTryAgain = 0;
            private void Wc_Complete(object sender, AsyncCompletedEventArgs e)
            {                
                lock (thisLock)
                {
                    _runningThread--;
                    if (Stack != null && Stack.Count > 0)
                    {
                        Console.WriteLine($"Completed  {Math.Round(((_result.Count - Stack.Count) / (_result.Count * 1.0)) * 100)}% - {_runningThread} thread(s) running");

                        var WC = new WebClient();
                        WC.DownloadFileCompleted += Wc_Complete;

                        string currFile = Stack.Pop();                        
                        WC.DownloadFileAsync(new Uri(currFile), _result[currFile]);
                        _runningThread++;
                    }
                    else
                    {
                        if (_runningThread == 0)
                        {
                            if (VefifyResult())
                            {
                                Stack = null;
                                Console.WriteLine("All files downloaded");                                
                            }
                        }
                        else
                            Console.WriteLine($"Completed  99% - {_runningThread} thread(s) running");
                    }
                }
            }

            private string GetFileNameIndex(int number, int max)
            {
                var maxstr = max.ToString();
                var numberStr = number.ToString();
                var numerStrLength = numberStr.Length;
                for (int i = 0; i < maxstr.Length - numerStrLength; i++)
                {
                    numberStr = "0" + numberStr;
                }
                return numberStr;
            }

            private bool VefifyResult() {
                var verifyResult = new Dictionary<string, string>();
                foreach (var r in _result)
                {
                    var file = new FileInfo(r.Value);
                    if(!file.Exists || file.Length == 0)
                    {
                        verifyResult.Add(r.Key, r.Value);
                    }
                }
                if (verifyResult.Any())
                {
                    //download again (3 times)
                    if (_numberOfTryAgain > 2) {
                        Console.WriteLine("There's some dead links: ");
                        foreach(var r in verifyResult) {
                            Console.WriteLine(r.Key);
                            Console.WriteLine("local path:"  + r.Value);
                        }
                        return true;
                    }
                    _numberOfTryAgain++;                    
                    _result = new Dictionary<string, string>(verifyResult);
                    Stack = new Stack<string>(verifyResult.Keys);

                    if (Stack.Count < _numberOfThread)
                    {
                        _numberOfThread = Stack.Count;
                    }

                    for (int i = 0; i < _numberOfThread; i++)
                    {
                        var WC = new WebClient();
                        WC.DownloadFileCompleted += Wc_Complete;
                        string currFile = Stack.Pop();
                        WC.DownloadFileAsync(new Uri(currFile), _result[currFile]);
                        _runningThread++;
                    }
                    return false;
                }
                return true;
            }
        }
    }
}
