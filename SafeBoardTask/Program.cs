using System.Net;
using System.Net.Sockets;

namespace SafeBoardTask
{
    class Program
    {
        const int Port = 8888;
        private static TcpListener listener;
        private const string Address = "127.0.0.1";

        static void Main(string[] args)
        {
            List<Thread> threads = new List<Thread>();
            
            try
            {
                listener = new TcpListener(IPAddress.Parse(Address), Port);
                listener.Start();
                Console.WriteLine("Сервер запущен");
                

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();

                    ClientObject newClient = new ClientObject(client);
                    var newUser = new Thread(newClient.Process);
                    newUser.Start();
                    threads.Add(newUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                //ожидание заврешения потоков 
                if(threads.Count != 0)
                    foreach (var thread in threads)
                        thread.Join();
                
                if(listener != null)
                    listener.Stop();
            }
            
        }
    }
}