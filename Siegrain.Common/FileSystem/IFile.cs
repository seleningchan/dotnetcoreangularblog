using System;
using System.IO;
using System.Threading.Tasks;

namespace Siegrain.Common.FileSystem
{
    public interface IFile {
        string GetPath();
        string GetName();
        long GetSize();
        
        Task<Stream> OpenReadAsync();
        Task<Stream> OpenWriteAsync();
        
        Task<string> GetPublicUrlAsync(TimeSpan timeout);
    }
}