# Cloudinary Integration Guide

## Overview
Your ASP.NET Core application has been updated to use Cloudinary for image storage instead of local file storage. All photo uploads now go directly to Cloudinary's CDN.

## Changes Made

### 1. **PhotoService.cs** - Complete Rewrite
- Replaced local file storage with Cloudinary integration
- Added three specialized upload methods:
  - `SaveAvatarAsync(IFormFile photo, string userId)` - For user avatars (300x300px, 5MB limit)
  - `SaveMeterPhotoAsync(IFormFile photo, string userId)` - For meter readings (10MB limit)
  - `SaveMaintenancePhotoAsync(IFormFile photo, string userId)` - For maintenance requests (10MB limit)
- Added `DeletePhotoAsync(string photoUrl)` method for removing photos from Cloudinary
- Maintained backward compatibility with `SavePhotoAsync()` and `DeletePhoto()` for sync operations

### 2. **appsettings.json** - Added Cloudinary Configuration
```json
"Cloudinary": {
  "CloudName": "dlftqagvo",
  "ApiKey": "858896998313189",
  "ApiSecret": "rf9Uy1vgyR3q6ncWVU3hy_Sp170"
}
```

### 3. **SmartBoardingHouse.csproj** - Added NuGet Package
- Added `CloudinaryDotNet` v1.26.1 package

### 4. **Controllers Updated**

#### ProfileController.cs
- `UploadAvatar` now uses `SaveAvatarAsync()` instead of generic `SavePhotoAsync()`
- Automatically deletes old avatar before uploading new one
- Added error handling for Cloudinary exceptions

#### MeterReadingController.cs
- `Create` method now uses `SaveMeterPhotoAsync()` for meter photos
- `Delete` method now properly deletes photos from Cloudinary using `DeletePhotoAsync()`
- Added try-catch for upload errors

## Cloudinary Folder Structure

Your images are organized by type:
- **Avatars**: `tenant-app/avatars/avatar_{userId}_{timestamp}`
- **Meter Readings**: `tenant-app/meter-readings/meter_{userId}_{timestamp}`
- **Maintenance**: `tenant-app/maintenance/maintenance_{userId}_{timestamp}`

## Supported File Formats
- JPG, JPEG, PNG, WebP

## File Size Limits
- Avatar: 5 MB
- Meter Reading: 10 MB
- Maintenance: 10 MB

## Image Transformations

### Avatars
- Width: 300px
- Height: 300px
- Crop: Fill (automatic square crop)

## Key Benefits
✅ No local storage needed on server
✅ Images served from Cloudinary CDN (faster delivery)
✅ Automatic image optimization
✅ Easy backup and recovery
✅ Reduced server storage requirements

## Next Steps

1. **Build the Project**
   ```bash
   dotnet build
   ```

2. **Run the Application**
   ```bash
   dotnet run
   ```

3. **Test Image Upload**
   - Upload an avatar via `/api/profile/avatar`
   - Upload a meter reading photo via `/api/meterreadings` POST endpoint
   - Verify images appear in Cloudinary Dashboard

## Troubleshooting

### Build Errors
If you get build errors about CloudinaryDotNet:
```bash
dotnet restore
```

### Upload Fails
- Check that Cloudinary credentials are correct in `appsettings.json`
- Verify file size is within limits
- Ensure file format is supported (jpg, jpeg, png, webp)

### Images Not Displaying
- Check that Cloudinary account is active
- Verify image URLs are using HTTPS (secure URLs)
- Check browser console for CORS issues

## Rollback (if needed)
If you need to revert to local storage:
1. Restore the original PhotoService.cs from version control
2. Remove CloudinaryDotNet from .csproj
3. Remove Cloudinary configuration from appsettings.json
4. Update controllers to use `SavePhotoAsync()` without userId parameter

## Additional Features to Implement

### For MaintenanceRequestController
Add photo upload support when creating maintenance requests:
```csharp
if (request.Photo is not null)
{
    photoUrl = await _photoService.SaveMaintenancePhotoAsync(request.Photo, userId);
}
```

### Image Versioning
Cloudinary automatically versions images via the `/v{timestamp}/` URL segment, so deleting and re-uploading with the same public_id will automatically version the image.

### Image Optimization
Cloudinary automatically optimizes images for:
- Different screen sizes
- Mobile/desktop
- Different browsers (WebP for Chrome, etc.)

## API Response Example

When uploading an avatar:
```json
{
  "avatarUrl": "https://res.cloudinary.com/dlftqagvo/image/upload/v1234567890/tenant-app/avatars/avatar_user123_1234567890123.jpg",
  "message": "Cập nhật avatar thành công"
}
```

## Security Notes
⚠️ **Important**: In production:
1. Move Cloudinary credentials to User Secrets or Azure Key Vault
2. Don't commit credentials to source control
3. Use environment-specific configuration

Example with User Secrets:
```bash
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
```

---

For more information, visit: https://cloudinary.com/documentation/dotnet_integration
