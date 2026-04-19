using AutoMapper;
using BE.DTOs.ServicePackage;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class ServicePackageService : IServicePackageService
    {
        private readonly IServicePackageRepository _repo;
        private readonly CarMaintenanceDbContext _context;
        private readonly IMapper _mapper;

        public ServicePackageService(IServicePackageRepository repo, CarMaintenanceDbContext context, IMapper mapper)
        {
            _repo = repo;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ResponseDto> CreateAsync(RequestDto dto)
        {
            if (!dto.BranchId.HasValue)
            {
                throw new ArgumentException("BranchId is required");
            }

            // Validation: Package phải có ít nhất 1 component
            var hasComponents = (dto.Components != null && dto.Components.Any()) || 
                                (dto.ComponentIds != null && dto.ComponentIds.Any());
            if (!hasComponents)
            {
                throw new ArgumentException("ServicePackage phải có ít nhất 1 component");
            }

            // Validation: StatusCode nếu có thì phải hợp lệ
            var validStatusCodes = new[] { "ACTIVE", "INACTIVE" };
            if (!string.IsNullOrWhiteSpace(dto.StatusCode) &&
                !validStatusCodes.Contains(dto.StatusCode))
            {
                throw new ArgumentException($"Trạng thái gói dịch vụ không hợp lệ: {dto.StatusCode}");
            }

            var entity = new ServicePackage
            {
                BranchId = dto.BranchId,
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StatusCode = dto.StatusCode
            };

            var created = await _repo.AddAsync(entity);
            
            // attach components if provided (sau khi đã có ID)
            if (dto.Components != null && dto.Components.Any())
            {
                // Gom các dòng có cùng ComponentId (tránh tạo trùng)
                var componentInfos = dto.Components
                    .GroupBy(c => c.ComponentId)
                    .Select(g => new { ComponentId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                    .ToList();

                // Validation: Components phải tồn tại
                var componentIds = componentInfos.Select(c => c.ComponentId).ToList();
                var existingComponents = await _context.Components
                    .Where(c => componentIds.Contains(c.Id))
                    .ToListAsync();
                
                if (existingComponents.Count != componentIds.Count)
                {
                    var foundIds = existingComponents.Select(c => c.Id).ToList();
                    var notFoundIds = componentIds.Except(foundIds).ToList();
                    throw new ArgumentException($"Không tìm thấy components với IDs: {string.Join(", ", notFoundIds)}");
                }
                
                // Validation: Components phải cùng branch với package
                if (created.BranchId.HasValue)
                {
                    var invalidComponents = existingComponents.Where(c => c.BranchId != created.BranchId.Value).ToList();
                    if (invalidComponents.Any())
                    {
                        throw new ArgumentException($"Các components {string.Join(", ", invalidComponents.Select(c => c.Id))} không thuộc cùng chi nhánh với gói dịch vụ này");
                    }
                }
                
                // Validation: Price >= 0 (nếu có)
                if (dto.Price.HasValue && dto.Price.Value < 0)
                {
                    throw new ArgumentException("Giá gói dịch vụ phải lớn hơn hoặc bằng 0");
                }
                
                foreach (var comp in componentInfos)
                {
                    // Validation: Quantity phải > 0
                    if (comp.Quantity <= 0)
                    {
                        throw new ArgumentException($"Quantity của component {comp.ComponentId} phải lớn hơn 0");
                    }
                    
                    created.ComponentPackages.Add(new ComponentPackage
                    {
                        ComponentId = comp.ComponentId,
                        ServicePackageId = created.Id,
                        Quantity = comp.Quantity
                    });
                }
                await _context.SaveChangesAsync();
            }
            // Backward compatibility: Nếu dùng ComponentIds (cũ)
            else if (dto.ComponentIds != null && dto.ComponentIds.Any())
            {
                foreach (var componentId in dto.ComponentIds.Distinct())
                {
                    created.ComponentPackages.Add(new ComponentPackage
                    {
                        ComponentId = componentId,
                        ServicePackageId = created.Id,
                        Quantity = 1 // Default quantity
                    });
                }
                await _context.SaveChangesAsync();
            }
            
            return MapToResponse(created);
        }

        public async Task DisableEnableAsync(long id, string statusCode)
        {
            await _repo.DisableEnableAsync(id, statusCode);
        }

        public async Task<IEnumerable<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var list = await _repo.GetAllAsync(page, pageSize, branchId, statusCode, search);
            return list.Select(MapToResponse).ToList();
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null)
        {
            return await _repo.GetTotalCountAsync(branchId, statusCode, search);
        }

        public async Task<ResponseDto?> GetByIdAsync(long id)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;
            return MapToResponse(e);
        }

        public async Task<ResponseDto?> UpdateAsync(RequestDto dto)
        {
            if (!dto.Id.HasValue) return null;
            var exist = await _repo.GetByIdAsync(dto.Id.Value);
            if (exist == null) return null;

            // Không cho phép thay đổi BranchId nếu đã có
            if (exist.BranchId.HasValue && dto.BranchId.HasValue && exist.BranchId.Value != dto.BranchId.Value)
            {
                throw new ArgumentException("Không thể thay đổi chi nhánh của gói dịch vụ hiện tại");
            }

            // Validation: StatusCode nếu có thì phải hợp lệ
            var validStatusCodes = new[] { "ACTIVE", "INACTIVE" };
            if (!string.IsNullOrWhiteSpace(dto.StatusCode) &&
                !validStatusCodes.Contains(dto.StatusCode))
            {
                throw new ArgumentException($"Trạng thái gói dịch vụ không hợp lệ: {dto.StatusCode}");
            }

            if (dto.BranchId.HasValue)
            {
                exist.BranchId = dto.BranchId;
            }
            exist.Code = dto.Code;
            exist.Name = dto.Name;
            exist.Description = dto.Description;
            exist.Price = dto.Price;
            exist.StatusCode = dto.StatusCode;

            // Validation: Price >= 0 (nếu có)
            if (dto.Price.HasValue && dto.Price.Value < 0)
            {
                throw new ArgumentException("Giá gói dịch vụ phải lớn hơn hoặc bằng 0");
            }
            
            // update components relationship
            if (dto.Components != null && dto.Components.Any())
            {
                // Gom các dòng có cùng ComponentId
                var componentInfos = dto.Components
                    .GroupBy(c => c.ComponentId)
                    .Select(g => new { ComponentId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                    .ToList();

                // Validation: Components phải tồn tại
                var componentIds = componentInfos.Select(c => c.ComponentId).ToList();
                var existingComponents = await _context.Components
                    .Where(c => componentIds.Contains(c.Id))
                    .ToListAsync();
                
                if (existingComponents.Count != componentIds.Count)
                {
                    var foundIds = existingComponents.Select(c => c.Id).ToList();
                    var notFoundIds = componentIds.Except(foundIds).ToList();
                    throw new ArgumentException($"Không tìm thấy components với IDs: {string.Join(", ", notFoundIds)}");
                }
                
                // Validation: Components phải cùng branch với package
                if (exist.BranchId.HasValue)
                {
                    var invalidComponents = existingComponents.Where(c => c.BranchId != exist.BranchId.Value).ToList();
                    if (invalidComponents.Any())
                    {
                        throw new ArgumentException($"Các components {string.Join(", ", invalidComponents.Select(c => c.Id))} không thuộc cùng chi nhánh với gói dịch vụ này");
                    }
                }
                
                // Validation: Quantity phải > 0 cho mỗi component
                foreach (var comp in componentInfos)
                {
                    if (comp.Quantity <= 0)
                    {
                        throw new ArgumentException($"Quantity của component {comp.ComponentId} phải lớn hơn 0");
                    }
                }
                
                // Remove existing ComponentPackages
                var existingComponentPackages = await _context.ComponentPackages
                    .Where(cp => cp.ServicePackageId == exist.Id)
                    .ToListAsync();
                _context.ComponentPackages.RemoveRange(existingComponentPackages);
                
                // Add new ComponentPackages với Quantity từ DTO đã gom
                foreach (var comp in componentInfos)
                {
                    exist.ComponentPackages.Add(new ComponentPackage
                    {
                        ComponentId = comp.ComponentId,
                        ServicePackageId = exist.Id,
                        Quantity = comp.Quantity
                    });
                }
            }
            // Backward compatibility: Nếu dùng ComponentIds (cũ)
            else if (dto.ComponentIds != null && dto.ComponentIds.Any())
            {
                // Remove existing ComponentPackages
                var existingComponentPackages = await _context.ComponentPackages
                    .Where(cp => cp.ServicePackageId == exist.Id)
                    .ToListAsync();
                _context.ComponentPackages.RemoveRange(existingComponentPackages);
                
                // Add new ComponentPackages với Quantity mặc định = 1
                foreach (var componentId in dto.ComponentIds.Distinct())
                {
                    exist.ComponentPackages.Add(new ComponentPackage
                    {
                        ComponentId = componentId,
                        ServicePackageId = exist.Id,
                        Quantity = 1 // Default quantity
                    });
                }
            }

            var updated = await _repo.UpdateAsync(exist);
            return MapToResponse(updated);
        }

        private ResponseDto MapToResponse(ServicePackage sp)
        {
            var dto = _mapper.Map<ResponseDto>(sp);
            dto.BranchName = sp.Branch?.Name;
            dto.Components = sp.ComponentPackages?.Select(cp => new ResponseDto.ComponentSummary
            {
                Id = cp.Component.Id,
                Code = cp.Component.Code,
                Name = cp.Component.Name,
                UnitPrice = cp.Component.UnitPrice,
                QuantityStock = cp.Component.QuantityStock,
                ImageUrl = cp.Component.ImageUrl,
                Quantity = cp.Quantity, // Lấy từ bảng trung gian
                StatusCode = cp.Component.StatusCode // Trạng thái của linh kiện
            }).ToList();
            
            
            dto.ServiceCategories = null; // Không load ServiceCategories
            
            return dto;
        }
    }
}


