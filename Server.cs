using System.Net;
using System.Text;

namespace KSiS_3;

public class Server
{
    private static string dir = Path.GetFullPath("filesstorage") + "\\";
    private static int _status = 200;
    private static byte[] response;

    private void DoGet(string filename)
    {
        response = File.ReadAllBytes(Path.Combine(dir, filename));
    }
    private void HandleRequest(object state)
    {
        HttpListenerContext context = (HttpListenerContext)state;
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        string requestUrl = request.RawUrl;
        string requestType = request.HttpMethod;
        Console.WriteLine(request.HttpMethod + " " + requestUrl);
        string filename = Path.GetFileName(requestUrl);
        try
        {
            switch (requestType)
            {
                case "GET":
                    DoGet(filename);
                    break;
                // case "POST":
                //     DoPost(request, filename);
                //     break;
                // case "PUT":
                //     DoPut(request, filename);
                //     break;
                // case "DELETE":
                //     DoDelete(filename);
                //     break;
                // case "MOVE":
                //     DoMove(request, filename);
                //     break;
                // case "COPY":
                //     DoCopy(request, filename);
                //     break;
                default:
                    _status = 405;
                    break;
            }
        }
        catch (FileNotFoundException)
        {
            _status = 404;
        }
        catch (IOException)
        {
            _status = 400;
        }
        response.StatusCode = _status;
        response.OutputStream.Write(Server.response, 0, Server.response.Length);
        response.Close();
    }
    public void CreateHttpListener()
    {
        Console.OutputEncoding = Encoding.UTF8;
        HttpListener listener = new();
        bool isIncorrect;
        do
        {
            try
            {
                Console.WriteLine("Введите номер порта:");
                int port = int.Parse(Console.ReadLine());
                listener = new HttpListener();
                string url = "http://localhost:" + port + "/";
                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine("Сервер с url " + url + " был успешно запущен.");
                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(HandleRequest, context);//Для асинхронного выполнения таски
                }
            }
            catch (HttpListenerException)
            {
                Console.WriteLine("Порт уже занят. Попробуйте еще раз.");
                isIncorrect = true;
            }
            catch (Exception)
            {
                Console.WriteLine("Ошибка создания порта. Попробуйте еще раз.");
                isIncorrect = true;
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        } 
        while (isIncorrect);
    }
    
   
    
    //
    // static void DoPost(HttpListenerRequest request, string filename)
    // {
    //     using (Stream body = request.InputStream)
    //     {
    //         using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
    //         {
    //             string content = reader.ReadToEnd();
    //             File.AppendAllText(Path.Combine(dir, filename), content);
    //         }
    //     }
    // }
    //
    // static void DoPut(HttpListenerRequest request, string filename)
    // {
    //     using (Stream body = request.InputStream)
    //     {
    //         using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
    //         {
    //             string content = reader.ReadToEnd();
    //             File.WriteAllText(Path.Combine(dir, filename), content);
    //         }
    //     }
    // }
    //
    // static void DoDelete(string filename)
    // {
    //     File.Delete(Path.Combine(dir, filename));
    // }
    //
    // static void DoMove(HttpListenerRequest request, string filename)
    // {
    //     string newPath = request.QueryString.Get("newPath");
    //     if (newPath == null)
    //     {
    //         throw new IOException("New path not provided");
    //     }
    //     string newFilename = Path.Combine(newPath, filename);
    //     if (File.Exists(newFilename))
    //     {
    //         throw new IOException("File with new name already exists");
    //     }
    //     File.Move(Path.Combine(dir, filename), newFilename);
    // }
    //
    // static void DoCopy(HttpListenerRequest request, string filename)
    // {
    //     string newPath = request.QueryString.Get("newPath");
    //     string newFilename = request.QueryString.Get("newFilename");
    //     if (newPath == null || newFilename == null)
    //     {
    //         throw new IOException("New path or new filename not provided");
    //     }
    //     string destination = Path.Combine(newPath, newFilename);
    //     if (File.Exists(destination))
    //     {
    //         throw new IOException("File with new name already exists");
    //     }
    //     File.Copy(Path.Combine(dir, filename), destination);
    // }
}
