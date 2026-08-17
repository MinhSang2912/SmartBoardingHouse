using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Text.RegularExpressions;

namespace SmartBoardingHouse.Services
{
    public class PhotoService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _config;

        public PhotoService(IConfiguration config)
        {
            _config = config;

            var cloudName = config["Cloudinary:CloudName"];
            var apiKey = config["Cloudinary:ApiKey"];
            var apiSecret = config["Cloudinary:ApiSecret"];
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        // ==================== AVATAR ====================
        public async Task<string> SaveAvatarAsync(IFormFile photo, string userId)
        {
            if (photo == null || photo.Length == 0)
                throw new ArgumentException("Photo cannot be empty");

            var allowedFormats = new[] { "jpg", "jpeg", "png", "webp" };
            var extension = Path.GetExtension(photo.FileName).ToLower().TrimStart('.');

            if (!allowedFormats.Contains(extension))
                throw new ArgumentException("Invalid file format. Only jpg, jpeg, png, webp are allowed");

            if (photo.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 5MB limit");

            using var stream = photo.OpenReadStream();
            var publicId = $"avatar_{userId}_{DateTime.UtcNow.Ticks}";

            EnsureFolderExists("tenant-app/avatars");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(photo.FileName, stream),
                Folder = "tenant-app/avatars",
                PublicId = publicId,
                AllowedFormats = allowedFormats
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                return uploadResult.SecureUrl.ToString();

            throw new Exception($"Upload failed: {uploadResult.Error?.Message}");
        }

        // ==================== CCCD (ID CARD) ====================
        /// <summary>
        /// Lưu ảnh CCCD mặt trước
        /// </summary>
        public async Task<string> SaveFrontIdCardAsync(IFormFile photo, string userId)
        {
            return await SaveIdCardAsync(photo, userId, "front");
        }

        /// <summary>
        /// Lưu ảnh CCCD mặt sau
        /// </summary>
        public async Task<string> SaveBackIdCardAsync(IFormFile photo, string userId)
        {
            return await SaveIdCardAsync(photo, userId, "back");
        }

        /// <summary>
        /// Method dùng chung cho CCCD
        /// </summary>
        private async Task<string> SaveIdCardAsync(IFormFile photo, string userId, string side)
        {
            if (photo == null || photo.Length == 0)
                throw new ArgumentException("Photo cannot be empty");

            var allowedFormats = new[] { "jpg", "jpeg", "png", "webp" };
            var extension = Path.GetExtension(photo.FileName).ToLower().TrimStart('.');

            if (!allowedFormats.Contains(extension))
                throw new ArgumentException("Invalid file format. Only jpg, jpeg, png, webp are allowed");

            if (photo.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 10MB limit");

            using var stream = photo.OpenReadStream();
            var publicId = $"idcard_{side}_{userId}_{DateTime.UtcNow.Ticks}";

            var folder = $"tenant-app/id-cards/{side}";
            EnsureFolderExists(folder);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(photo.FileName, stream),
                Folder = folder,
                PublicId = publicId,
                AllowedFormats = allowedFormats
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                return uploadResult.SecureUrl.ToString();

            throw new Exception($"Upload failed: {uploadResult.Error?.Message}");
        }

        // ==================== METER ====================
        public async Task<string> SaveMeterPhotoAsync(IFormFile photo, string userId)
        {
            var allowedFormats = new[] { "jpg", "jpeg", "png", "webp" };
            var extension = Path.GetExtension(photo.FileName).ToLower().TrimStart('.');

            if (!allowedFormats.Contains(extension))
                throw new ArgumentException("File ảnh không hợp lệ. Chỉ chấp nhận định dạng jpg, jpeg, png, webp.");

            if (photo.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File ảnh không được vượt quá 10MB.");

            using var stream = photo.OpenReadStream();
            var publicId = $"meter_{userId}_{DateTime.UtcNow.Ticks}";

            EnsureFolderExists("tenant-app/meter-readings");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(photo.FileName, stream),
                Folder = "tenant-app/meter-readings",
                PublicId = publicId,
                AllowedFormats = allowedFormats
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                return uploadResult.SecureUrl.ToString();

            throw new Exception($"Upload failed: {uploadResult.Error?.Message}");
        }

        // ==================== MAINTENANCE / GENERIC ====================
        public async Task<string> SaveMaintenancePhotoAsync(IFormFile photo, string userId, string folder)
        {
            if (photo == null || photo.Length == 0)
                throw new ArgumentException("Photo cannot be empty");

            var allowedFormats = new[] { "jpg", "jpeg", "png", "webp" };
            var extension = Path.GetExtension(photo.FileName).ToLower().TrimStart('.');

            if (!allowedFormats.Contains(extension))
                throw new ArgumentException("Invalid file format. Only jpg, jpeg, png, webp are allowed");

            if (photo.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 10MB limit");

            using var stream = photo.OpenReadStream();
            var publicId = $"{folder}_{userId}_{DateTime.UtcNow.Ticks}";

            EnsureFolderExists($"tenant-app/{folder}");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(photo.FileName, stream),
                Folder = $"tenant-app/{folder}",
                PublicId = publicId,
                AllowedFormats = allowedFormats
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                return uploadResult.SecureUrl.ToString();

            throw new Exception($"Upload failed: {uploadResult.Error?.Message}");
        }

        // Backward compatibility
        public async Task<string> SavePhotoAsync(IFormFile photo)
        {
            return await SaveMeterPhotoAsync(photo, "default");
        }

        // ==================== DELETE ====================
        public async Task DeletePhotoAsync(string? photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl))
                return;

            try
            {
                var publicId = ExtractPublicIdFromUrl(photoUrl);
                if (!string.IsNullOrEmpty(publicId))
                {
                    var deleteParams = new DeletionParams(publicId);
                    var deleteResult = await _cloudinary.DestroyAsync(deleteParams);

                    if (deleteResult.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new Exception($"Delete failed: {deleteResult.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting photo: {ex.Message}");
            }
        }

        public void DeletePhoto(string? photoUrl)
        {
            DeletePhotoAsync(photoUrl).Wait();
        }

        // ==================== HELPERS ====================
        private string? ExtractPublicIdFromUrl(string url)
        {
            try
            {
                var match = Regex.Match(url, @"/upload/(?:v\d+/)?(.+)\.\w+");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            try
            {
                _cloudinary.CreateFolder(folderPath);
            }
            catch
            {
                // Ignore - Cloudinary sẽ tự tạo folder khi upload
            }
        }
    }
}