using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BE.DTOs.TotalReceipt;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class TotalReceiptService : ITotalReceiptService
    {
        private readonly ITotalReceiptRepository _repository;
        private readonly CarMaintenanceDbContext _context;
        private readonly IMapper _mapper;

        public TotalReceiptService(ITotalReceiptRepository repository,
                                   CarMaintenanceDbContext context,
                                   IMapper mapper)
        {
            _repository = repository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ResponseDto> CreateAsync(RequestDto dto)
        {
            if (!dto.MaintenanceTicketId.HasValue)
            {
                throw new ArgumentException("MaintenanceTicketId is required");
            }

            var maintenanceTicket = await _context.MaintenanceTickets
                .Include(mt => mt.Car)
                    .ThenInclude(c => c.User)
                .Include(mt => mt.Branch)
                .Include(mt => mt.ServiceTasks)
                .Include(mt => mt.TicketComponents)
                .Include(mt => mt.ServicePackage)
                    .ThenInclude(sp => sp.ComponentPackages)
                        .ThenInclude(cp => cp.Component) // Load components của package để so sánh
                .FirstOrDefaultAsync(mt => mt.Id == dto.MaintenanceTicketId.Value);

            if (maintenanceTicket == null)
            {
                throw new ArgumentException($"Maintenance ticket #{dto.MaintenanceTicketId.Value} not found");
            }

            if (!string.Equals(maintenanceTicket.StatusCode, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chỉ tạo hóa đơn cho phiếu bảo dưỡng đã hoàn thành");
            }

            if (await _repository.ExistsByMaintenanceTicketIdAsync(maintenanceTicket.Id))
            {
                throw new InvalidOperationException("Phiếu bảo dưỡng này đã có hóa đơn");
            }

            var entity = _mapper.Map<TotalReceipt>(dto);
            entity.MaintenanceTicketId = maintenanceTicket.Id;
            entity.CarId = dto.CarId ?? maintenanceTicket.CarId;
            entity.BranchId = dto.BranchId ?? maintenanceTicket.BranchId;
            entity.CreatedAt = dto.CreatedAt ?? DateTime.UtcNow;
            entity.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? "VND" : dto.CurrencyCode;
            entity.StatusCode = string.IsNullOrWhiteSpace(dto.StatusCode) ? "PENDING" : dto.StatusCode;

            // ✅ Tính tổng retail (giá gốc của components + labor)
            var retailTotal = CalculateMaintenanceTicketTotal(maintenanceTicket);

            // ✅ Xử lý Service Package Discount nếu có
            if (maintenanceTicket.ServicePackageId.HasValue && maintenanceTicket.ServicePackagePrice.HasValue)
            {
                decimal packagePrice = maintenanceTicket.ServicePackagePrice.Value;
                
                // ✅ Chỉ tính giảm giá dựa trên components THUỘC GÓI, không tính phụ tùng thêm vào sau
                var packageRetailTotal = CalculatePackageRetailTotal(maintenanceTicket);
                
                // Tính package discount = packageRetailTotal - packagePrice
                decimal packageDiscount = packageRetailTotal - packagePrice;
                // Nếu package đắt hơn retail thì discount = 0 (không có discount)
                if (packageDiscount < 0) packageDiscount = 0m;
                
                // Lưu thông tin package vào receipt
                entity.ServicePackageId = maintenanceTicket.ServicePackageId;
                entity.ServicePackagePrice = packagePrice;
                entity.ServicePackageName = maintenanceTicket.ServicePackage?.Name;
                entity.PackageDiscountAmount = packageDiscount;
                
                // Subtotal = retail total (tổng giá gốc)
                if (!dto.Subtotal.HasValue || dto.Subtotal.Value == 0m)
                {
                    entity.Subtotal = retailTotal;
                }
                
                // DiscountAmount = discount khác (không phải package discount)
                // Package discount được hiển thị riêng, không tính vào DiscountAmount
                entity.DiscountAmount = dto.DiscountAmount ?? 0m;
            }
            else
            {
                // Không có package, tính bình thường
                if (!dto.Subtotal.HasValue || dto.Subtotal.Value == 0m)
                {
                    entity.Subtotal = retailTotal;
                }
                entity.DiscountAmount = dto.DiscountAmount ?? 0m;
            }

            ApplyFinancials(entity, dto);

            var fallbackAmount = retailTotal;
            if (fallbackAmount > 0m)
            {
                if (entity.Subtotal.GetValueOrDefault() == 0m) entity.Subtotal = fallbackAmount;
                if (!entity.FinalAmount.HasValue || entity.FinalAmount.Value == 0m) entity.FinalAmount = fallbackAmount;
                if (entity.Amount == 0m) entity.Amount = fallbackAmount;
            }

            var created = await _repository.AddAsync(entity);
            return MapToResponse(created);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<ResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<ResponseDto?> GetByMaintenanceTicketIdAsync(long maintenanceTicketId)
        {
            var entity = await _repository.GetByMaintenanceTicketIdAsync(maintenanceTicketId);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<PagedResultDto<ResponseDto>> GetPagedAsync(int page, int pageSize, string? search = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? branchId = null, long? userId = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            // ✅ Tự động lấy BranchId của user đang đăng nhập nếu chưa có
            if (!branchId.HasValue && userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null && user.BranchId.HasValue)
                {
                    branchId = user.BranchId;
                }
            }

            var list = await _repository.GetListAsync(statusCode, branchId, fromDate, toDate);
            var mapped = list.Select(MapToResponse).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLowerInvariant();
                mapped = mapped.Where(r =>
                    (!string.IsNullOrWhiteSpace(r.Code) && r.Code.ToLowerInvariant().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(r.MaintenanceTicketCode) && r.MaintenanceTicketCode.ToLowerInvariant().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(r.CustomerName) && r.CustomerName.ToLowerInvariant().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(r.CustomerPhone) && r.CustomerPhone.Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(r.LicensePlate) && r.LicensePlate.ToLowerInvariant().Contains(keyword)))
                    .ToList();
            }

            var total = mapped.Count;
            var items = mapped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResultDto<ResponseDto>
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ResponseDto?> UpdateAsync(long id, RequestDto dto, long? currentUserId = null)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            if (dto.MaintenanceTicketId.HasValue && dto.MaintenanceTicketId.Value != existing.MaintenanceTicketId)
            {
                var newMaintenanceTicket = await _context.MaintenanceTickets
                    .Include(mt => mt.Car)
                        .ThenInclude(c => c.User)
                    .Include(mt => mt.Branch)
                    .Include(mt => mt.ServiceTasks)
                    .Include(mt => mt.TicketComponents)
                    .Include(mt => mt.ServicePackage)
                        .ThenInclude(sp => sp.ComponentPackages)
                            .ThenInclude(cp => cp.Component) // Load components của package để so sánh
                    .FirstOrDefaultAsync(mt => mt.Id == dto.MaintenanceTicketId.Value);

                if (newMaintenanceTicket == null)
                {
                    throw new ArgumentException($"Maintenance ticket #{dto.MaintenanceTicketId.Value} not found");
                }

                if (!string.Equals(newMaintenanceTicket.StatusCode, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Chỉ gán hóa đơn cho phiếu bảo dưỡng đã hoàn thành");
                }

                if (await _repository.ExistsByMaintenanceTicketIdAsync(newMaintenanceTicket.Id, id))
                {
                    throw new InvalidOperationException("Phiếu bảo dưỡng này đã có hóa đơn");
                }

                existing.MaintenanceTicketId = newMaintenanceTicket.Id;
                existing.CarId = newMaintenanceTicket.CarId;
                existing.BranchId = newMaintenanceTicket.BranchId;
            }

            existing.CarId = dto.CarId ?? existing.CarId;
            existing.BranchId = dto.BranchId ?? existing.BranchId;
            
            // ✅ Xử lý AccountantId: Khi thanh toán (statusCode = 'PAID'), tự động lưu người bấm thanh toán vào AccountantId
            var newStatusCode = string.IsNullOrWhiteSpace(dto.StatusCode) ? existing.StatusCode : dto.StatusCode;
            if (!string.IsNullOrWhiteSpace(newStatusCode) && 
                string.Equals(newStatusCode, "PAID", StringComparison.OrdinalIgnoreCase) &&
                currentUserId.HasValue)
            {
                // Khi thanh toán: Nếu AccountantId chưa có hoặc được truyền null trong DTO, tự động gán currentUserId
                if (!dto.AccountantId.HasValue && !existing.AccountantId.HasValue)
                {
                    existing.AccountantId = currentUserId.Value;
                }
                else if (dto.AccountantId.HasValue)
                {
                    // Nếu DTO có AccountantId, ưu tiên dùng giá trị từ DTO
                    existing.AccountantId = dto.AccountantId.Value;
                }
                // Nếu existing đã có AccountantId và DTO không truyền, giữ nguyên
            }
            else
            {
                // Không phải thanh toán: Cập nhật AccountantId theo DTO hoặc giữ nguyên
                existing.AccountantId = dto.AccountantId ?? existing.AccountantId;
            }
            
            existing.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? (existing.CurrencyCode ?? "VND") : dto.CurrencyCode;
            existing.StatusCode = newStatusCode;
            existing.Note = dto.Note ?? existing.Note;
            existing.CreatedAt = dto.CreatedAt ?? existing.CreatedAt ?? DateTime.UtcNow;

            // ✅ Xử lý Service Package Discount khi update
            var maintenanceTicketEntity = existing.MaintenanceTicket;
            if (maintenanceTicketEntity == null && existing.MaintenanceTicketId.HasValue)
            {
                maintenanceTicketEntity = await _context.MaintenanceTickets
                    .Include(mt => mt.ServiceTasks)
                    .Include(mt => mt.TicketComponents)
                    .Include(mt => mt.ServicePackage)
                        .ThenInclude(sp => sp.ComponentPackages)
                            .ThenInclude(cp => cp.Component) // Load components của package để so sánh
                    .FirstOrDefaultAsync(mt => mt.Id == existing.MaintenanceTicketId.Value);
            }

            if (maintenanceTicketEntity != null)
            {
                var retailTotal = CalculateMaintenanceTicketTotal(maintenanceTicketEntity);
                
                // Nếu có package, tính lại package discount
                if (maintenanceTicketEntity.ServicePackageId.HasValue && maintenanceTicketEntity.ServicePackagePrice.HasValue)
                {
                    decimal packagePrice = maintenanceTicketEntity.ServicePackagePrice.Value;
                    
                    // ✅ Chỉ tính giảm giá dựa trên components THUỘC GÓI, không tính phụ tùng thêm vào sau
                    var packageRetailTotal = CalculatePackageRetailTotal(maintenanceTicketEntity);
                    decimal packageDiscount = packageRetailTotal - packagePrice;
                    if (packageDiscount < 0) packageDiscount = 0m;
                    
                    existing.ServicePackageId = maintenanceTicketEntity.ServicePackageId;
                    existing.ServicePackagePrice = packagePrice;
                    existing.ServicePackageName = maintenanceTicketEntity.ServicePackage?.Name;
                    existing.PackageDiscountAmount = packageDiscount;
                    
                    // Chỉ update nếu không có giá trị từ DTO
                    if (!dto.Subtotal.HasValue || dto.Subtotal.Value == 0m)
                    {
                        existing.Subtotal = retailTotal;
                    }
                    // DiscountAmount = discount khác (không phải package discount)
                    // Package discount được hiển thị riêng, không tính vào DiscountAmount
                    // Chỉ update DiscountAmount nếu có giá trị từ DTO
                }
                else
                {
                    // Không có package, xóa thông tin package
                    existing.ServicePackageId = null;
                    existing.ServicePackagePrice = null;
                    existing.ServicePackageName = null;
                    existing.PackageDiscountAmount = null;
                }
            }

            ApplyFinancials(existing, dto);

            var fallbackAmount = maintenanceTicketEntity != null ? CalculateMaintenanceTicketTotal(maintenanceTicketEntity) : 0m;
            if (fallbackAmount > 0m)
            {
                if (existing.Subtotal.GetValueOrDefault() == 0m) existing.Subtotal = fallbackAmount;
                if (!existing.FinalAmount.HasValue || existing.FinalAmount.Value == 0m) existing.FinalAmount = fallbackAmount;
                if (existing.Amount == 0m) existing.Amount = fallbackAmount;
            }

            var updated = await _repository.UpdateAsync(existing);
            
            // ✅ Load Accountant navigation property nếu chưa có (để map AccountantName)
            if (updated.AccountantId.HasValue && updated.Accountant == null)
            {
                await _context.Entry(updated).Reference(e => e.Accountant).LoadAsync();
            }
            
            return MapToResponse(updated);
        }

        private void ApplyFinancials(TotalReceipt entity, RequestDto dto)
        {
            decimal subtotal;
            if (dto.Subtotal.HasValue)
            {
                subtotal = dto.Subtotal.Value;
            }
            else if (entity.Subtotal.HasValue)
            {
                subtotal = entity.Subtotal.Value;
            }
            else if (dto.Amount.HasValue)
            {
                subtotal = dto.Amount.Value;
            }
            else
            {
                subtotal = entity.Amount;
            }

            var vatPercent = dto.VatPercent ?? entity.VatPercent ?? 10m; // Mặc định 10% VAT
            var surcharge = dto.SurchargeAmount ?? entity.SurchargeAmount ?? 0m;
            var discount = dto.DiscountAmount ?? entity.DiscountAmount ?? 0m;
            var packageDiscount = entity.PackageDiscountAmount ?? 0m; // Package discount chỉ lấy từ entity, không từ DTO
            
            // Tổng discount = package discount + discount khác
            var totalDiscount = packageDiscount + discount;
            
            // ✅ VAT phải tính CUỐI CÙNG, sau khi đã trừ mọi loại giảm giá (KHÔNG tính trên phụ thu)
            // Bước 1: Trừ tất cả giảm giá
            var amountAfterDiscount = subtotal - totalDiscount;
            if (amountAfterDiscount < 0) amountAfterDiscount = 0m; // Đảm bảo không âm
            
            // Bước 2: Tính VAT trên số tiền sau khi đã trừ giảm giá (KHÔNG tính trên phụ thu)
            var vatAmount = dto.VatAmount ?? entity.VatAmount ?? (vatPercent * amountAfterDiscount / 100m);
            
            // Bước 3: Final = (Subtotal - TotalDiscount) + VAT + Surcharge
            var final = dto.FinalAmount ?? entity.FinalAmount ?? (amountAfterDiscount + vatAmount + surcharge);

            entity.Subtotal = subtotal;
            entity.VatPercent = vatPercent;
            entity.VatAmount = vatAmount;
            entity.DiscountAmount = discount;
            entity.SurchargeAmount = surcharge;
            entity.FinalAmount = final;
            entity.Amount = dto.Amount ?? entity.Amount;
        }

        private ResponseDto MapToResponse(TotalReceipt entity)
        {
            // ✅ Load Accountant navigation nếu chưa có (để map AccountantName)
            if (entity.AccountantId.HasValue && entity.Accountant == null)
            {
                _context.Entry(entity).Reference(e => e.Accountant).Load();
            }
            
            // Load ServicePackage navigation nếu chưa có
            if (entity.ServicePackage == null && entity.ServicePackageId.HasValue)
            {
                _context.Entry(entity).Reference(e => e.ServicePackage).Load();
                // Load Components của ServicePackage
                if (entity.ServicePackage != null)
                {
                    _context.Entry(entity.ServicePackage).Collection(sp => sp.ComponentPackages).Load();
                    foreach (var cp in entity.ServicePackage.ComponentPackages)
                    {
                        _context.Entry(cp).Reference(cp => cp.Component).Load();
                    }
                }
            }
            
            // ✅ Nếu receipt chưa có package info nhưng maintenance ticket có package, tính lại
            if (!entity.ServicePackageId.HasValue && entity.MaintenanceTicket != null)
            {
                var ticket = entity.MaintenanceTicket;
                
                // Load ServicePackage với Components nếu chưa có
                if (ticket.ServicePackage == null && ticket.ServicePackageId.HasValue)
                {
                    _context.Entry(ticket).Reference(t => t.ServicePackage).Load();
                    if (ticket.ServicePackage != null)
                    {
                        _context.Entry(ticket.ServicePackage).Collection(sp => sp.ComponentPackages).Load();
                        foreach (var cp in ticket.ServicePackage.ComponentPackages)
                        {
                            _context.Entry(cp).Reference(cp => cp.Component).Load();
                        }
                    }
                }
                
                if (ticket.ServicePackageId.HasValue && ticket.ServicePackagePrice.HasValue)
                {
                    // ✅ Chỉ tính giảm giá dựa trên components THUỘC GÓI, không tính phụ tùng thêm vào sau
                    var packageRetailTotal = CalculatePackageRetailTotal(ticket);
                    var packagePrice = ticket.ServicePackagePrice.Value;
                    var packageDiscount = Math.Max(0, packageRetailTotal - packagePrice);
                    
                    entity.ServicePackageId = ticket.ServicePackageId;
                    entity.ServicePackagePrice = packagePrice;
                    entity.ServicePackageName = ticket.ServicePackage?.Name;
                    entity.PackageDiscountAmount = packageDiscount;
                    
                    // DiscountAmount = discount khác (không phải package discount)
                    // Package discount được hiển thị riêng, không tính vào DiscountAmount
                    // Không tự động set DiscountAmount = packageDiscount
                }
            }
            
            var dto = _mapper.Map<ResponseDto>(entity);
            dto.Code = EnsureInvoiceFormat(dto.Code, GenerateInvoiceCode(entity.Id));
            dto.MaintenanceTicketCode = entity.MaintenanceTicket?.Code;

            var car = entity.Car ?? entity.MaintenanceTicket?.Car;
            if (car != null)
            {
                dto.CarId = car.Id;
                dto.CarName = car.CarName;
                dto.CarModel = car.CarModel;
                dto.VehicleType = car.VehicleType?.Name;
                dto.LicensePlate = car.LicensePlate;
                dto.VinNumber = car.VinNumber;
                dto.EngineNumber = car.VehicleEngineNumber;
                if (car.User != null)
                {
                    var fullName = $"{car.User.FirstName} {car.User.LastName}".Trim();
                    dto.CustomerName = string.IsNullOrWhiteSpace(fullName) ? car.User.Username : fullName;
                    dto.CustomerCode = car.User.Code;
                    dto.CustomerGender = car.User.Gender;
                    dto.CustomerPhone = car.User.Phone;
                    dto.CustomerEmail = car.User.Email;
                    dto.CustomerAddress = car.User.Address;
                }
            }
            
            // Nếu không có từ Car, lấy từ MaintenanceTicket snapshot
            if (entity.MaintenanceTicket != null)
            {
                // Map thêm thời gian bảo dưỡng từ phiếu bảo dưỡng
                dto.MaintenanceStartTime = entity.MaintenanceTicket.StartTime;
                dto.MaintenanceEndTime = entity.MaintenanceTicket.EndTime;

                if (string.IsNullOrWhiteSpace(dto.VinNumber))
                {
                    dto.VinNumber = entity.MaintenanceTicket.SnapshotVinNumber;
                }
                if (string.IsNullOrWhiteSpace(dto.EngineNumber))
                {
                    dto.EngineNumber = entity.MaintenanceTicket.SnapshotEngineNumber;
                }
                // Lấy địa chỉ từ snapshot nếu chưa có
                if (string.IsNullOrWhiteSpace(dto.CustomerAddress))
                {
                    dto.CustomerAddress = entity.MaintenanceTicket.SnapshotCustomerAddress;
                }
                // Lấy thông tin khách hàng từ snapshot nếu chưa có
                if (string.IsNullOrWhiteSpace(dto.CustomerName))
                {
                    dto.CustomerName = entity.MaintenanceTicket.SnapshotCustomerName;
                }
                if (string.IsNullOrWhiteSpace(dto.CustomerPhone))
                {
                    dto.CustomerPhone = entity.MaintenanceTicket.SnapshotCustomerPhone;
                }
                if (string.IsNullOrWhiteSpace(dto.CustomerEmail))
                {
                    dto.CustomerEmail = entity.MaintenanceTicket.SnapshotCustomerEmail;
                }
            }

            dto.BranchId = entity.BranchId ?? entity.MaintenanceTicket?.BranchId;
            dto.BranchName = entity.Branch?.Name ?? entity.MaintenanceTicket?.Branch?.Name;
            dto.BranchAddress = entity.Branch?.Address ?? entity.MaintenanceTicket?.Branch?.Address;
            dto.BranchPhone = entity.Branch?.Phone ?? entity.MaintenanceTicket?.Branch?.Phone;
            dto.AccountantName = entity.Accountant != null
                ? $"{entity.Accountant.FirstName} {entity.Accountant.LastName}".Trim()
                : null;
            dto.StatusName = entity.StatusCodeNavigation?.Name;
            dto.CurrencyCode ??= entity.CurrencyCode ?? "VND";
            dto.FinalAmount ??= entity.FinalAmount ?? entity.Amount;
            dto.Amount ??= entity.Amount;
            dto.Subtotal ??= entity.Subtotal;
            
            // Đảm bảo VAT luôn có giá trị mặc định 10% nếu chưa có
            // Luôn set giá trị, không dùng ??= để đảm bảo override giá trị từ AutoMapper
            var vatPercent = entity.VatPercent ?? 10m;
            dto.VatPercent = vatPercent; // Luôn set, không dùng ??=
                      
            dto.SurchargeAmount ??= entity.SurchargeAmount ?? 0m;
            dto.DiscountAmount ??= entity.DiscountAmount ?? 0m;
            dto.CreatedAt ??= entity.CreatedAt;
            
            // ✅ Map Service Package fields (luôn map, kể cả null)
            dto.ServicePackageId = entity.ServicePackageId;
            dto.ServicePackagePrice = entity.ServicePackagePrice;
            dto.ServicePackageName = entity.ServicePackageName ?? entity.ServicePackage?.Name;
            dto.PackageDiscountAmount = entity.PackageDiscountAmount;
            
            // VAT được tính trên giá sau khi đã trừ giảm giá và cộng phụ thụ (theo quy định)
            var subtotalForVat = dto.Subtotal ?? entity.Subtotal ?? entity.Amount;
            var discountForVat = dto.DiscountAmount ?? entity.DiscountAmount ?? 0m;
            var surchargeForVat = dto.SurchargeAmount ?? entity.SurchargeAmount ?? 0m;
            var subtotalAfterDiscount = subtotalForVat - discountForVat;
            if (subtotalAfterDiscount < 0) subtotalAfterDiscount = 0m; // Đảm bảo không âm
            var subtotalForVatCalculation = subtotalAfterDiscount + surchargeForVat; // VAT tính trên cả phụ thu
            
            // Tính VAT amount nếu chưa có hoặc cần tính lại
            if (!entity.VatAmount.HasValue || entity.VatAmount.Value == 0m)
            {
                dto.VatAmount = vatPercent * subtotalForVatCalculation / 100m;
            }
            else
            {
                dto.VatAmount = entity.VatAmount;
            }
            
            // Cập nhật FinalAmount: Subtotal - Discount + VAT + Surcharge
            if (!entity.FinalAmount.HasValue || entity.VatAmount != dto.VatAmount)
            {
                var newSubtotal = dto.Subtotal ?? entity.Subtotal ?? 0m;
                var newVat = dto.VatAmount ?? 0m;
                var newSurcharge = dto.SurchargeAmount ?? 0m;
                var newDiscount = dto.DiscountAmount ?? 0m;
                dto.FinalAmount = newSubtotal - newDiscount + newVat + newSurcharge;
            }

            if (dto.Amount.GetValueOrDefault() == 0m || dto.FinalAmount.GetValueOrDefault() == 0m)
            {
                var fallback = CalculateMaintenanceTicketTotal(entity.MaintenanceTicket);
                if (fallback > 0m)
                {
                    if (dto.Amount.GetValueOrDefault() == 0m) dto.Amount = fallback;
                    if (dto.FinalAmount.GetValueOrDefault() == 0m) dto.FinalAmount = fallback;
                    if (dto.Subtotal.GetValueOrDefault() == 0m) dto.Subtotal = fallback;
                }
            }

            return dto;
        }

        private static string EnsureInvoiceFormat(string? existing, string fallback)
        {
            if (string.IsNullOrWhiteSpace(existing)) return fallback;
            var normalized = existing.Trim().ToUpperInvariant();
            return normalized.StartsWith("INV") && normalized.Length == 9 ? normalized : fallback;
        }

        private static string GenerateInvoiceCode(long seed)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var value = seed > 0 ? seed : DateTime.UtcNow.Ticks;
            var result = string.Empty;
            for (var i = 0; i < 6; i++)
            {
                value = (value * 9301 + 49297) % 233280;
                result += chars[(int)(value % chars.Length)];
            }
            return $"INV{result}";
        }

        private static decimal CalculateMaintenanceTicketTotal(MaintenanceTicket? maintenanceTicket)
        {
            if (maintenanceTicket == null) return 0m;

            decimal componentTotal = 0m;
            if (maintenanceTicket.TicketComponents != null)
            {
                foreach (var component in maintenanceTicket.TicketComponents)
                {
                    var quantity = component.ActualQuantity ?? Convert.ToDecimal(component.Quantity);
                    var unitPrice = component.UnitPrice ?? 0m;
                    componentTotal += quantity * unitPrice;
                }
            }

            decimal laborTotal = 0m;
            if (maintenanceTicket.ServiceTasks != null)
            {
                laborTotal = maintenanceTicket.ServiceTasks.Sum(task => task.LaborCost ?? 0m);
            }

            var estimated = maintenanceTicket.TotalEstimatedCost ?? 0m;
            var total = componentTotal + laborTotal;
            if (total <= 0m)
            {
                total = estimated;
            }

            return total;
        }

        /// <summary>
        /// Tính tổng giá retail chỉ cho các components THUỘC GÓI DỊCH VỤ
        /// Không tính các phụ tùng được thêm vào sau khi áp dụng gói
        /// </summary>
        private static decimal CalculatePackageRetailTotal(MaintenanceTicket? maintenanceTicket)
        {
            if (maintenanceTicket == null) return 0m;
            if (!maintenanceTicket.ServicePackageId.HasValue) return 0m;
            if (maintenanceTicket.ServicePackage == null) return 0m;

            // Lấy danh sách ComponentId thuộc gói dịch vụ
            var packageComponentIds = maintenanceTicket.ServicePackage.ComponentPackages?
                .Select(cp => cp.ComponentId)
                .ToHashSet() ?? new HashSet<long>();

            if (!packageComponentIds.Any()) return 0m;

            // Chỉ tính components có ComponentId thuộc gói
            decimal packageComponentTotal = 0m;
            if (maintenanceTicket.TicketComponents != null)
            {
                foreach (var ticketComponent in maintenanceTicket.TicketComponents)
                {
                    // Chỉ tính nếu ComponentId thuộc gói
                    if (ticketComponent.ComponentId.HasValue && 
                        packageComponentIds.Contains(ticketComponent.ComponentId.Value))
                    {
                        var quantity = ticketComponent.ActualQuantity ?? Convert.ToDecimal(ticketComponent.Quantity);
                        var unitPrice = ticketComponent.UnitPrice ?? 0m;
                        packageComponentTotal += quantity * unitPrice;
                    }
                }
            }

            // Note: ServiceTasks không được tự động tạo từ gói, nên không tính vào package retail total
            // Nếu cần tính tasks thuộc gói, cần thêm logic so sánh với ServicePackageCategories

            return packageComponentTotal;
        }
    }
}


