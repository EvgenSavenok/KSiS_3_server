using System.Net;
using System.Text;

namespace KSiS_3;

public class Server
{
    private static string _dir = Path.GetFullPath("filesStorage") + "\\";
    private static int _status = 200;
    private static byte[] _byteResponse;

    private void HadleGet(string filename)
    {
        _byteResponse = File.ReadAllBytes(Path.Combine(_dir, filename));
    }
    
    private void HandlePost(HttpListenerRequest request, string filename)
    {
        using (Stream body = request.InputStream)
        {
            using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
            {
                string content = reader.ReadToEnd();
                File.AppendAllText(Path.Combine(_dir, filename), content);
                _byteResponse = Encoding.UTF8.GetBytes("");
            }
        }
    }
    
    private void HandlePut(HttpListenerRequest request, string filename)
    {
        using (Stream body = request.InputStream)
        {
            using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
            {
                string content = reader.ReadToEnd();
                File.WriteAllText(Path.Combine(_dir, filename), content);
                _byteResponse = Encoding.UTF8.GetBytes("");
            }
        }
    }
    
    private void HandleDelete(string filename)
    {
        File.Delete(Path.Combine(_dir, filename));
        _byteResponse = Encoding.UTF8.GetBytes("");
    }

    private string findParam(string postData, string param)
    {
        string[] pairs = postData.Split('&');
        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                string key = keyValue[0];
                string value = keyValue[1];
                if (key == param)
                    return Uri.UnescapeDataString(value); 
            }
        }
        return null;
    }
    private void HandleCopy(HttpListenerRequest request, string filename)
    {
        string postData = "";
        using (StreamReader reader = new StreamReader(request.InputStream))
        {
            postData = reader.ReadToEnd();
        }
        string newPath = findParam(postData, "newPath");
        if (!Directory.Exists(newPath))
            Directory.CreateDirectory(newPath);
        string newFileName = findParam(postData, "newFileName");
        string newFullPath = Path.Combine(newPath, newFileName);
        File.Copy(Path.Combine(_dir, filename), newFullPath);
        _byteResponse = Encoding.UTF8.GetBytes("");
    }
    
    private void HandleMove(HttpListenerRequest request, string filename)
    {
        string postData = "";
        using (StreamReader reader = new StreamReader(request.InputStream))
        {
            postData = reader.ReadToEnd();
        }
        string newPath = findParam(postData, "newPath");
        if (!Directory.Exists(newPath))
            Directory.CreateDirectory(newPath);
        string newFullPath = Path.Combine(newPath, filename);
        File.Move(Path.Combine(_dir, filename), newFullPath);
        _byteResponse = Encoding.UTF8.GetBytes("");
    }
    
    private void SendResponse(HttpListenerResponse response)
    {
        response.StatusCode = _status;
        response.OutputStream.Write(_byteResponse, 0, _byteResponse.Length);
        response.Close();
    }

    private void DetermineHttpMethod(string requestType, string filename, HttpListenerRequest request)
    {
        switch (requestType)
        {
            case "GET":
                HadleGet(filename);
                break;
            case "POST":
                HandlePost(request, filename);
                break;
            case "PUT":
                HandlePut(request, filename);
                break;
             case "DELETE":
                 HandleDelete(filename);
                 break;
            case "COPY":
                HandleCopy(request, filename);
                break; 
            case "MOVE": 
                HandleMove(request, filename);
                break;
            default:
                _status = 405;
                break;
        }
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
            DetermineHttpMethod(requestType, filename, request);
            SendResponse(response);
        }
        catch (FileNotFoundException)
        {
            _status = 404;
        }
        catch (IOException)
        {
            _status = 400;
        }
    }
    public void CreateHttpListener()
    {
        Console.OutputEncoding = Encoding.UTF8;
        HttpListener listener = new();
        bool isIncorrect = false;
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
                if (!isIncorrect)
                {
                    listener.Stop();
                    listener.Close();
                }
            }
        } 
        while (isIncorrect);
    }
}
