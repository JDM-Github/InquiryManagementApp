using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace InquiryManagementApp.Service
{
    public class FileUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(Cloudinary cloudinary, ILogger<FileUploadService> logger)
        {
            _cloudinary = cloudinary;
            _logger = logger;
        }

        // public async Task<string> UploadFileToCloudinaryAsync(IFormFile file)
        // {
        //     if (file == null || file.Length == 0)
        //     {
        //         return "";
        //     }

        //     try
        //     {
        //         using (var stream = file.OpenReadStream())
        //         {
        //             var uploadParams = new ImageUploadParams
        //             {
        //                 File = new FileDescription(file.FileName, stream),
        //                 Folder = "your-folder-name"
        //             };

        //             var uploadResult = await Task.Run(() => _cloudinary.Upload(uploadParams));

        //             if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
        //             {
        //                 _logger.LogInformation($"File uploaded successfully: {uploadResult.SecureUrl}");
        //                 return uploadResult.SecureUrl.ToString();
        //             }
        //             else
        //             {
        //                 _logger.LogError("Error uploading file: " + uploadResult.Error.Message);
        //                 return "";
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError("Exception during file upload: " + ex.Message);
        //         return "";
        //     }
        // }

        public async Task<string> UploadFileToCloudinaryAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return "";
            }

            try
            {
                string contentType = file.ContentType;

                bool isDocument = !contentType.StartsWith("image/");

                using (var stream = file.OpenReadStream())
                {
                    UploadResult uploadResult;

                    if (isDocument)
                    {
                        var uploadParams = new RawUploadParams
                        {
                            File = new FileDescription(file.FileName, stream),
                            Folder = "documents" 
                        };
                        uploadResult = await Task.Run(() => _cloudinary.Upload(uploadParams));
                    }
                    else
                    {
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(file.FileName, stream),
                            Folder = "images"
                        };
                        uploadResult = await Task.Run(() => _cloudinary.Upload(uploadParams));
                    }

                    if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogInformation($"File uploaded successfully: {uploadResult.SecureUrl}");
                        return uploadResult.SecureUrl.ToString();
                    }
                    else
                    {
                        _logger.LogError("Error uploading file: " + uploadResult.Error.Message);
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception during file upload: " + ex.Message);
                return "";
            }
        }

    }
}