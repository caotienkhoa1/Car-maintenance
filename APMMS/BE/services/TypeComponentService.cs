using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BE.DTOs.TypeComponent;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class TypeComponentService : ITypeComponentService
    {
        private readonly ITypeComponentRepository _repo;
        private readonly CarMaintenanceDbContext _context;
        private readonly IMapper _mapper;

        public TypeComponentService(ITypeComponentRepository repo, CarMaintenanceDbContext context, IMapper mapper)
        {
            _repo = repo;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ResponseDto> CreateAsync(RequestDto dto)
        {
            var entity = _mapper.Map<TypeComponent>(dto);
            var created = await _repo.AddAsync(entity);
            
            // Nếu có ComponentIds, thêm các components vào TypeComponent này
            if (dto.ComponentIds != null && dto.ComponentIds.Any())
            {
                // Validation: ComponentIds phải tồn tại
                var components = await _context.Components
                    .Where(c => dto.ComponentIds.Contains(c.Id))
                    .ToListAsync();
                
                if (components.Count != dto.ComponentIds.Count)
                {
                    var foundIds = components.Select(c => c.Id).ToList();
                    var notFoundIds = dto.ComponentIds.Except(foundIds).ToList();
                    throw new ArgumentException($"Không tìm thấy components với IDs: {string.Join(", ", notFoundIds)}");
                }
                
                // Validation: Components phải cùng branch với TypeComponent
                if (created.BranchId.HasValue)
                {
                    var invalidComponents = components.Where(c => c.BranchId != created.BranchId.Value).ToList();
                    if (invalidComponents.Any())
                    {
                        throw new ArgumentException($"Các components {string.Join(", ", invalidComponents.Select(c => c.Id))} không thuộc cùng chi nhánh với loại linh kiện này");
                    }
                }

                // Validation bổ sung: linh kiện không được thuộc loại linh kiện khác
                var componentsInOtherTypes = components
                    .Where(c => c.TypeComponentId.HasValue)
                    .ToList();
                if (componentsInOtherTypes.Any())
                {
                    var conflictCodes = componentsInOtherTypes
                        .Select(c => !string.IsNullOrWhiteSpace(c.Code) ? c.Code! : c.Id.ToString())
                        .ToList();
                    throw new ArgumentException($"Các linh kiện {string.Join(", ", conflictCodes)} đã thuộc loại linh kiện khác. Vui lòng bỏ chọn các linh kiện này trước khi tạo loại linh kiện mới.");
                }

                foreach (var component in components)
                {
                    // Cập nhật TypeComponentId của component
                    component.TypeComponentId = created.Id;
                }
                
                await _context.SaveChangesAsync();
            }
            
            return _mapper.Map<ResponseDto>(created);
        }

        public async Task DisableEnableAsync(long id, string statusCode)
        {
            await _repo.DisableEnableAsync(id, statusCode);
        }

        public async Task<List<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var list = await _repo.GetAllAsync(page, pageSize, branchId, statusCode, search);
            return _mapper.Map<List<ResponseDto>>(list);
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null)
        {
            return await _repo.GetTotalCountAsync(branchId, statusCode, search);
        }

        public async Task<ResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseDto>(entity);
        }

        public async Task<ResponseDto?> UpdateAsync(RequestDto dto)
        {
            if (!dto.Id.HasValue) return null;
            var exist = await _repo.GetByIdAsync(dto.Id.Value);
            if (exist == null) return null;

            // Map changed fields
            exist.Name = dto.Name;
            exist.Description = dto.Description;
            exist.BranchId = dto.BranchId;
            exist.StatusCode = dto.StatusCode;

            // Xử lý ComponentIds: thêm/xóa components khỏi TypeComponent này
            if (dto.ComponentIds != null)
            {
                // Lấy danh sách components hiện tại thuộc TypeComponent này
                var currentComponents = await _context.Components
                    .Where(c => c.TypeComponentId == exist.Id)
                    .ToListAsync();
                
                var currentComponentIds = currentComponents.Select(c => c.Id).ToHashSet();
                var newComponentIds = dto.ComponentIds.ToHashSet();
                
                // Xóa các components không còn trong danh sách mới
                var componentsToRemove = currentComponents
                    .Where(c => !newComponentIds.Contains(c.Id))
                    .ToList();
                foreach (var component in componentsToRemove)
                {
                    component.TypeComponentId = null;
                }
                
                // Thêm các components mới vào TypeComponent này
                if (newComponentIds.Any())
                {
                    // Validation: ComponentIds phải tồn tại
                    var componentsToAdd = await _context.Components
                        .Where(c => newComponentIds.Contains(c.Id))
                        .ToListAsync();
                    
                    if (componentsToAdd.Count != newComponentIds.Count)
                    {
                        var foundIds = componentsToAdd.Select(c => c.Id).ToHashSet();
                        var notFoundIds = newComponentIds.Except(foundIds).ToList();
                        throw new ArgumentException($"Không tìm thấy components với IDs: {string.Join(", ", notFoundIds)}");
                    }
                    
                    // Validation: Components phải cùng branch với TypeComponent
                    if (exist.BranchId.HasValue)
                    {
                        var invalidComponents = componentsToAdd.Where(c => c.BranchId != exist.BranchId.Value).ToList();
                        if (invalidComponents.Any())
                        {
                            throw new ArgumentException($"Các components {string.Join(", ", invalidComponents.Select(c => c.Id))} không thuộc cùng chi nhánh với loại linh kiện này");
                        }
                    }

                    // Validation bổ sung: linh kiện không được thuộc loại linh kiện khác
                    var componentsInOtherTypes = componentsToAdd
                        .Where(c => c.TypeComponentId.HasValue && c.TypeComponentId.Value != exist.Id)
                        .ToList();
                    if (componentsInOtherTypes.Any())
                    {
                        var conflictCodes = componentsInOtherTypes
                            .Select(c => !string.IsNullOrWhiteSpace(c.Code) ? c.Code! : c.Id.ToString())
                            .ToList();
                        throw new ArgumentException($"Các linh kiện {string.Join(", ", conflictCodes)} đã thuộc loại linh kiện khác. Vui lòng bỏ chọn các linh kiện này trước khi cập nhật.");
                    }

                    // Cập nhật TypeComponentId cho các components mới
                    foreach (var component in componentsToAdd)
                    {
                        component.TypeComponentId = exist.Id;
                    }
                }
                
                await _context.SaveChangesAsync();
            }

            var updated = await _repo.UpdateAsync(exist);
            return _mapper.Map<ResponseDto>(updated);
        }
    }
}