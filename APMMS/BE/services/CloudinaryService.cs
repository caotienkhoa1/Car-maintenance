using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace BE.services
{
    /// <summary>
    /// Service for handling Cloudinary operations
    /// </summary>
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        /// <summary>
        /// Upload image to Cloudinary
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="folder">Folder path in Cloudinary (e.g., "vehicle-checkins", "user-avatars", "component-images")</param>
        /// <param name="isAvatar">If true, will crop image to square format (suitable for avatars)</param>
        /// <returns>Public URL of the uploaded image</returns>
        public async Task<string> UploadImageAsync(IFormFile file, string folder = "vehicle-checkins", bool isAvatar = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is empty or null");
                }

                // Generate unique public ID
                var publicId = $"{folder}/{Guid.NewGuid()}";

                // Upload parameters
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    PublicId = publicId,
                    Folder = folder,
                    Overwrite = false
                };

                // Apply transformation based on type
                if (isAvatar)
                {
                    // For avatars: crop to square, max 400x400
                    uploadParams.Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                        .Width(400)
                        .Height(400)
                        .Crop("fill")
                        .Gravity("face"); // Focus on faces if detected
                }
                else
                {
                    // For other images: limit size to 800x600
                    uploadParams.Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                        .Width(800)
                        .Height(600)
                        .Crop("limit");
                }

                // Upload to Cloudinary
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return uploadResult.SecureUrl.ToString();
                }
                else
                {
                    throw new Exception($"Upload failed: {uploadResult.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload image to Cloudinary: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Upload multiple images to Cloudinary
        /// </summary>
        /// <param name="files">List of image files to upload</param>
        /// <param name="folder">Folder path in Cloudinary</param>
        /// <returns>List of public URLs of uploaded images</returns>
        public async Task<List<string>> UploadImagesAsync(IFormFile[] files, string folder = "vehicle-checkins")
        {
            var uploadedUrls = new List<string>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var url = await UploadImageAsync(file, folder);
                    uploadedUrls.Add(url);
                }
            }

            return uploadedUrls;
        }

        /// <summary>
        /// Delete image from Cloudinary
        /// </summary>
        /// <param name="imageUrl">The public URL of the image to delete</param>
        public async Task DeleteImageAsync(string imageUrl)
        {
            try
            {
                // Extract public ID from URL
                var uri = new Uri(imageUrl);
                var publicId = Path.GetFileNameWithoutExtension(uri.AbsolutePath);

                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                if (result.Result != "ok")
                {
                    throw new Exception($"Delete failed: {result.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete image from Cloudinary: {ex.Message}", ex);
            }
        }
    }
}
