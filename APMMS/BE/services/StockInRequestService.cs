using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BE.interfaces;
using BE.models;
using BE.DTOs.StockInRequest;
using BE.repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace BE.services
{
    public class StockInRequestService : IStockInRequestService
    {
        private readonly IStockInRequestRepository _repo;
        private readonly IComponentRepository _componentRepo;
        private readonly IMapper _mapper;
        private readonly CarMaintenanceDbContext _context;

        public StockInRequestService(
            IStockInRequestRepository repo,
            IComponentRepository componentRepo,
            IMapper mapper,
            CarMaintenanceDbContext context)
        {
            _repo = repo;
            _componentRepo = componentRepo;
            _mapper = mapper;
            _context = context;
        }

        public async Task<StockInRequestUploadResponseDto> UploadExcelAsync(IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet == null)
                throw new ArgumentException("File Excel không có dữ liệu");

            var result = new StockInRequestUploadResponseDto();
            var details = new List<StockInRequestDetailUploadDto>();

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            int colCount = worksheet.Dimension?.Columns ?? 0;

            // Tìm dòng header (có chứa "Mã linh kiện")
            int headerRow = 1;
            int dataStartRow = 2;
            
            for (int r = 1; r <= Math.Min(5, rowCount); r++)
            {
                var cellValue = worksheet.Cells[r, 1]?.Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(cellValue) && 
                    (cellValue.Contains("Mã linh kiện") || cellValue.Contains("STT")))
                {
                    headerRow = r;
                    dataStartRow = r + 1;
                    break;
                }
            }

            if (rowCount < dataStartRow)
                throw new ArgumentException("File Excel không có dữ liệu linh kiện");

            var errors = new List<string>();
            int validRowCount = 0;
            int emptyRowCount = 0;

            // Đọc tất cả component codes trước để query batch
            var componentCodes = new List<string>();
            for (int row = dataStartRow; row <= rowCount; row++)
            {
                var componentCode = worksheet.Cells[row, 1]?.Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(componentCode))
                    componentCodes.Add(componentCode);
            }

            // Query batch tất cả components
            var allComponents = await _context.Components
                .Where(c => componentCodes.Contains(c.Code) && c.StatusCode != "DELETED")
                .Include(c => c.TypeComponent)
                .ToListAsync();
            
            var componentDict = allComponents.ToDictionary(c => c.Code, c => c);

            for (int row = dataStartRow; row <= rowCount; row++)
            {
                var componentCode = worksheet.Cells[row, 1]?.Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(componentCode))
                {
                    emptyRowCount++;
                    continue;
                }

                // Đọc thông tin từ Excel
                var componentNameFromExcel = worksheet.Cells[row, 2]?.Value?.ToString()?.Trim();
                var typeFromExcel = worksheet.Cells[row, 3]?.Value?.ToString()?.Trim();

                // Tìm component theo code
                if (!componentDict.TryGetValue(componentCode, out var component))
                {
                    errors.Add($"Dòng {row}: Linh kiện '{componentCode}' không tồn tại trong hệ thống");
                    continue;
                }

                var quantity = 0;
                if (int.TryParse(worksheet.Cells[row, 4]?.Value?.ToString(), out var qty))
                    quantity = qty;
                else
                {
                    errors.Add($"Dòng {row}: Số lượng không hợp lệ");
                    continue;
                }

                if (quantity <= 0)
                {
                    errors.Add($"Dòng {row}: Số lượng phải lớn hơn 0");
                    continue;
                }

                details.Add(new StockInRequestDetailUploadDto
                {
                    ComponentId = component.Id,
                    ComponentCode = componentCode,
                    ComponentName = component.Name ?? componentNameFromExcel ?? "",
                    TypeComponentName = component.TypeComponent?.Name ?? typeFromExcel ?? "",
                    Quantity = quantity
                });
                validRowCount++;
            }

            if (details.Count == 0)
            {
                var errorMsg = $"Không tìm thấy linh kiện hợp lệ trong file Excel. {errors.Count} lỗi:\n{string.Join("\n", errors.Take(10))}";
                throw new ArgumentException(errorMsg);
            }


            result.Details = details;
            return result;
        }

        public async Task<StockInRequestResponseDto> CreateAsync(StockInRequestRequestDto dto, long userId)
        {
            // Validate userId
            if (userId <= 0)
            {
                throw new ArgumentException("UserId không hợp lệ. Vui lòng đăng nhập lại.");
            }

            // Validation
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Yêu cầu nhập kho phải có ít nhất một linh kiện");

            // Luôn tự động tạo mã (không cho phép người dùng nhập)
            dto.Code = await GenerateUniqueCodeAsync();

            // Validate BranchId
            if (dto.BranchId <= 0)
            {
                throw new ArgumentException("BranchId không hợp lệ. Vui lòng liên hệ quản trị viên.");
            }

            // Create entity - Status mặc định là CREATED (chưa gửi)
            var entity = new StockInRequest
            {
                Code = dto.Code,
                BranchId = dto.BranchId,
                Description = dto.Description,
                Note = dto.Note,
                StatusCode = dto.StatusCode ?? "CREATED", // Mặc định là CREATED, phải gửi đơn mới thành PENDING
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            // Add details
            entity.StockInRequestDetails = new List<StockInRequestDetail>();
            foreach (var detailDto in dto.Details)
            {
                // Validate component exists
                var component = await _componentRepo.GetByIdAsync(detailDto.ComponentId);
                if (component == null)
                    throw new ArgumentException($"Linh kiện với ID {detailDto.ComponentId} không tồn tại");

                // Validate component belongs to same branch
                if (!component.BranchId.HasValue)
                    throw new ArgumentException($"Linh kiện {component.Name} chưa được gán cho chi nhánh nào");

                if (component.BranchId.Value != dto.BranchId)
                {
                    var componentBranch = component.Branch?.Name ?? "N/A";
                    throw new ArgumentException($"Linh kiện {component.Name} (Mã: {component.Code ?? "N/A"}) không thuộc chi nhánh này. Linh kiện thuộc chi nhánh: {componentBranch}.");
                }

                entity.StockInRequestDetails.Add(new StockInRequestDetail
                {
                    StockInRequestId = entity.Id, // Will be set after save
                    ComponentId = detailDto.ComponentId,
                    Quantity = detailDto.Quantity
                });
            }

            var created = await _repo.AddAsync(entity);
            return await MapToResponseAsync(created);
        }

        public async Task<StockInRequestResponseDto> UpdateAsync(StockInRequestRequestDto dto, long userId)
        {
            if (!dto.StockInRequestId.HasValue)
                throw new ArgumentException("ID yêu cầu nhập kho là bắt buộc");

            var entity = await _repo.GetByIdAsync(dto.StockInRequestId.Value);
            if (entity == null)
                throw new ArgumentException("Yêu cầu nhập kho không tồn tại");

            // Only allow update if status is PENDING, CREATED, or CANCELLED
            if (entity.StatusCode != "PENDING" && entity.StatusCode != "CREATED" && entity.StatusCode != "CANCELLED")
                throw new ArgumentException("Không thể cập nhật yêu cầu nhập kho ở trạng thái này");

            // Update basic info
            entity.Description = dto.Description;
            entity.Note = dto.Note;
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.Now;

            // Update code if changed
            if (!string.IsNullOrEmpty(dto.Code) && dto.Code != entity.Code)
            {
                if (await _repo.CodeExistsAsync(dto.Code, dto.StockInRequestId))
                    throw new ArgumentException($"Mã yêu cầu nhập kho '{dto.Code}' đã tồn tại");
                entity.Code = dto.Code;
            }

            // Update details
            if (dto.Details != null && dto.Details.Any())
            {
                // Remove all existing details
                _context.StockInRequestDetails.RemoveRange(entity.StockInRequestDetails);

                // Add new details
                entity.StockInRequestDetails = new List<StockInRequestDetail>();
                foreach (var detailDto in dto.Details)
                {
                    if (detailDto.Quantity <= 0)
                        continue; // Skip if quantity is 0 (means delete)

                    var component = await _componentRepo.GetByIdAsync(detailDto.ComponentId);
                    if (component == null)
                        throw new ArgumentException($"Linh kiện với ID {detailDto.ComponentId} không tồn tại");

                    if (!component.BranchId.HasValue)
                        throw new ArgumentException($"Linh kiện {component.Name} chưa được gán cho chi nhánh nào");

                    if (component.BranchId.Value != entity.BranchId)
                    {
                        var componentBranch = component.Branch?.Name ?? "N/A";
                        throw new ArgumentException($"Linh kiện {component.Name} (Mã: {component.Code ?? "N/A"}) không thuộc chi nhánh này. Linh kiện thuộc chi nhánh: {componentBranch}.");
                    }

                    entity.StockInRequestDetails.Add(new StockInRequestDetail
                    {
                        StockInRequestId = entity.Id,
                        ComponentId = detailDto.ComponentId,
                        Quantity = detailDto.Quantity
                    });
                }

                if (!entity.StockInRequestDetails.Any())
                    throw new ArgumentException("Yêu cầu nhập kho phải có ít nhất một linh kiện");
            }

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<StockInRequestResponseDto> GetByIdAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Yêu cầu nhập kho không tồn tại");
            return await MapToResponseAsync(entity);
        }

        public async Task<IEnumerable<StockInRequestResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var list = await _repo.GetAllAsync(page, pageSize, branchId, statusCode, search);
            var result = new List<StockInRequestResponseDto>();
            foreach (var entity in list)
            {
                result.Add(await MapToResponseAsync(entity));
            }
            return result;
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null)
        {
            return await _repo.GetTotalCountAsync(branchId, statusCode, search);
        }

        public async Task<IEnumerable<StockInRequestResponseDto>> GetByStatusAsync(string statusCode)
        {
            var list = await _repo.GetByStatusAsync(statusCode);
            var result = new List<StockInRequestResponseDto>();
            foreach (var entity in list)
            {
                result.Add(await MapToResponseAsync(entity));
            }
            return result;
        }

        public async Task<StockInRequestResponseDto> ChangeStatusAsync(long id, string statusCode, long userId)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Yêu cầu nhập kho không tồn tại");

            // Validate status transition
            if (statusCode == "PENDING")
            {
                if (entity.StatusCode != "CREATED" && entity.StatusCode != "CANCELLED")
                    throw new ArgumentException("Chỉ có thể gửi yêu cầu từ trạng thái CREATED hoặc CANCELLED");
            }

            entity.StatusCode = statusCode;
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.Now;

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<StockInRequestResponseDto> ApproveAsync(long id, long userId)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Yêu cầu nhập kho không tồn tại");

            if (entity.StatusCode != "PENDING")
                throw new ArgumentException("Chỉ có thể duyệt yêu cầu ở trạng thái PENDING");

            entity.StatusCode = "APPROVED";
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.Now;

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<StockInRequestResponseDto> CancelAsync(long id, string? note, long userId)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Yêu cầu nhập kho không tồn tại");

            if (entity.StatusCode == "APPROVED")
                throw new ArgumentException("Không thể hủy yêu cầu đã được duyệt");

            entity.StatusCode = "CANCELLED";
            if (!string.IsNullOrEmpty(note))
                entity.Note = note;
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.Now;

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _repo.ExistsAsync(id);
        }

        private async Task<StockInRequestResponseDto> MapToResponseAsync(StockInRequest entity)
        {
            var dto = _mapper.Map<StockInRequestResponseDto>(entity);
            dto.BranchName = entity.Branch?.Name;
            dto.StatusName = entity.StatusCodeNavigation?.Name;
            dto.CreatedByName = entity.CreatedByNavigation != null 
                ? $"{entity.CreatedByNavigation.FirstName} {entity.CreatedByNavigation.LastName}".Trim()
                : null;
            dto.LastModifiedByName = entity.LastModifiedByNavigation != null
                ? $"{entity.LastModifiedByNavigation.FirstName} {entity.LastModifiedByNavigation.LastName}".Trim()
                : null;

            // Map details
            if (entity.StockInRequestDetails != null && entity.StockInRequestDetails.Any())
            {
                dto.Details = entity.StockInRequestDetails.Select(d => new StockInRequestDetailResponseDto
                {
                    StockInRequestId = d.StockInRequestId,
                    ComponentId = d.ComponentId,
                    ComponentCode = d.Component?.Code ?? "",
                    ComponentName = d.Component?.Name ?? "",
                    ImageUrl = d.Component?.ImageUrl,
                    Quantity = d.Quantity,
                    ImportPricePerUnit = d.Component?.PurchasePrice,
                    ExportPricePerUnit = d.Component?.UnitPrice,
                    MinQuantity = d.Component?.MinimumQuantity
                }).ToList();
            }

            return dto;
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            // Format: {prefix}{Date}{counter} = 12 ký tự
            // Prefix: "YC" (2 ký tự) + Date: yyMMdd (6 ký tự) + Counter: D4 (4 ký tự) = 12 ký tự
            string prefix = "YC";
            string code;
            int counter = 1;
            do
            {
                code = $"{prefix}{DateTime.Now:yyMMdd}{counter:D4}";
                counter++;
            } while (await _repo.CodeExistsAsync(code) && counter < 10000);

            if (counter >= 10000)
                throw new Exception("Không thể tạo mã yêu cầu nhập kho duy nhất");

            return code;
        }
    }
}

