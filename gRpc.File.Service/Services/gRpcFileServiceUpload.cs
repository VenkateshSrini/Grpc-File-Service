using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.IO.Compression;

namespace gRpc.File.Service.Services
{
    public partial class gRpcFileService : FileStreamingService.FileStreamingServiceBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<gRpcFileService> _logger;
        public gRpcFileService(IConfiguration configuration, ILogger<gRpcFileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            compressionEnabled = _configuration.GetValue<bool>("Compress");
        }
        private void createFilePath(string path)
        {

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        public override async Task<Empty> Upload(IAsyncStreamReader<BytesContent> requestStream, ServerCallContext context)
        {
            var path = _configuration["FileStoragePath"];
            
            createFilePath(path);
            int count = 0;
            decimal chunkSize = 0;
            FileStream fileStream = null;
            string fileName = default, fileExt = default;
            try
            {
                while (await requestStream.MoveNext())
                {
                    if (count++ == 0)
                    {
                        fileName = requestStream.Current.Info.FileName;
                        fileExt = requestStream.Current.Info.FileExtension;
                        string filePath = "";
                        if (!fileExt.Contains("."))
                            filePath = $"{path}/{fileName}.{fileExt}";
                        else
                            filePath = $"{path}/{fileName}{fileExt}";

                        fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                        fileStream.SetLength(requestStream.Current.FileSize);
                    }

                    var buffer = requestStream.Current.Buffer.ToByteArray();

                    await fileStream.WriteAsync(buffer, 0, requestStream.Current.ReadedByte);

                    _logger.LogInformation($"{Math.Round(((chunkSize += requestStream.Current.ReadedByte) * 100) / requestStream.Current.FileSize)}%");
                    
                }
                   
                
            }
            catch
            {
                throw;
            }
            finally
            {
                fileStream.Close();
                await fileStream.DisposeAsync();

            }
            try
            {
                if (compressionEnabled)
                    await CompressFileStream(path, fileName, fileExt);
            }
            catch { throw ; }
            
            return new Empty();

        }
        private async Task CompressFileStream(string path, string fileName, string fileExtension)
        {
            bool addDot = !fileExtension.Contains(".");
            string uncompressPath = "";
            string zipPath = "";

            if (addDot)
            {
                uncompressPath = $"{path}/{fileName}.{fileExtension}";
                zipPath = $"{path}/{fileName}.{fileExtension}.zip";
            }
            else
            {
                uncompressPath = $"{path}/{fileName}{fileExtension}";
                zipPath = $"{path}/{fileName}{fileExtension}.zip";
            }



            using FileStream sourceFile = System.IO.File.OpenRead(uncompressPath);
            using FileStream destinationFile = System.IO.File.Create(zipPath);
            using BrotliStream zipStream = new(destinationFile, CompressionMode.Compress);
            byte[] buffer = new byte[4068];
            //sourceFile.Read(buffer, 0, buffer.Length);
            int readPos;
            while ((readPos = await sourceFile.ReadAsync(buffer, 0, buffer.Length)) > 0)
               await zipStream.WriteAsync(buffer, 0, buffer.Length);

            zipStream.Close();
            destinationFile.Close();
            sourceFile.Close();
            System.IO.File.Delete(uncompressPath);

        }
    }
}
