using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace TBot.Service
{
    public class StorageService: IStorageService
    {
        private readonly Lazy<CloudBlobClient> _blobClient;
        private readonly string _storageBaseUrl;

        public StorageService(string connectionString, string storageBaseUrl)
        {
            _blobClient = new Lazy<CloudBlobClient>(() =>
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();

                return blobClient;
            });

            _storageBaseUrl = storageBaseUrl;
        }

        #region " Public "

        public async Task UploadAsync(string containerName, string blobName, string content)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName, containerName);

            //Upload
            await blockBlob.UploadTextAsync(content);
        }

        public async Task<string> DownloadAsync(string containerName, string blobName)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName, containerName);

            //Download

            var text = await blockBlob.DownloadTextAsync();
            return text;
        }

        public async Task DeleteAsync(string containerName, string blobName)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName, containerName);

            //Delete
            await blockBlob.DeleteAsync();
        }

        public async Task<bool> ExistsAsync(string containerName, string blobName)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName, containerName);

            //Exists
            return await blockBlob.ExistsAsync();
        }

        public async Task<string> FetchAttributesAsync(string containerName, string blobName)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName, containerName);

            //Attributes
            await blockBlob.FetchAttributesAsync();

            return blockBlob.Properties.ContentType;
        }

        public MemoryStream SerializeToStream(object o)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            return stream;
        }

        public object DeserializeFromStream(MemoryStream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            object o = formatter.Deserialize(stream);
            return o;
        }

        #endregion

        #region " Private "

        private async Task<CloudBlobContainer> GetContainerAsync(string container)
        {
            //Container
            CloudBlobContainer blobContainer = _blobClient.Value.GetContainerReference(container);
            await blobContainer.CreateIfNotExistsAsync();

            return blobContainer;
        }

        private async Task<CloudBlockBlob> GetBlockBlobAsync(string blobName, string container)
        {
            //Container
            CloudBlobContainer blobContainer = await GetContainerAsync(container);

            //Blob
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);

            return blockBlob;
        }

        #endregion
    }
}
