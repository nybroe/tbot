using System.IO;
using System.Threading.Tasks;

namespace TBot.Service
{
    public interface IStorageService
    {
        Task UploadAsync(string containerName, string blobName, string content);
        Task<string> DownloadAsync(string containerName, string blobName);
        Task DeleteAsync(string containerName, string blobName);
        Task<bool> ExistsAsync(string containerName, string blobName);
        MemoryStream SerializeToStream(object o);
        object DeserializeFromStream(MemoryStream stream);
    }
}
