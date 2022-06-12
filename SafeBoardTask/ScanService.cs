using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

namespace SafeBoardTask;


public class statistics
{

    public statistics()
    {
        Directory = "";
        workTime = TimeSpan.Zero;
        Files = 0;
        jsDetects = 0;
        rmDetects = 0;
        dllDetects = 0;
        Errors = 0;
    }

    public string Directory;
    public int Files;
    public int jsDetects;
    public int rmDetects;
    public int dllDetects;
    public int Errors;
    public TimeSpan workTime;



    public override string ToString()
            {
                return $"Directory: {Directory}\nProcessed files: {Files}\n" +
                       $"JS detects: {jsDetects}\nrm -rf detects:{rmDetects}\n" +
                       $"Rundll32 detects: {dllDetects}\nErrors: {Errors}\n" +
                       $"Exection time: {workTime}";
            }
}

    public class ScanService
    {
        public Dictionary<int, statistics> operations;
        public ScanService()
        {
            
            operations = new Dictionary<int, statistics>();
        }

        /// <summary>
        /// Сканирование файлов в текущей директории
        /// Чтобы пройтись и по всем папкам можно использовать обход в глубину
        /// </summary>
        /// <param name="path"></param>
        /// <param name="id"></param>
        public void StartScanning(string path, int id)
        {
            var timeWork = new Stopwatch();
            timeWork.Start();
           
            var statistic = new statistics();
            statistic.Directory = path;
            //string[] dirs = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);
            
            CheckFiles(files, statistic);

            /*
            for (int i = 0; i < dirs.Length; ++i)
            {
                path += dirs[i];
                dirs = Directory.GetDirectories(path);
                files = Directory.GetFiles(path);
                CheckFiles(files, statistics);
            }
            */
            
            
            timeWork.Stop();
            statistic.workTime = timeWork.Elapsed;
            operations[id] = statistic;
        }

        /// <summary>
        /// Обход всех файлов в директории 
        /// </summary>
        /// <param name="files"></param>
        /// <param name="statistic"></param>
        private void CheckFiles(string[] files, statistics statistic)
        {
            for (int i = 0; i < files.Length; ++i)
            {
                try
                {
                    string type = Path.GetExtension(files[i]);
                    FileStream fstream = File.OpenRead(files[i]);
                    
                    byte[] buffer = new byte[fstream.Length];
                    // считываем данные
                    fstream.Read(buffer, 0, buffer.Length);
                    // декодируем байты в строку
                    string textFromFile = Encoding.Default.GetString(buffer);

                    if (type == ".js")
                    {
                        if (textFromFile.IndexOf(@"<script>evil_script()</script>") != -1)
                            statistic.jsDetects += 1;
                    }
                    else
                    {
                        if (textFromFile.IndexOf(@"rm -rf %userprofile%\Documents") != -1)
                            statistic.rmDetects += 1;
                        

                        if (textFromFile.IndexOf(@"Rundll32 sus.dll SusEntry") != -1)
                            statistic.dllDetects += 1;
                    }

                    statistic.Files += 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    statistic.Errors += 1;
                }
            }
        }

        
    }