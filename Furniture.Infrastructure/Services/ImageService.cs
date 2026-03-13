using CloudinaryDotNet;
using Furniture.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;
using CloudinaryDotNet.Actions;

namespace Furniture.Infrastructure.Services
{
    public class ImageService : IImageService
    {
 private readonly Cloudinary _cloudinary;

        public ImageService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey    = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            Console.WriteLine($"=== Cloudinary Config ===");
            Console.WriteLine($"CloudName: {cloudName}");
            Console.WriteLine($"ApiKey:    {apiKey}");
            Console.WriteLine($"ApiSecret: {apiSecret}");

            if (string.IsNullOrEmpty(cloudName) || 
                string.IsNullOrEmpty(apiKey)    || 
                string.IsNullOrEmpty(apiSecret))
                throw new Exception("Cloudinary credentials missing!");

            var account     = new Account(cloudName, apiKey, apiSecret);
            _cloudinary     = new Cloudinary(account);
            _cloudinary.Api.Secure  = true;
            _cloudinary.Api.Timeout = 180000;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid image file.");

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new ArgumentException("Only JPEG, PNG, and WebP images are allowed.");

            if (file.Length > 10 * 1024 * 1024) // raise limit to 10MB input
                throw new ArgumentException("Image size must not exceed 10MB.");

            // Compress locally before uploading
            using var compressedStream = await CompressImageAsync(file);

            Console.WriteLine($"Original size: {file.Length / 1024}KB");
            Console.WriteLine($"Compressed size: {compressedStream.Length / 1024}KB");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, compressedStream),
                Folder = $"furniture/{folder}",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation()
                    .Quality("auto:good")
                    .FetchFormat("auto")
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            try
            {
                var uploadResult = await _cloudinary.UploadAsync(uploadParams, cts.Token);

                if (uploadResult?.Error != null)
                    throw new Exception($"Cloudinary error: {uploadResult.Error.Message}");

                return uploadResult!.SecureUrl.ToString();
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Upload timed out. Please try again.");
            }
        }

        private async Task<MemoryStream> CompressImageAsync(IFormFile file)
        {
            using var inputStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);

            // Resize if too large
            if (image.Width > 1200 || image.Height > 1200)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(1200, 1200),
                    Mode = ResizeMode.Max  
                }));
            }

            var outputStream = new MemoryStream();

            await image.SaveAsJpegAsync(outputStream, new JpegEncoder
            {
                Quality = 75 
            });

            outputStream.Position = 0;

            Console.WriteLine($"Compressed to: {outputStream.Length / 1024}KB");

            return outputStream;
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}
