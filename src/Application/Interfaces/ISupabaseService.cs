using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISupabaseService
    {
        Task<string?> UploadImageAsync(IFormFile file, string bucketName, string fileName);
    }
}
