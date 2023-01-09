// See https://aka.ms/new-console-template for more information

using Google.Protobuf;
using gRpc.File.Service;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using System.Reflection;

using System.Runtime.CompilerServices;
using static gRpc.File.Service.FileStreamingService;

class Program
{
    //static string gRpcUrl = "http://localhost:5288";
    static string gRpcUrl = "http://localhost/file-service";
    static string dnsURL = "dns:///localhost";
    static async Task Main(string[] args)
    {

        await FileUpload();
       //await FileDownload();
    }
    static async Task FileUpload()
    {
        //var channel = GrpcChannel.ForAddress(dnsURL, 
        //    new GrpcChannelOptions { 
        //        Credentials = ChannelCredentials.Insecure,
        //        ServiceConfig = new ServiceConfig { 
        //            LoadBalancingConfigs = { new RoundRobinConfig() }
        //        }
        //    });
        var channel = GrpcChannel.ForAddress(gRpcUrl);
        var client = new FileStreamingServiceClient(channel);
        var contentRootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        string file = Path.Combine(contentRootPath, "Files", "sample.mp4");
        var fileInfo = new System.IO.FileInfo(file);
        using FileStream fileStream = new FileStream(file, FileMode.Open);

        var content = new BytesContent
        {
            FileSize = fileStream.Length,
            ReadedByte = 0,
            Info = new gRpc.File.Service.FileInfo
            { 
                FileName = Path.GetFileNameWithoutExtension(fileInfo.Name), 
                FileExtension = fileInfo.Extension 
            }
        };

        var upload =   client.Upload();

        byte[] buffer = new byte[2048];

        while ((content.ReadedByte = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            content.Buffer = ByteString.CopyFrom(buffer);
            await upload.RequestStream.WriteAsync(content);
        }
        
        await upload.RequestStream.CompleteAsync();
        var response = await upload;
        fileStream.Close();
    

        

    }
    private static async Task FileDownload()
    {
        var channel = GrpcChannel.ForAddress(gRpcUrl);
        var client = new FileStreamingServiceClient(channel);
        var contentRootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        string downloadPath = Path.Combine(contentRootPath, "DownloadFiles");
        if (!Directory.Exists(downloadPath))
            Directory.CreateDirectory(downloadPath);
        var fileInfo = new gRpc.File.Service.FileInfo
        {
            FileExtension = ".mp4",
            FileName = "sample"
        };

        FileStream fileStream = null;

        var request = client.Download(fileInfo);

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        int count = 0;
        decimal chunkSize = 0;

        while (await request.ResponseStream.MoveNext(cancellationTokenSource.Token))
        {
            if (count++ == 0)
            {
                fileStream = new FileStream(@$"{downloadPath}\{request.ResponseStream.Current.Info.FileName}{request.ResponseStream.Current.Info.FileExtension}", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                fileStream.SetLength(request.ResponseStream.Current.FileSize);
            }

            var buffer = request.ResponseStream.Current.Buffer.ToByteArray();
            await fileStream.WriteAsync(buffer, 0, request.ResponseStream.Current.ReadedByte);

            Console.WriteLine($"{Math.Round(((chunkSize += request.ResponseStream.Current.ReadedByte) * 100) / request.ResponseStream.Current.FileSize)}%");
        }
        Console.WriteLine("completed...");

        await fileStream.DisposeAsync();
        fileStream.Close();
    }
}