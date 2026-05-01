using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Supabase;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SupabaseService : ISupabaseService
    {
        private readonly Client _supabaseClient;
        private readonly string _supabaseUrl;

        public SupabaseService(Client supabaseClient, IConfiguration configuration)
        {
            _supabaseClient = supabaseClient;
            _supabaseUrl = configuration["Supabase:Url"] ?? throw new ArgumentNullException("Supabase:Url");
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string bucketName, string fileName)
        {
            if (file == null || file.Length == 0) return null;

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                var storage = _supabaseClient.Storage.From(bucketName);
                
                // Upload the file
                var result = await storage.Upload(bytes, fileName, new Supabase.Storage.FileOptions
                {
                    CacheControl = "3600",
                    Upsert = true
                });

                // Generate public URL
                var publicUrl = storage.GetPublicUrl(fileName);
                return publicUrl;
            }
            catch (Exception ex)
            {
                // In a real app, log the exception
                Console.WriteLine($"Error uploading to Supabase: {ex.Message}");
                return null;
            }
        }
    }
}
