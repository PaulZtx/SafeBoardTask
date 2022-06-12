using System.Net.Sockets;
using System.Text;

namespace SafeBoardTask;

public class ClientObject
{
    public TcpClient client;
    private const short Buffer = 1024;
    private static int Id = 1000;
    //Уникальные маршруты, лучше HashSet использовать, но поздно додумался :D
    private List<string> paths;

    //Словарь со всей информацией, которая была обработана сервером
    private Dictionary<int, string> info;


    private List<Thread> threads;
    
    private const string help = "scan_service - запустить сервис\n" +
                                "scan_util scan [path] - начать сканирование по указанному пути\n" +
                                "scan_util status [id] - узнать статус сканирования по id\n";

    //Возможные варианты ответов
    private static string[] response = 
    {
        "Scan service was started.",
        "Scan service didn't start!",
        "Scan task was created with ID: ",
        "Scan task in progress, please wait.",
        "!You have already started the service!",
        "!Wrong Id!",
        "====== Scan result ======"
    };

    //Перечиссления для вариантов ответа сервера 
    enum ResponseType
    {
        StartService = 0,
        NotStart,
        CreateTask,
        TaskInProgress,
        AlredyStartService,
        WrongId,
        Result
    }

    private ScanService? service;

    public ClientObject(TcpClient tcpClient)
    {
        client = tcpClient;
        service = null;
        threads = new List<Thread>();
        paths = new List<string>();
        info = new Dictionary<int, string>();
    }

    /// <summary>
    /// Проверка потоков на работу
    /// </summary>
    private async Task CheckThreadsAsync()
    {
        //Проверяем потоки на работу, если неактивен, то удаляем и добавляем резуьтат работы
        for (int i = 0; i < threads.Count; ++i)
        {
            if (!threads[i].IsAlive)
            {
                int id = Convert.ToInt32(threads[i].Name.Split(" ")[0]);
                
                info[id] = service.operations[id].ToString();
                paths.RemoveAt(paths.IndexOf(threads[i].Name.Split(" ")[1]));
                threads.RemoveAt(threads.IndexOf(threads[i]));
                
                --i;
            }
        }
       
    }
 
    public async void Process()
    {
        NetworkStream stream = null;
        try
        {
            stream = client.GetStream();
            byte[] data = new byte[Buffer];
            
            while (true)
            {

                await CheckThreadsAsync();
                
                string mes = GetMessage(stream);


                string []commands = mes.Split(" ");
               
                    
                switch (commands[0])
                {
                    case "help":
                        data = Encoding.UTF8.GetBytes(help);
                        break;
                    
                    case "scan_service":
                        if (service == null)
                        {
                            service = new ScanService();
                            data = Encoding.UTF8.GetBytes(response[(int) ResponseType.StartService]);
                        }
                        else
                            data = Encoding.UTF8.GetBytes(response[(int) ResponseType.AlredyStartService]);
                        
                        break;
                    
                    case "scan_util":
                        //костыль с минимальной проверкой введеных данных
                        if (commands.Length <= 1)
                        {
                            data = Encoding.UTF8.GetBytes(help);
                            break;
                        }
                        
                        if (commands[1] == "scan")
                        {
                            if (service == null)
                            {
                                data = Encoding.UTF8.GetBytes(response[(int) ResponseType.NotStart]);
                                break;
                            }

                            //Проверяем, что по этому пути идет работа
                            if (paths.Contains(commands[2]))
                            {
                                data = Encoding.UTF8.GetBytes(response[(int) ResponseType.TaskInProgress]);
                                break;
                            }
                            
                            //Если работа не идет, то проверям путь на корректность
                            //и запускаем отдельный поток с проверкой файлов
                            
                            if (Directory.Exists(commands[2]))
                            {
                                int id = Id;
                                Thread newTask = new Thread(() => service.StartScanning(commands[2], id));
                                newTask.Name = id + " " + commands[2];
                                newTask.Start();
                                
                                
                                threads.Add(newTask);
                                paths.Add(commands[2]);
                                
                                
                                data = Encoding.UTF8.GetBytes(response[(int)ResponseType.CreateTask] + Id++);
                            }
                        }

                        if (commands[1] == "status")
                        {
                            await CheckThreadsAsync();
                            
                            if (info.ContainsKey(Convert.ToInt32(commands[2])))
                            {
                                
                                data = Encoding.UTF8.GetBytes(response[(int) ResponseType.Result] + "\n" + 
                                                              info[Convert.ToInt32(commands[2])] + "\n=============\n");
                                break;
                            }

                            bool flag = false;
                            for (int i = 0; i < threads.Count; ++i)
                            {
                                if (threads[i].Name.Split(" ")[0] == commands[2])
                                {
                                    flag = true;
                                    data = Encoding.UTF8.GetBytes(response[(int) ResponseType.TaskInProgress]);
                                    break;
                                }
                            }
                            if(!flag)
                                data = Encoding.UTF8.GetBytes(response[(int) ResponseType.WrongId]);
                        }
                        break;
                    
                    default:
                        data = Encoding.UTF8.GetBytes(help);
                        break;
                }
                //Чтобы не крутить проверку потоков в отдельной потоке,
                //сделал проверку в наиболее важных местах
                await CheckThreadsAsync();
                stream.Write(data, 0, data.Length);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            if (stream != null)
                stream.Close();
            
            if (client != null)
                client.Close();
        }
    }
    
    /// <summary>
    /// Преобразование NetworkStream в string 
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    private string GetMessage(NetworkStream stream)
    {
        StringBuilder builder = new StringBuilder();
        int bytes = 0;
        byte[] data = new byte[Buffer]; 
            
        do
        {
            bytes = stream.Read(data, 0, data.Length);
            builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                
        } while (stream.DataAvailable);

        return builder.ToString();
    }

}