using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace InterviewWizard.Models.Storage
{
    public class Blob
    {
        private readonly BlobContainerClient _blobContainerClient;
        private HttpContext httpContext => new HttpContextAccessor().HttpContext;

        public Blob(BlobContainerClient blobContainerClient)
        {
            _blobContainerClient = blobContainerClient;
        }

        public async Task<BlobFile?> GetBlob(string filename)
        {
            var blobClient = _blobContainerClient.GetBlobClient(filename);
            MemoryStream fileStream = new MemoryStream();
            Response<BlobDownloadResult> blobDownloadInfo = await blobClient.DownloadContentAsync();
            if (blobDownloadInfo != null && blobDownloadInfo.Value != null)
            {
                var fileData = await blobClient.GetPropertiesAsync();
                BlobFile blobFile = new BlobFile()
                {
                    Filename = filename,
                    Content = Encoding.UTF8.GetString( blobDownloadInfo.Value.Content),
                    DateUploaded = fileData.Value.Metadata.ContainsKey("dateUploaded") ? DateTime.Parse(fileData.Value.Metadata["dateUploaded"]) : null
                };
                return blobFile;
            }
            else
            {
                return null;
            }
        }

        public async Task<BlobFile> SaveBlob(BlobFile file)
        {
            file = await SaveBlob(file, null);
            return file;
        }

        public async Task<BlobFile> SaveBlob(BlobFile file, string? extension)
        {
            string filename = extension == null ? Guid.NewGuid().ToString("n") : Guid.NewGuid().ToString("n") + $".{extension}"; ;
            var blobClient = _blobContainerClient.GetBlobClient(filename);
            string? sessionKey = httpContext.Session.GetString("SessionKey");
            if (sessionKey == null)
            {
                throw new Exception("Session key not found in session");
            }
            var blobTags = new Dictionary<string, string>()
            {
                {"dateUploaded", file.DateUploaded.HasValue ? file.DateUploaded.Value.ToString("o") : DateTime.UtcNow.ToString("o")},
                {"sessionKey", sessionKey}
            };
            Byte[] uploadContent = file.Content == null ? new Byte[0] : Encoding.UTF8.GetBytes(file.Content);
            await blobClient.UploadAsync(new MemoryStream(uploadContent));
            await blobClient.SetTagsAsync(blobTags);
            file.Filename = blobClient.Name;
            file.Url = blobClient.Uri.ToString();
            return file;
        }

        public async Task<List<object>> ListFiles()
        {
            var files = new List<object>();
            string tagQuery = "sessionKey = '" + httpContext.Session.GetString("SessionKey") + "'";
            List<TaggedBlobItem> sessionBlobs = new List<TaggedBlobItem>();
            await foreach (var blobItem in _blobContainerClient.FindBlobsByTagsAsync(tagQuery))
            {
                sessionBlobs.Add(blobItem);
            }
            foreach (var blobItem in sessionBlobs)
            {
                var blobClient = _blobContainerClient.GetBlobClient(blobItem.BlobName);
                var blobProperties = await blobClient.GetPropertiesAsync();
                var blobTags = await blobClient.GetTagsAsync();
                files.Add(new
                {
                    filename = blobItem.BlobName,
                    url = blobClient.Uri.ToString(),
                    dateUploaded = blobTags.Value.Tags.ContainsKey("dateUploaded") ? blobTags.Value.Tags["dateUploaded"] : null,
                    sessionKey = blobTags.Value.Tags.ContainsKey("sessionKey") ? blobTags.Value.Tags["sessionKey"] : null
                });
            }
            return files;
        }
    }
}
