using Google.Protobuf;
using Grpc.Core;
using System.IO;
using System.IO.Compression;

namespace gRpc.File.Service.Services
{
    public partial class gRpcFileService
    {
        private bool compressionEnabled;
        private void DeCompress(FileInfo fileInfo)
        {
            var path = _configuration["FileStoragePath"];
            
            if (compressionEnabled)
            {
                string zipPath = "";
                string uncompressPath = "";
                bool addDot = !fileInfo.FileExtension.Contains(".");
                if(addDot)
                {
                    zipPath = $"{path}/{fileInfo.FileName}.{fileInfo.FileExtension}.zip";
                    uncompressPath = $"{path}/{fileInfo.FileName}.{fileInfo.FileExtension}";
                }
                else
                {
                    zipPath = $"{path}/{fileInfo.FileName}{fileInfo.FileExtension}.zip";
                    uncompressPath = $"{path}/{fileInfo.FileName}{fileInfo.FileExtension}";
                }
                
                using FileStream sourceFile = System.IO.File.OpenRead(zipPath);
                using FileStream destinationFile = System.IO.File.Create(uncompressPath);
                using BrotliStream zipStream = new(sourceFile, CompressionMode.Decompress);
                byte[] buffer = new byte[4068];
                //sourceFile.Read(buffer, 0, buffer.Length);
                //zipStream.Write(buffer, 0, buffer.Length);
                int read = 0;
                while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                    destinationFile.Write(buffer, 0, read);
                zipStream.Close();
                destinationFile.Close();
                sourceFile.Close();
            }
        }
        public override async Task Download(FileInfo request, IServerStreamWriter<BytesContent> responseStream, ServerCallContext context)
        {
            DeCompress(request);
            var path = _configuration["FileStoragePath"];
            string filePath = "";
            if (request.FileExtension.Contains("."))
                filePath = $"{path}/{request.FileName}{request.FileExtension}";
            else
                filePath=$"{path}/{request.FileName}.{request.FileExtension}";
            
            using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4068];
            BytesContent content = new BytesContent
            {
                FileSize = fileStream.Length,
                Info = request,
                ReadedByte = 0
            };
            while ((content.ReadedByte = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                content.Buffer = ByteString.CopyFrom(buffer);
                await responseStream.WriteAsync(content);
            }
            fileStream.Close();
            if (compressionEnabled)
            {
                string uncompressPath = "";
                if (request.FileExtension.Contains("."))
                    uncompressPath = $"{path}/{request.FileName}{request.FileExtension}";
                else
                    uncompressPath = $"{path}/{request.FileName}.{request.FileExtension}";
                System.IO.File.Delete(uncompressPath);
            }
        }
    }
}
