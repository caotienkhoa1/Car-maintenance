using AutoMapper;
using BE.vn.fpt.edu.DTOs.AutoOwner;
using BE.vn.fpt.edu.models;
using BE.vn.fpt.edu.repository.IRepository;
using BE.vn.fpt.edu.services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BE.UnitTests.Services
{
    public class AutoOwnerServiceTests
    {
        private readonly Mock<IAutoOwnerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<CarMaintenanceDbContext> _dbMock;
        private readonly AutoOwnerService _service;

        public AutoOwnerServiceTests()
        {
            _repoMock = new Mock<IAutoOwnerRepository>();
            _mapperMock = new Mock<IMapper>();

            // Fake DbContext
            var options = new DbContextOptionsBuilder<CarMaintenanceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbMock = new Mock<CarMaintenanceDbContext>(options);

            _service = new AutoOwnerService(_repoMock.Object, _mapperMock.Object, _dbMock.Object);
        }

        // ================
        // 1. GetAllAsync()
        // ================
        [Fact]
        public async Task GetAllAsync_ReturnsMappedList()
        {
            // Arrange
            var users = new List<User> { new User { Id = 1, Username = "A" } };

            _repoMock.Setup(r => r.GetAllAsync(1, 10))
                     .ReturnsAsync(users);

            _mapperMock.Setup(m => m.Map<List<ResponseDto>>(users))
                       .Returns(new List<ResponseDto> { new ResponseDto { Id = 1 } });

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        // ======================================
        // 2. GetWithFiltersAsync()
        // ======================================
        [Fact]
        public async Task GetWithFiltersAsync_ReturnsPagedResponse()
        {
            // Arrange
            var users = new List<User> { new User { Id = 1 } };
            _repoMock.Setup(r => r.GetWithFiltersAsync(1, 10, null, null, null))
                     .ReturnsAsync(users);

            _repoMock.Setup(r => r.GetTotalCountAsync(null, null, null))
                     .ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<List<ResponseDto>>(users))
                       .Returns(new List<ResponseDto> { new ResponseDto { Id = 1 } });

            // Act
            var result = await _service.GetWithFiltersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(((dynamic)result).success);
            Assert.Equal(1, ((dynamic)result).totalCount);
        }


        // ======================================
        // 3. GetByIdAsync()
        // ======================================
        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenFound()
        {
            var user = new User { Id = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<ResponseDto>(user))
                       .Returns(new ResponseDto { Id = 1 });

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);

            var result = await _service.GetByIdAsync(1);

            Assert.Null(result);
        }


        // ======================================
        // 4. CreateAsync()
        // ======================================
        [Fact]
        public async Task CreateAsync_CreatesAutoOwnerSuccessfully()
        {
            // Arrange
            var dto = new RequestDto { Username = "A", Address = "HN" };
            var user = new User { Username = "A" };

            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            _mapperMock.Setup(m => m.Map<ResponseDto>(user))
                       .Returns(new ResponseDto { Username = "A" });

            _repoMock.Setup(r => r.CreateAsync(user)).ReturnsAsync(user);

            // Fake DB for code generation
            _dbMock.Setup(d => d.Users)
                   .Returns(DbContextMock.GetQueryableMockDbSet(new List<User>()));

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("A", result.Username);
            Assert.Equal(7, user.RoleId);
            Assert.Equal("ACTIVE", user.StatusCode);
        }


        // ======================================
        // 5. UpdateAsync()
        // ======================================
        [Fact]
        public async Task UpdateAsync_Throws_WhenNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);

            var dto = new RequestDto();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(1, dto));
        }

        [Fact]
        public async Task UpdateAsync_UpdatesSuccessfully()
        {
            var existing = new User { Id = 1, Address = "Old" };
            var dto = new RequestDto { Address = "New" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(dto, existing));

            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

            _mapperMock.Setup(m => m.Map<ResponseDto>(existing))
                       .Returns(new ResponseDto { Id = 1 });

            var result = await _service.UpdateAsync(1, dto);

            Assert.Equal(1, result.Id);
            Assert.Equal("New", existing.Address);
        }


        // ======================================
        // 6. UpdateStatusAsync()
        // ======================================
        [Fact]
        public async Task UpdateStatusAsync_Throws_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateStatusAsync(1, "ACTIVE"));
        }

        [Fact]
        public async Task UpdateStatusAsync_Throws_WhenInvalidStatus()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateStatusAsync(1, "XYZ"));
        }

        [Fact]
        public async Task UpdateStatusAsync_UpdatesSuccessfully()
        {
            var user = new User { Id = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _repoMock.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

            _mapperMock.Setup(m => m.Map<ResponseDto>(user))
                       .Returns(new ResponseDto { Id = 1, StatusCode = "ACTIVE" });

            var result = await _service.UpdateStatusAsync(1, "ACTIVE");

            Assert.Equal("ACTIVE", user.StatusCode);
        }
    }
}
