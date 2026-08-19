// Services/PhotoService.cs
namespace SmartBoardingHouse.Services
{
    public class PhotoService
    {
        private readonly string _uploadFolder;
        private readonly string _baseUrl;

        public PhotoService(IWebHostEnvironment env, IConfiguration config)
        {
            // Lưu vào wwwroot/uploads/meter-readings/
            _uploadFolder = Path.Combine(env.WebRootPath, "uploads", "meter-readings");
            _baseUrl = config["AppSettings:BaseUrl"] ?? "https://localhost:7100";

            // Tạo thư mục nếu chưa có
            if (!Directory.Exists(_uploadFolder))
                Directory.CreateDirectory(_uploadFolder);
        }

        public async Task<string> SavePhotoAsync(IFormFile photo)
        {
            // Tạo tên file unique: roomnumber_timestamp.jpg
            var extension = Path.GetExtension(photo.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // Trả về URL có thể truy cập từ client
            return $"{_baseUrl}/uploads/meter-readings/{fileName}";
        }

        public void DeletePhoto(string? photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl)) return;

            var fileName = Path.GetFileName(photoUrl);
            var filePath = Path.Combine(_uploadFolder, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}