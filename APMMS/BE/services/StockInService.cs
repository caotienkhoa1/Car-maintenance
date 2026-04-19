using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BE.interfaces;
using BE.models;
using BE.DTOs.StockIn;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace BE.services
{
    public class StockInService : IStockInService
    {
        private readonly IStockInRepository _repo;
        private readonly IStockInRequestRepository _requestRepo;
        private readonly IComponentRepository _componentRepo;
        private readonly IMapper _mapper;
        private readonly CarMaintenanceDbContext _context;

        public StockInService(
            IStockInRepository repo,
            IStockInRequestRepository requestRepo,
            IComponentRepository componentRepo,
            IMapper mapper,
            CarMaintenanceDbContext context)
        {
            _repo = repo;
            _requestRepo = requestRepo;
            _componentRepo = componentRepo;
            _mapper = mapper;
            _context = context;
        }

        public async Task<StockInResponseDto> CreateFromRequestAsync(StockInRequestDto dto, long userId)
        {
            // Validate stock in request code (giống VWMS - bắt buộc phải có mã yêu cầu)
            if (string.IsNullOrEmpty(dto.StockInRequestCode) && !dto.StockInRequestId.HasValue)
                throw new ArgumentException("Mã yêu cầu nhập kho là bắt buộc");

            // Tìm yêu cầu nhập kho với status APPROVED (giống VWMS - getImportRequestByCodeAndStatus)
            StockInRequest? request = null;
            if (dto.StockInRequestId.HasValue)
            {
                request = await _requestRepo.GetByIdAsync(dto.StockInRequestId.Value);
                // Kiểm tra status sau khi tìm theo ID
                if (request != null && request.StatusCode != "APPROVED")
                    request = null; // Không hợp lệ nếu không phải APPROVED
            }
            else if (!string.IsNullOrEmpty(dto.StockInRequestCode))
            {
                // Tìm trực tiếp với status APPROVED (giống VWMS)
                request = await _requestRepo.GetByCodeAndStatusAsync(dto.StockInRequestCode, "APPROVED");
            }

            // Validate yêu cầu nhập kho phải tồn tại và đã được duyệt (giống VWMS)
            if (request == null)
            {
                var codeToCheck = dto.StockInRequestCode ?? "N/A";
                throw new ArgumentException($"Yêu cầu nhập kho '{codeToCheck}' không tồn tại hoặc chưa được duyệt (APPROVED). Vui lòng kiểm tra lại mã yêu cầu trong file Excel.");
            }

            // Check if stock in already exists for this request (giống VWMS - kiểm tra đã có phiếu nhập kho chưa)
            if (await _repo.ExistsByStockInRequestIdAsync(request.Id))
                throw new ArgumentException("Phiếu nhập kho đã tồn tại cho yêu cầu này");

            // Validate details
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phiếu nhập kho phải có ít nhất một linh kiện");

            // Validate status code nếu có, nếu không thì để null (sẽ dùng default từ database)
            // Giống VWMS: Khi tạo phiếu nhập kho, status là CREATED (chưa gửi)
            string statusCode = dto.StatusCode ?? "CREATED";
            
            // Kiểm tra status code có tồn tại trong database không
            var statusExists = await _context.StatusLookups.AnyAsync(s => s.Code == statusCode);
            if (!statusExists)
            {
                // Thử dùng PENDING nếu status code không hợp lệ
                statusCode = "PENDING";
                var pendingExists = await _context.StatusLookups.AnyAsync(s => s.Code == statusCode);
                if (!pendingExists)
                {
                    // Nếu PENDING cũng không có, lấy status code đầu tiên từ database
                    var firstStatus = await _context.StatusLookups.FirstOrDefaultAsync();
                    if (firstStatus != null)
                    {
                        statusCode = firstStatus.Code;
                    }
                    else
                    {
                        throw new ArgumentException("Không tìm thấy status code hợp lệ trong hệ thống");
                    }
                }
            }

            // Create stock in entity
            var entity = new StockIn
            {
                StockInRequestId = request.Id,
                StatusCode = statusCode,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Create details with snapshot
            entity.StockInDetails = new List<StockInDetail>();
            foreach (var detailDto in dto.Details)
            {
                var component = await _componentRepo.GetByIdAsync(detailDto.ComponentId ?? 0);
                if (component == null && !string.IsNullOrEmpty(detailDto.ComponentCode))
                {
                    // Try to find by code
                    var components = await _componentRepo.GetAllAsync(1, 1000);
                    component = components.FirstOrDefault(c => c.Code == detailDto.ComponentCode);
                }

                var detail = new StockInDetail
                {
                    StockInId = entity.Id, // Will be set after save
                    ComponentId = component?.Id ?? detailDto.ComponentId ?? 0,
                    Quantity = detailDto.Quantity,
                    QuantityAfterCheck = detailDto.QuantityAfterCheck ?? detailDto.Quantity,
                    ImportPricePerUnit = detailDto.ImportPricePerUnit,
                    ExportPricePerUnit = detailDto.ExportPricePerUnit,
                    Vat = detailDto.Vat,
                    ComponentCode = detailDto.ComponentCode ?? component?.Code ?? "",
                    ComponentName = detailDto.ComponentName,
                    TypeComponent = detailDto.TypeComponent ?? component?.TypeComponent?.Name,
                    MinQuantity = detailDto.MinQuantity ?? component?.MinimumQuantity,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                entity.StockInDetails.Add(detail);
            }

            var created = await _repo.AddAsync(entity);
            return await MapToResponseAsync(created);
        }

        public async Task<StockInResponseDto> UpdateAsync(StockInRequestDto dto, long userId)
        {
            if (!dto.Id.HasValue)
                throw new ArgumentException("ID phiếu nhập kho là bắt buộc");

            var entity = await _repo.GetByIdAsync(dto.Id.Value);
            if (entity == null)
                throw new ArgumentException("Phiếu nhập kho không tồn tại");

            // Only allow update if status is CREATED or CANCELLED
            if (entity.StatusCode != "CREATED" && entity.StatusCode != "CANCELLED")
                throw new ArgumentException("Không thể cập nhật phiếu nhập kho ở trạng thái này");

            entity.StatusCode = dto.StatusCode ?? entity.StatusCode;
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.UtcNow;

            // Update details (quantity after check, prices)
            if (dto.Details != null && dto.Details.Any())
            {
                foreach (var detailDto in dto.Details)
                {
                    var detail = entity.StockInDetails?.FirstOrDefault(d => 
                        d.ComponentCode == detailDto.ComponentCode || 
                        d.ComponentId == (detailDto.ComponentId ?? 0));
                    
                    if (detail != null)
                    {
                        if (detailDto.QuantityAfterCheck.HasValue)
                            detail.QuantityAfterCheck = detailDto.QuantityAfterCheck.Value;
                        if (detailDto.ImportPricePerUnit.HasValue)
                            detail.ImportPricePerUnit = detailDto.ImportPricePerUnit;
                        if (detailDto.ExportPricePerUnit.HasValue)
                            detail.ExportPricePerUnit = detailDto.ExportPricePerUnit;
                        if (detailDto.Vat.HasValue)
                            detail.Vat = detailDto.Vat;
                        detail.LastModifiedBy = userId;
                        detail.LastModifiedDate = DateTime.UtcNow;
                    }
                }
            }

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<StockInResponseDto> GetByIdAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Phiếu nhập kho không tồn tại");
            return await MapToResponseAsync(entity);
        }

        public async Task<IEnumerable<StockInResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var list = await _repo.GetAllAsync(page, pageSize, branchId, statusCode, search);
            var result = new List<StockInResponseDto>();
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

        public async Task<IEnumerable<StockInResponseDto>> GetByStatusAsync(string statusCode)
        {
            var list = await _repo.GetByStatusAsync(statusCode);
            var result = new List<StockInResponseDto>();
            foreach (var entity in list)
            {
                result.Add(await MapToResponseAsync(entity));
            }
            return result;
        }

        public async Task<StockInResponseDto> ChangeStatusAsync(long id, string statusCode, long userId)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Phiếu nhập kho không tồn tại");

            // Validate status transition
            if (statusCode == "PENDING")
            {
                if (entity.StatusCode != "CREATED" && entity.StatusCode != "CANCELLED")
                    throw new ArgumentException("Chỉ có thể gửi phiếu từ trạng thái CREATED hoặc CANCELLED");
            }

            entity.StatusCode = statusCode;
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<StockInResponseDto> ApproveAsync(StockInRequestDto dto, long userId)
        {
            if (!dto.Id.HasValue)
                throw new ArgumentException("ID phiếu nhập kho là bắt buộc");

            var entity = await _repo.GetByIdAsync(dto.Id.Value);
            if (entity == null)
                throw new ArgumentException("Phiếu nhập kho không tồn tại");

            // Cho phép duyệt khi status là CREATED hoặc PENDING
            if (entity.StatusCode != "PENDING" && entity.StatusCode != "CREATED")
                throw new ArgumentException("Chỉ có thể duyệt phiếu ở trạng thái CREATED hoặc PENDING");

            // Validate prices and quantities
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phiếu nhập kho phải có ít nhất một linh kiện");

            foreach (var detailDto in dto.Details)
            {
                if (!detailDto.QuantityAfterCheck.HasValue || detailDto.QuantityAfterCheck.Value <= 0)
                    throw new ArgumentException("Số lượng sau kiểm tra phải lớn hơn 0");

                // Lấy giá từ DTO hoặc từ detail hiện tại (từ Excel)
                var detail = entity.StockInDetails?.FirstOrDefault(d => 
                    d.ComponentCode == detailDto.ComponentCode || 
                    d.ComponentId == (detailDto.ComponentId ?? 0));
                
                var importPrice = detailDto.ImportPricePerUnit ?? detail?.ImportPricePerUnit;
                var exportPrice = detailDto.ExportPricePerUnit ?? detail?.ExportPricePerUnit;

                if (!importPrice.HasValue || importPrice.Value <= 0)
                    throw new ArgumentException($"Giá nhập phải lớn hơn 0 cho linh kiện {detailDto.ComponentCode ?? detailDto.ComponentId?.ToString() ?? "N/A"}");

                if (!exportPrice.HasValue || exportPrice.Value <= 0)
                    throw new ArgumentException($"Giá xuất phải lớn hơn 0 cho linh kiện {detailDto.ComponentCode ?? detailDto.ComponentId?.ToString() ?? "N/A"}");

                if (detailDto.Vat.HasValue && detailDto.Vat.Value < 0)
                    throw new ArgumentException("VAT phải >= 0");
            }

            // Update details with prices and quantities
            foreach (var detailDto in dto.Details)
            {
                var detail = entity.StockInDetails?.FirstOrDefault(d => 
                    d.ComponentCode == detailDto.ComponentCode || 
                    d.ComponentId == (detailDto.ComponentId ?? 0));
                
                if (detail != null)
                {
                    detail.QuantityAfterCheck = detailDto.QuantityAfterCheck.Value;
                    // Cập nhật giá từ DTO nếu có, nếu không thì giữ giá hiện tại từ Excel
                    // Ưu tiên giá từ DTO (từ form approve), nếu không có thì dùng giá từ Excel (đã lưu trong detail)
                    detail.ImportPricePerUnit = detailDto.ImportPricePerUnit ?? detail.ImportPricePerUnit;
                    detail.ExportPricePerUnit = detailDto.ExportPricePerUnit ?? detail.ExportPricePerUnit;
                    detail.Vat = detailDto.Vat ?? detail.Vat ?? 0;
                    detail.LastModifiedBy = userId;
                    detail.LastModifiedDate = DateTime.UtcNow;
                }
            }

            // Update stock in component quantities and prices (giống VWMS)
            // Lấy giá từ detail đã được cập nhật ở trên
            foreach (var detail in entity.StockInDetails)
            {
                if (detail.ComponentId > 0)
                {
                    var component = await _componentRepo.GetByIdAsync(detail.ComponentId);
                    if (component != null)
                    {
                        // Update component stock (giống VWMS: component.setQuantity(component.getQuantity() + requestDto.getQuantityAfterCheck()))
                        component.QuantityStock = (component.QuantityStock ?? 0) + (detail.QuantityAfterCheck ?? 0);
                        
                        // Update prices (giống VWMS - luôn cập nhật giá khi approve, không có điều kiện)
                        // VWMS: component.setExportPricePerUnit(requestDto.getPriceExportPerUnit());
                        // VWMS: component.setImportPricePerUnit(requestDto.getPriceImportPerUnit());
                        if (detail.ImportPricePerUnit.HasValue)
                            component.PurchasePrice = detail.ImportPricePerUnit.Value;
                        if (detail.ExportPricePerUnit.HasValue)
                            component.UnitPrice = detail.ExportPricePerUnit.Value;
                        // VAT chỉ lưu trong StockInDetail, không lưu trong Component

                        await _componentRepo.UpdateAsync(component);
                    }
                }
                else if (!string.IsNullOrEmpty(detail.ComponentCode))
                {
                    // Component không tồn tại, tìm theo code để cập nhật giá nếu tìm thấy
                    var component = await _componentRepo.GetByCodeAsync(detail.ComponentCode);
                    if (component != null)
                    {
                        // Update component stock
                        component.QuantityStock = (component.QuantityStock ?? 0) + (detail.QuantityAfterCheck ?? 0);
                        
                        // Update prices (luôn cập nhật nếu có giá)
                        if (detail.ImportPricePerUnit.HasValue)
                            component.PurchasePrice = detail.ImportPricePerUnit.Value;
                        if (detail.ExportPricePerUnit.HasValue)
                            component.UnitPrice = detail.ExportPricePerUnit.Value;

                        await _componentRepo.UpdateAsync(component);
                    }
                }
            }

            // Update stock in status
            entity.StatusCode = "APPROVED";
            entity.ApprovedBy = userId;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<StockInResponseDto> UpdateQuantityAfterCheckAsync(StockInRequestDto dto, long userId)
        {
            if (!dto.Id.HasValue)
                throw new ArgumentException("ID phiếu nhập kho là bắt buộc");

            var entity = await _repo.GetByIdAsync(dto.Id.Value);
            if (entity == null)
                throw new ArgumentException("Phiếu nhập kho không tồn tại");

            // Only allow update if status is CREATED or CANCELLED
            if (entity.StatusCode != "CREATED" && entity.StatusCode != "CANCELLED")
                throw new ArgumentException("Không thể cập nhật số lượng sau kiểm tra ở trạng thái này");

            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phải có ít nhất một linh kiện");

            // Update quantity after check
            foreach (var detailDto in dto.Details)
            {
                var detail = entity.StockInDetails?.FirstOrDefault(d => 
                    d.ComponentCode == detailDto.ComponentCode || 
                    d.ComponentId == (detailDto.ComponentId ?? 0));
                
                if (detail != null && detailDto.QuantityAfterCheck.HasValue)
                {
                    detail.QuantityAfterCheck = detailDto.QuantityAfterCheck.Value;
                    detail.LastModifiedBy = userId;
                    detail.LastModifiedDate = DateTime.UtcNow;
                }
            }

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<StockInResponseDto> CancelAsync(long id, long userId)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Phiếu nhập kho không tồn tại");

            if (entity.StatusCode == "APPROVED")
                throw new ArgumentException("Không thể hủy phiếu đã được duyệt");

            entity.StatusCode = "CANCELLED";
            entity.LastModifiedBy = userId;
            entity.LastModifiedDate = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(entity);
            return await MapToResponseAsync(updated);
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _repo.ExistsAsync(id);
        }

        private async Task<StockInResponseDto> MapToResponseAsync(StockIn entity)
        {
            var dto = _mapper.Map<StockInResponseDto>(entity);
            // Tạo mã phiếu nhập kho tự động: PNK + Id (ví dụ: PNK1, PNK2, ...)
            dto.Code = $"PNK{entity.Id}";
            dto.StockInRequestCode = entity.StockInRequest?.Code ?? "";
            dto.BranchName = entity.StockInRequest?.Branch?.Name;
            dto.StatusName = entity.StatusCodeNavigation?.Name;
            dto.CreatedByName = entity.CreatedByNavigation != null
                ? $"{entity.CreatedByNavigation.FirstName} {entity.CreatedByNavigation.LastName}".Trim()
                : null;
            dto.ApprovedByName = entity.ApprovedByNavigation != null
                ? $"{entity.ApprovedByNavigation.FirstName} {entity.ApprovedByNavigation.LastName}".Trim()
                : null;
            dto.LastModifiedByName = entity.LastModifiedByNavigation != null
                ? $"{entity.LastModifiedByNavigation.FirstName} {entity.LastModifiedByNavigation.LastName}".Trim()
                : null;

            // Map details
            if (entity.StockInDetails != null && entity.StockInDetails.Any())
            {
                dto.Details = entity.StockInDetails.Select(d => new StockInDetailResponseDto
                {
                    Id = d.Id,
                    StockInId = d.StockInId,
                    ComponentId = d.ComponentId,
                    ComponentCode = d.ComponentCode ?? "",
                    ComponentName = d.ComponentName ?? "",
                    TypeComponent = d.TypeComponent,
                    Quantity = d.Quantity,
                    QuantityAfterCheck = d.QuantityAfterCheck,
                    ImportPricePerUnit = d.ImportPricePerUnit,
                    ExportPricePerUnit = d.ExportPricePerUnit,
                    Vat = d.Vat,
                    MinQuantity = d.MinQuantity,
                    CreatedAt = d.CreatedAt,
                    LastModifiedDate = d.LastModifiedDate
                }).ToList();
            }

            return dto;
        }

        public async Task<StockInUploadResponseDto> UploadExcelAsync(IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên

            if (worksheet == null)
                throw new ArgumentException("File Excel không có dữ liệu");

            var result = new StockInUploadResponseDto();
            var details = new List<StockInDetailUploadDto>();

            // Đọc từ dòng 4 (dòng 2 = mã yêu cầu, dòng 3 = header, dòng 4+ = data)
            int rowCount = worksheet.Dimension?.Rows ?? 0;
            int colCount = worksheet.Dimension?.Columns ?? 0;
            
            // Xác định format và dòng bắt đầu dữ liệu
            // Format mới (theo hình ảnh): dòng 1 = mã yêu cầu, dòng 2 = header, dòng 3+ = data
            // Format cũ: dòng 2 = mã yêu cầu, dòng 3 = header, dòng 4+ = data
            int dataStartRow = 3; // Mặc định format mới
            int headerRow = 2;
            string? requestCode = null;
            
            // Kiểm tra format mới: dòng 1 có chứa "Mã yêu cầu" không?
            var row1Check = worksheet.Cells[1, 1]?.Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(row1Check) && row1Check.Contains("Mã yêu cầu"))
            {
                // Format mới: dòng 1 = mã yêu cầu, dòng 2 = header, dòng 3+ = data
                requestCode = row1Check;
                headerRow = 2;
                dataStartRow = 3;
            }
            else
            {
                // Thử format cũ: dòng 2 = mã yêu cầu, dòng 3 = header, dòng 4+ = data
                var row2Check = worksheet.Cells[2, 1]?.Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(row2Check) && row2Check.Contains("Mã yêu cầu"))
                {
                    requestCode = row2Check;
                    headerRow = 3;
                    dataStartRow = 4;
                }
                else
                {
                    // Tự động tìm header
                    for (int r = 1; r <= Math.Min(5, rowCount); r++)
                    {
                        var cellValue = worksheet.Cells[r, 2]?.Value?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(cellValue) && cellValue.Contains("Mã linh kiện"))
                        {
                            headerRow = r;
                            dataStartRow = r + 1;
                            // Thử tìm mã yêu cầu ở dòng trước header
                            if (r > 1)
                            {
                                var prevRow = worksheet.Cells[r - 1, 1]?.Value?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(prevRow) && prevRow.Contains("Mã yêu cầu"))
                                {
                                    requestCode = prevRow;
                                }
                            }
                            break;
                        }
                    }
                }
            }

            // Parse mã yêu cầu
            if (!string.IsNullOrEmpty(requestCode) && requestCode.Contains(":"))
            {
                // Format: "Mã yêu cầu: YC2501150001"
                var parts = requestCode.Split(':');
                if (parts.Length > 1)
                    requestCode = parts[1].Trim();
            }
            if (string.IsNullOrEmpty(requestCode) || !requestCode.StartsWith("YC"))
            {
                // Thử lấy từ tên file
                requestCode = file.FileName.Replace(".xlsx", "").Replace(".xls", "");
            }
            result.StockInRequestCode = requestCode;

            if (rowCount < dataStartRow)
                throw new ArgumentException($"File Excel không có dữ liệu linh kiện. Tổng số dòng: {rowCount}, Dòng bắt đầu dữ liệu: {dataStartRow}");

            // Parse dữ liệu linh kiện
            // Format Excel: Dòng 2 = Mã yêu cầu, Dòng 3 = Header, Dòng 4+ = Data
            // Cột: STT(1) | Mã linh kiện(2) | Tên linh kiện(3) | Loại(4) | Số lượng yêu cầu(5) | Số lượng thực tế(6) | Giá nhập(7) | Giá xuất(8)
            var errors = new List<string>();
            int validRowCount = 0;
            int emptyRowCount = 0;
            
            for (int row = dataStartRow; row <= rowCount; row++)
            {
                var componentCode = worksheet.Cells[row, 2]?.Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(componentCode))
                {
                    emptyRowCount++;
                    continue; // Bỏ qua dòng trống
                }

                // Đọc thông tin từ Excel (giống VWMS - không cần component phải tồn tại trong DB)
                var componentNameFromExcel = worksheet.Cells[row, 3]?.Value?.ToString()?.Trim();
                var typeFromExcel = worksheet.Cells[row, 4]?.Value?.ToString()?.Trim();

                // Tìm component theo code (nếu có) - nhưng không bắt buộc
                var component = await _componentRepo.GetByCodeAsync(componentCode);
                
                var quantity = 0;
                if (int.TryParse(worksheet.Cells[row, 5]?.Value?.ToString(), out var qty))
                    quantity = qty;
                else
                {
                    errors.Add($"Dòng {row}: Số lượng yêu cầu không hợp lệ");
                    continue; // Bỏ qua dòng không hợp lệ
                }

                var quantityAfterCheck = quantity;
                if (int.TryParse(worksheet.Cells[row, 6]?.Value?.ToString(), out var qtyAfter))
                    quantityAfterCheck = qtyAfter;

                decimal? importPrice = null;
                if (decimal.TryParse(worksheet.Cells[row, 7]?.Value?.ToString(), out var impPrice))
                    importPrice = impPrice;

                decimal? exportPrice = null;
                if (decimal.TryParse(worksheet.Cells[row, 8]?.Value?.ToString(), out var expPrice))
                    exportPrice = expPrice;

                // Sử dụng thông tin từ Excel, nếu component tồn tại trong DB thì lấy thêm thông tin
                details.Add(new StockInDetailUploadDto
                {
                    ComponentId = component?.Id ?? 0, // 0 nếu không tồn tại
                    ComponentCode = componentCode,
                    ComponentName = !string.IsNullOrEmpty(componentNameFromExcel) ? componentNameFromExcel : (component?.Name ?? ""),
                    TypeComponentName = !string.IsNullOrEmpty(typeFromExcel) ? typeFromExcel : (component?.TypeComponent?.Name ?? ""),
                    Quantity = quantity,
                    QuantityAfterCheck = quantityAfterCheck,
                    ImportPricePerUnit = importPrice,
                    ExportPricePerUnit = exportPrice
                });
                validRowCount++;
            }

            // Chỉ throw exception nếu không có linh kiện hợp lệ nào
            if (details.Count == 0)
            {
                var errorMsg = $"Không tìm thấy linh kiện hợp lệ trong file Excel. ";
                errorMsg += $"Tổng số dòng trong file: {rowCount}, ";
                errorMsg += $"Dòng bắt đầu dữ liệu: {dataStartRow}, ";
                errorMsg += $"Số dòng đã kiểm tra: {rowCount - dataStartRow + 1}, ";
                errorMsg += $"Số dòng trống: {emptyRowCount}";
                if (errors.Count > 0)
                {
                    errorMsg += $"\nLỗi chi tiết:\n{string.Join("\n", errors)}";
                }
                throw new ArgumentException(errorMsg);
            }

            result.Details = details;
            return result;
        }
    }
}

