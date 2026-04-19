using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BE.interfaces;
using BE.models;
using BE.DTOs.Component;
using BE.repository.IRepository;

namespace BE.services
{
    public class ComponentService : IComponentService
    {
        private readonly IComponentRepository _repo;
        private readonly IMapper _mapper;

        public ComponentService(IComponentRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ResponseDto> CreateAsync(RequestDto dto)
        {
            if (!dto.BranchId.HasValue)
            {
                throw new ArgumentException("BranchId is required");
            }

            // Validation: MinimumQuantity phải >= 0
            if (dto.MinimumQuantity.HasValue && dto.MinimumQuantity.Value < 0)
            {
                throw new ArgumentException("Số lượng tối thiểu phải lớn hơn hoặc bằng 0");
            }
            
            // Khi tạo Component mới, chỉ khai báo danh mục
            // Không bao gồm giá và số lượng (chỉ được thêm qua module "Nhập kho")
            var entity = new Component
            {
                Code = dto.Code,
                Name = dto.Name,
                ImageUrl = dto.ImageUrl,
                MinimumQuantity = dto.MinimumQuantity,
                BranchId = dto.BranchId,
                TypeComponentId = dto.TypeComponentId,
                StatusCode = dto.StatusCode ?? "ACTIVE",
                // UnitPrice, PurchasePrice, QuantityStock sẽ null
                // Chỉ được cập nhật qua module "Nhập kho"
            };
            
            var created = await _repo.AddAsync(entity);
            var response = _mapper.Map<ResponseDto>(created);
            // map nested names if needed
            response.TypeComponentName = created.TypeComponent?.Name;
            response.BranchName = created.Branch?.Name;
            return response;
        }

        public async Task DisableEnableAsync(long id, string statusCode)
        {
            await _repo.DisableEnableAsync(id, statusCode);
        }

        public async Task<IEnumerable<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null)
        {
            var list = await _repo.GetAllAsync(page, pageSize, branchId, typeComponentId, statusCode, search);
            var mapped = _mapper.Map<IEnumerable<ResponseDto>>(list);
            // enrich nested names
            foreach (var dst in mapped)
            {
                var src = System.Linq.Enumerable.FirstOrDefault(list, x => x.Id == dst.Id);
                if (src != null)
                {
                    dst.TypeComponentName = src.TypeComponent?.Name;
                    dst.BranchName = src.Branch?.Name;
                }
            }
            return mapped;
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null)
        {
            return await _repo.GetTotalCountAsync(branchId, typeComponentId, statusCode, search);
        }

        public async Task<ResponseDto?> GetByIdAsync(long id)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;
            var dto = _mapper.Map<ResponseDto>(e);
            dto.TypeComponentName = e.TypeComponent?.Name;
            dto.BranchName = e.Branch?.Name;
            return dto;
        }

        public async Task<ResponseDto?> UpdateAsync(RequestDto dto)
        {
            if (!dto.Id.HasValue) return null;
            var exist = await _repo.GetByIdAsync(dto.Id.Value);
            if (exist == null) return null;

            if (exist.BranchId.HasValue && dto.BranchId.HasValue && exist.BranchId.Value != dto.BranchId.Value)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật linh kiện thuộc chi nhánh khác");
            }

            // Không cho phép cập nhật tồn kho/giá qua endpoint metadata
            if (dto.QuantityStock.HasValue || dto.UnitPrice.HasValue || dto.PurchasePrice.HasValue)
            {
                throw new ArgumentException("Không thể cập nhật tồn kho hoặc giá qua API cập nhật thông tin linh kiện.");
            }

            // Cập nhật thông tin danh mục
            exist.Code = dto.Code;
            exist.Name = dto.Name;
            exist.ImageUrl = dto.ImageUrl;
            exist.MinimumQuantity = dto.MinimumQuantity;
            exist.TypeComponentId = dto.TypeComponentId;
            exist.StatusCode = dto.StatusCode;

            var updated = await _repo.UpdateAsync(exist);
            var resp = _mapper.Map<ResponseDto>(updated);
            resp.TypeComponentName = updated.TypeComponent?.Name;
            resp.BranchName = updated.Branch?.Name;
            return resp;
        }

        public async Task<int> BatchUpdateStatusAsync(List<long> componentIds, string statusCode)
        {
            if (componentIds == null || componentIds.Count == 0)
            {
                throw new ArgumentException("Danh sách linh kiện không được rỗng");
            }

            if (string.IsNullOrWhiteSpace(statusCode))
            {
                throw new ArgumentException("Trạng thái không được rỗng");
            }

            // Validate status code
            var validStatusCodes = new[] { "ACTIVE", "OUT_OF_STOCK", "DISCONTINUED" };
            if (!Array.Exists(validStatusCodes, s => s.Equals(statusCode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Trạng thái không hợp lệ: {statusCode}");
            }

            int updatedCount = 0;
            foreach (var id in componentIds)
            {
                try
                {
                    await _repo.DisableEnableAsync(id, statusCode);
                    updatedCount++;
                }
                catch (Exception)
                {
                    // Log error but continue with other items
                }
            }

            return updatedCount;
        }
    }
}
