using Microsoft.AspNetCore.Mvc;
using BE.models;
using Microsoft.EntityFrameworkCore;
using BE.services;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly CarMaintenanceDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly CloudinaryService _cloudinaryService;

        public ImageController(CarMaintenanceDbContext context, IWebHostEnvironment environment, CloudinaryService cloudinaryService)
        {
            _context = context;
            _environment = environment;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, long vehicleCheckinId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                // Tạo thư mục lưu ảnh
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "vehicle-checkins");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Tạo URL ảnh
                var imageUrl = $"/uploads/vehicle-checkins/{fileName}";

                // Lưu vào database
                var vehicleCheckinImage = new VehicleCheckinImage
                {
                    VehicleCheckinId = vehicleCheckinId,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VehicleCheckinImages.Add(vehicleCheckinImage);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = new { id = vehicleCheckinImage.Id, imageUrl = imageUrl } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Upload image to Cloudinary
        /// </summary>
        [HttpPost("upload-cloudinary")]
        public async Task<IActionResult> UploadImageToCloudinary(IFormFile file, long vehicleCheckinId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                // Upload to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(file, "vehicle-checkins");

                // Save to database
                var vehicleCheckinImage = new VehicleCheckinImage
                {
                    VehicleCheckinId = vehicleCheckinId,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VehicleCheckinImages.Add(vehicleCheckinImage);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = new { id = vehicleCheckinImage.Id, imageUrl = imageUrl } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Upload multiple images to Cloudinary for Vehicle Check-in
        /// </summary>
        [HttpPost("upload-multiple-cloudinary")]
        public async Task<IActionResult> UploadMultipleImagesToCloudinary(IFormFile[] files, long vehicleCheckinId)
        {
            try
            {
                Console.WriteLine($"Received {files?.Length ?? 0} files for vehicleCheckinId: {vehicleCheckinId}");
                
                if (files == null || files.Length == 0)
                {
                    Console.WriteLine("No files received");
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                var uploadedImages = new List<object>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Upload to Cloudinary
                        var imageUrl = await _cloudinaryService.UploadImageAsync(file, "vehicle-checkins");

                        // Save to database
                        var vehicleCheckinImage = new VehicleCheckinImage
                        {
                            VehicleCheckinId = vehicleCheckinId,
                            ImageUrl = imageUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.VehicleCheckinImages.Add(vehicleCheckinImage);
                        uploadedImages.Add(new { id = vehicleCheckinImage.Id, imageUrl = imageUrl });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = uploadedImages });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Upload image to Cloudinary without vehicleCheckinId (for temporary upload)
        /// </summary>
        [HttpPost("upload-temp")]
        public async Task<IActionResult> UploadImageTemp(IFormFile file, string folder = "temp")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                // Upload to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(file, folder);

                return Ok(new { success = true, data = new { url = imageUrl } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Upload user avatar to Cloudinary
        /// </summary>
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadUserAvatar(IFormFile file, long userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                // Upload to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(file, "user-avatars");

                // TODO: Save to user profile in database
                // var user = await _context.Users.FindAsync(userId);
                // user.AvatarUrl = imageUrl;
                // await _context.SaveChangesAsync();

                return Ok(new { success = true, data = new { imageUrl = imageUrl } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Upload component image to Cloudinary
        /// </summary>
        [HttpPost("upload-component-image")]
        public async Task<IActionResult> UploadComponentImage(IFormFile file, long componentId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                // Upload to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(file, "component-images");

                // TODO: Save to component in database
                // var component = await _context.Components.FindAsync(componentId);
                // component.ImageUrl = imageUrl;
                // await _context.SaveChangesAsync();

                return Ok(new { success = true, data = new { imageUrl = imageUrl } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Upload maintenance photo to Cloudinary
        /// </summary>
        [HttpPost("upload-maintenance-photo")]
        public async Task<IActionResult> UploadMaintenancePhoto(IFormFile file, long maintenanceId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                // Upload to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(file, "maintenance-photos");

                // TODO: Save to maintenance record in database
                // var maintenance = await _context.MaintenanceTickets.FindAsync(maintenanceId);
                // maintenance.PhotoUrl = imageUrl;
                // await _context.SaveChangesAsync();

                return Ok(new { success = true, data = new { imageUrl = imageUrl } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetImages(long id)
        {
            try
            {
                var images = await _context.VehicleCheckinImages
                    .Where(img => img.VehicleCheckinId == id)
                    .ToListAsync();

                return Ok(new { success = true, data = images });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
