namespace SmartBoardingHouse.Services
{
    public class PhotoService
    {
        private readonly string _uploadFolder;
        private readonly string _baseUrl;

        public PhotoService(IWebHostEnvironment env, IConfiguration config)
        {
            _baseUrl = config["AppSettings:BaseUrl"] ?? "https://localhost:7100";
            _uploadFolder = Path.Combine(env.ContentRootPath, "Images");

            if (!Directory.Exists(_uploadFolder))
                Directory.CreateDirectory(_uploadFolder);
        }

        public async Task<string> SavePhotoAsync(IFormFile photo)
        {
            var extension = Path.GetExtension(photo.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);

            return $"{_baseUrl}/images/{fileName}";
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