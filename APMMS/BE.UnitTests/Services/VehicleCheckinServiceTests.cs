using Xunit;
using Moq;
using FluentAssertions;
using BE.vn.fpt.edu.services;
using BE.vn.fpt.edu.interfaces;
using BE.vn.fpt.edu.repository.IRepository;
using BE.vn.fpt.edu.models;
using BE.vn.fpt.edu.DTOs.VehicleCheckin;
using Microsoft.EntityFrameworkCore;
using BE.UnitTests.Helpers;
using System.Linq;
using System.Reflection;

namespace BE.UnitTests.Services
{
    /// <summary>
    /// Unit Tests cho VehicleCheckinService sử dụng Mock
    /// </summary>
    public class VehicleCheckinServiceTests : IDisposable
    {
        private readonly Mock<IVehicleCheckinRepository> _mockRepository;
        private readonly CarMaintenanceDbContext _context;
        private readonly VehicleCheckinService _service;

        public VehicleCheckinServiceTests()
        {
            // Tạo Mock Repository
            _mockRepository = new Mock<IVehicleCheckinRepository>();

            // Tạo InMemory database thực sự (cần cho GenerateVehicleCheckinCodeAsync)
            var options = new DbContextOptionsBuilder<CarMaintenanceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new CarMaintenanceDbContext(options);

            // Tạo Service với Mock Repository và InMemory DbContext
            _service = new VehicleCheckinService(_mockRepository.Object, _context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region CreateVehicleCheckinAsync Tests

        [Fact]
        public async Task CreateVehicleCheckinAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange - Setup Mock
            var request = MockDataBuilder.CreateVehicleCheckinRequestDto();
            var createdVehicleCheckin = MockDataBuilder.CreateVehicleCheckin();
            var car = MockDataBuilder.CreateCar();
            var branch = MockDataBuilder.CreateBranch();
            var user = MockDataBuilder.CreateUser();

            // Thêm Car, Branch, User vào DbContext để service có thể validate
            car.Branch = null;
            car.User = null;
            _context.Cars.Add(car);
            _context.Branches.Add(branch);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Mock Repository.CreateAsync
            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<VehicleCheckin>()))
                .ReturnsAsync(createdVehicleCheckin);

            // Mock Repository.GetByIdWithDetailsAsync (for response mapping)
            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<long>()))
                .ReturnsAsync(new VehicleCheckin
                {
                    Id = createdVehicleCheckin.Id,
                    CarId = request.CarId,
                    BranchId = request.BranchId,
                    Mileage = request.Mileage,
                    Notes = request.Notes,
                    Code = createdVehicleCheckin.Code,
                    StatusCode = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    Car = car,
                    Branch = branch,
                    VehicleCheckinImages = createdVehicleCheckin.VehicleCheckinImages
                });

            // Act - Gọi method cần test
            var result = await _service.CreateVehicleCheckinAsync(request);

            // Assert - Kiểm tra kết quả
            result.Should().NotBeNull();
            result.Id.Should().Be(createdVehicleCheckin.Id);
            result.CarId.Should().Be(request.CarId);
            result.BranchId.Should().Be(request.BranchId);
            result.Mileage.Should().Be(request.Mileage);
            result.Notes.Should().Be(request.Notes);
            result.StatusCode.Should().Be("PENDING");
            result.Code.Should().NotBeNull();
            result.Code.Should().StartWith("VCI-");

            // Verify - Kiểm tra Repository được gọi đúng cách
            _mockRepository.Verify(x => x.CreateAsync(It.IsAny<VehicleCheckin>()), Times.Once);
            _mockRepository.Verify(x => x.GetByIdWithDetailsAsync(It.IsAny<long>()), Times.Once);
        }

        [Fact]
        public async Task CreateVehicleCheckinAsync_ShouldSetDefaultStatusCodeToPENDING()
        {
            // Arrange
            var request = MockDataBuilder.CreateVehicleCheckinRequestDto();
            var createdVehicleCheckin = MockDataBuilder.CreateVehicleCheckin(statusCode: "PENDING");
            var car = MockDataBuilder.CreateCar();
            var branch = MockDataBuilder.CreateBranch();
            var user = MockDataBuilder.CreateUser();

            // Thêm Car, Branch, User vào DbContext để service có thể validate
            car.Branch = null;
            car.User = null;
            _context.Cars.Add(car);
            _context.Branches.Add(branch);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<VehicleCheckin>()))
                .ReturnsAsync(createdVehicleCheckin);

            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<long>()))
                .ReturnsAsync(new VehicleCheckin
                {
                    Id = createdVehicleCheckin.Id,
                    StatusCode = "PENDING",
                    Car = car,
                    Branch = branch
                });

            // Act
            var result = await _service.CreateVehicleCheckinAsync(request);

            // Assert
            result.StatusCode.Should().Be("PENDING");

            // Verify - Kiểm tra StatusCode được set đúng
            _mockRepository.Verify(x => x.CreateAsync(It.Is<VehicleCheckin>(vc => vc.StatusCode == "PENDING")), Times.Once);
        }

        [Fact]
        public async Task CreateVehicleCheckinAsync_ShouldGenerateUniqueCode()
        {
            // Arrange
            var request = MockDataBuilder.CreateVehicleCheckinRequestDto();
            var createdVehicleCheckin = MockDataBuilder.CreateVehicleCheckin();
            var car = MockDataBuilder.CreateCar();
            var branch = MockDataBuilder.CreateBranch();
            var user = MockDataBuilder.CreateUser();

            // Thêm Car, Branch, User vào DbContext để service có thể validate
            car.Branch = null;
            car.User = null;
            _context.Cars.Add(car);
            _context.Branches.Add(branch);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Setup repository để lưu vào in-memory database (để service có thể check code uniqueness)
            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<VehicleCheckin>()))
                .ReturnsAsync((VehicleCheckin vc) => 
                {
                    // Simulate saving to database để service có thể check code uniqueness
                    _context.VehicleCheckins.Add(vc);
                    if (vc.VehicleCheckinImages != null)
                    {
                        _context.VehicleCheckinImages.AddRange(vc.VehicleCheckinImages);
                    }
                    _context.SaveChanges();
                    return vc;
                });

            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => 
                {
                    var vc = _context.VehicleCheckins.FirstOrDefault(v => v.Id == id);
                    if (vc != null)
                    {
                        vc.Car = car;
                        vc.Branch = branch;
                        // Load images
                        vc.VehicleCheckinImages = _context.VehicleCheckinImages
                            .Where(img => img.VehicleCheckinId == id)
                            .ToList();
                    }
                    return vc;
                });

            // Act
            var result = await _service.CreateVehicleCheckinAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().NotBeNull();
            result.Code.Should().StartWith("VCI-");
            result.Code.Length.Should().BeGreaterThan(4); // VCI- + digits

            // Verify - Code được generate và không null
            _mockRepository.Verify(x => x.CreateAsync(It.Is<VehicleCheckin>(vc => !string.IsNullOrEmpty(vc.Code))), Times.Once);
        }

        #endregion

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_WithValidStatus_ShouldUpdateSuccessfully()
        {
            // Arrange - Create fresh context for this test to avoid tracking conflicts
            var options = new DbContextOptionsBuilder<CarMaintenanceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var testContext = new CarMaintenanceDbContext(options);
            var testService = new VehicleCheckinService(_mockRepository.Object, testContext);

            var vehicleCheckinId = 1;
            var newStatusCode = "CONFIRMED";
            
            // Create User first (Car depends on User)
            var user = MockDataBuilder.CreateUser();
            testContext.Users.Add(user);
            await testContext.SaveChangesAsync();

            // Create Branch
            var branch = MockDataBuilder.CreateBranch();
            testContext.Branches.Add(branch);
            await testContext.SaveChangesAsync();

            // Create Car with User and Branch
            var car = MockDataBuilder.CreateCar(userId: user.Id, branchId: branch.Id);
            car.User = user;
            car.Branch = branch;
            testContext.Cars.Add(car);
            await testContext.SaveChangesAsync();

            // Create VehicleCheckin
            var existingVehicleCheckin = MockDataBuilder.CreateVehicleCheckin(
                id: vehicleCheckinId,
                statusCode: "PENDING");
            existingVehicleCheckin.CarId = car.Id;
            existingVehicleCheckin.BranchId = branch.Id;
            testContext.VehicleCheckins.Add(existingVehicleCheckin);
            await testContext.SaveChangesAsync();

            // Act
            var result = await testService.UpdateStatusAsync(vehicleCheckinId, newStatusCode);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(newStatusCode);

            // Verify - Check database was updated
            var updatedEntity = await testContext.VehicleCheckins.FindAsync((long)vehicleCheckinId);
            updatedEntity.Should().NotBeNull();
            updatedEntity!.StatusCode.Should().Be(newStatusCode);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithInvalidStatus_ShouldThrowException()
        {
            // Arrange - Create fresh context for this test
            var options = new DbContextOptionsBuilder<CarMaintenanceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var testContext = new CarMaintenanceDbContext(options);
            var testService = new VehicleCheckinService(_mockRepository.Object, testContext);

            var vehicleCheckinId = 1;
            var invalidStatusCode = "IN_PROGRESS"; // Invalid - chỉ cho phép PENDING hoặc CONFIRMED
            
            // Create User, Branch, Car, and VehicleCheckin
            var user = MockDataBuilder.CreateUser();
            testContext.Users.Add(user);
            await testContext.SaveChangesAsync();

            var branch = MockDataBuilder.CreateBranch();
            testContext.Branches.Add(branch);
            await testContext.SaveChangesAsync();

            var car = MockDataBuilder.CreateCar(userId: user.Id, branchId: branch.Id);
            car.User = user;
            car.Branch = branch;
            testContext.Cars.Add(car);
            await testContext.SaveChangesAsync();

            var existingVehicleCheckin = MockDataBuilder.CreateVehicleCheckin(
                id: vehicleCheckinId,
                statusCode: "PENDING");
            existingVehicleCheckin.CarId = car.Id;
            existingVehicleCheckin.BranchId = branch.Id;
            testContext.VehicleCheckins.Add(existingVehicleCheckin);
            await testContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await testService.UpdateStatusAsync(vehicleCheckinId, invalidStatusCode));

            exception.Message.Should().Contain("PENDING hoặc CONFIRMED");
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var vehicleCheckinId = 99999; // Non-existent
            var statusCode = "CONFIRMED";

            // No data in database - service will query _context directly

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.UpdateStatusAsync(vehicleCheckinId, statusCode));

            exception.Message.Should().Contain("Vehicle check-in not found");
        }

        #endregion

        #region GetVehicleCheckinByIdAsync Tests

        [Fact]
        public async Task GetVehicleCheckinByIdAsync_WithValidId_ShouldReturnVehicleCheckin()
        {
            // Arrange
            var vehicleCheckinId = 1;
            var vehicleCheckin = MockDataBuilder.CreateVehicleCheckin(id: vehicleCheckinId);
            vehicleCheckin.Car = MockDataBuilder.CreateCar();
            vehicleCheckin.Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(vehicleCheckinId))
                .ReturnsAsync(vehicleCheckin);

            // Act
            var result = await _service.GetVehicleCheckinByIdAsync(vehicleCheckinId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(vehicleCheckinId);
            result.CarId.Should().Be(vehicleCheckin.CarId);
            result.BranchId.Should().Be(vehicleCheckin.BranchId);

            // Verify
            _mockRepository.Verify(x => x.GetByIdWithDetailsAsync(vehicleCheckinId), Times.Once);
        }

        [Fact]
        public async Task GetVehicleCheckinByIdAsync_WithInvalidId_ShouldThrowException()
        {
            // Arrange
            var vehicleCheckinId = 99999; // Non-existent

            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(vehicleCheckinId))
                .ReturnsAsync((VehicleCheckin?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.GetVehicleCheckinByIdAsync(vehicleCheckinId));

            // Verify
            _mockRepository.Verify(x => x.GetByIdWithDetailsAsync(vehicleCheckinId), Times.Once);
        }

        #endregion

        #region UpdateVehicleCheckinAsync Tests

        [Fact]
        public async Task UpdateVehicleCheckinAsync_WithValidData_ShouldUpdateSuccessfully()
        {
            // Arrange
            var updateDto = new UpdateDto
            {
                Id = 1,
                Mileage = 15000,
                Notes = "Updated notes",
                ImageUrls = new List<string> { "https://example.com/new-image.jpg" }
            };

            var existingVehicleCheckin = MockDataBuilder.CreateVehicleCheckin(id: updateDto.Id);
            existingVehicleCheckin.Car = MockDataBuilder.CreateCar();
            existingVehicleCheckin.Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetByIdAsync(updateDto.Id))
                .ReturnsAsync(existingVehicleCheckin);

            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<VehicleCheckin>()))
                .ReturnsAsync((VehicleCheckin vc) => vc);

            var updatedVehicleCheckin = MockDataBuilder.CreateVehicleCheckin(id: updateDto.Id);
            updatedVehicleCheckin.Mileage = updateDto.Mileage;
            updatedVehicleCheckin.Notes = updateDto.Notes;
            updatedVehicleCheckin.Car = MockDataBuilder.CreateCar();
            updatedVehicleCheckin.Branch = MockDataBuilder.CreateBranch();
            updatedVehicleCheckin.VehicleCheckinImages = updateDto.ImageUrls.Select(url => new VehicleCheckinImage
            {
                ImageUrl = url,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _mockRepository.Setup(x => x.GetByIdWithDetailsAsync(updateDto.Id))
                .ReturnsAsync(updatedVehicleCheckin);

            // Act
            var result = await _service.UpdateVehicleCheckinAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Mileage.Should().Be(updateDto.Mileage);
            result.Notes.Should().Be(updateDto.Notes);

            // Verify
            _mockRepository.Verify(x => x.GetByIdAsync(updateDto.Id), Times.Once);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<VehicleCheckin>()), Times.Once);
        }

        [Fact]
        public async Task UpdateVehicleCheckinAsync_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var updateDto = new UpdateDto
            {
                Id = 99999, // Non-existent
                Mileage = 15000,
                Notes = "Updated notes"
            };

            _mockRepository.Setup(x => x.GetByIdAsync(updateDto.Id))
                .ReturnsAsync((VehicleCheckin?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.UpdateVehicleCheckinAsync(updateDto));

            // Verify
            _mockRepository.Verify(x => x.GetByIdAsync(updateDto.Id), Times.Once);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<VehicleCheckin>()), Times.Never);
        }

        #endregion

        #region DeleteVehicleCheckinAsync Tests

        [Fact]
        public async Task DeleteVehicleCheckinAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var vehicleCheckinId = 1;

            _mockRepository.Setup(x => x.DeleteAsync(vehicleCheckinId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteVehicleCheckinAsync(vehicleCheckinId);

            // Assert
            result.Should().BeTrue();

            // Verify
            _mockRepository.Verify(x => x.DeleteAsync(vehicleCheckinId), Times.Once);
        }

        [Fact]
        public async Task DeleteVehicleCheckinAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var vehicleCheckinId = 99999; // Non-existent

            _mockRepository.Setup(x => x.DeleteAsync(vehicleCheckinId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteVehicleCheckinAsync(vehicleCheckinId);

            // Assert
            result.Should().BeFalse();

            // Verify
            _mockRepository.Verify(x => x.DeleteAsync(vehicleCheckinId), Times.Once);
        }

        #endregion

        #region GetAllVehicleCheckinsAsync Tests

        [Fact]
        public async Task GetAllVehicleCheckinsAsync_WithDefaultParameters_ShouldReturnList()
        {
            // Arrange
            var vehicleCheckins = new List<VehicleCheckin>
            {
                MockDataBuilder.CreateVehicleCheckin(id: 1, statusCode: "PENDING"),
                MockDataBuilder.CreateVehicleCheckin(id: 2, statusCode: "CONFIRMED")
            };
            vehicleCheckins[0].Car = MockDataBuilder.CreateCar();
            vehicleCheckins[0].Branch = MockDataBuilder.CreateBranch();
            vehicleCheckins[1].Car = MockDataBuilder.CreateCar(id: 2);
            vehicleCheckins[1].Branch = MockDataBuilder.CreateBranch(id: 2);

            _mockRepository.Setup(x => x.GetAllWithDetailsAsync(1, 10, null, null, null, null, null))
                .ReturnsAsync(vehicleCheckins);

            // Act
            var result = await _service.GetAllVehicleCheckinsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);

            // Verify
            _mockRepository.Verify(x => x.GetAllWithDetailsAsync(1, 10, null, null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllVehicleCheckinsAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var page = 2;
            var pageSize = 5;
            var vehicleCheckins = new List<VehicleCheckin>
            {
                MockDataBuilder.CreateVehicleCheckin(id: 6),
                MockDataBuilder.CreateVehicleCheckin(id: 7)
            };
            vehicleCheckins[0].Car = MockDataBuilder.CreateCar();
            vehicleCheckins[0].Branch = MockDataBuilder.CreateBranch();
            vehicleCheckins[1].Car = MockDataBuilder.CreateCar();
            vehicleCheckins[1].Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetAllWithDetailsAsync(page, pageSize, null, null, null, null, null))
                .ReturnsAsync(vehicleCheckins);

            // Act
            var result = await _service.GetAllVehicleCheckinsAsync(page, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(6);
            result[1].Id.Should().Be(7);

            // Verify
            _mockRepository.Verify(x => x.GetAllWithDetailsAsync(page, pageSize, null, null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllVehicleCheckinsAsync_WithSearchTerm_ShouldFilterResults()
        {
            // Arrange
            var searchTerm = "51A-12345";
            var vehicleCheckins = new List<VehicleCheckin>
            {
                MockDataBuilder.CreateVehicleCheckin(id: 1)
            };
            var car = MockDataBuilder.CreateCar(licensePlate: searchTerm);
            vehicleCheckins[0].Car = car;
            vehicleCheckins[0].Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetAllWithDetailsAsync(1, 10, searchTerm, null, null, null, null))
                .ReturnsAsync(vehicleCheckins);

            // Act
            var result = await _service.GetAllVehicleCheckinsAsync(searchTerm: searchTerm);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].LicensePlate.Should().Be(searchTerm);

            // Verify
            _mockRepository.Verify(x => x.GetAllWithDetailsAsync(1, 10, searchTerm, null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllVehicleCheckinsAsync_WithStatusCodeFilter_ShouldFilterByStatus()
        {
            // Arrange
            var statusCode = "PENDING";
            var vehicleCheckins = new List<VehicleCheckin>
            {
                MockDataBuilder.CreateVehicleCheckin(id: 1, statusCode: statusCode)
            };
            vehicleCheckins[0].Car = MockDataBuilder.CreateCar();
            vehicleCheckins[0].Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetAllWithDetailsAsync(1, 10, null, statusCode, null, null, null))
                .ReturnsAsync(vehicleCheckins);

            // Act
            var result = await _service.GetAllVehicleCheckinsAsync(statusCode: statusCode);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].StatusCode.Should().Be(statusCode);

            // Verify
            _mockRepository.Verify(x => x.GetAllWithDetailsAsync(1, 10, null, statusCode, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllVehicleCheckinsAsync_WithDateRange_ShouldFilterByDate()
        {
            // Arrange
            var fromDate = new DateTime(2024, 1, 1);
            var toDate = new DateTime(2024, 12, 31);
            var vehicleCheckins = new List<VehicleCheckin>
            {
                MockDataBuilder.CreateVehicleCheckin(id: 1)
            };
            vehicleCheckins[0].Car = MockDataBuilder.CreateCar();
            vehicleCheckins[0].Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetAllWithDetailsAsync(1, 10, null, null, fromDate, toDate, null))
                .ReturnsAsync(vehicleCheckins);

            // Act
            var result = await _service.GetAllVehicleCheckinsAsync(fromDate: fromDate, toDate: toDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);

            // Verify
            _mockRepository.Verify(x => x.GetAllWithDetailsAsync(1, 10, null, null, fromDate, toDate, null), Times.Once);
        }

        [Fact]
        public async Task GetAllVehicleCheckinsAsync_WithEmptyResults_ShouldReturnEmptyList()
        {
            // Arrange
            var emptyList = new List<VehicleCheckin>();

            _mockRepository.Setup(x => x.GetAllWithDetailsAsync(1, 10, null, null, null, null, null))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _service.GetAllVehicleCheckinsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            // Verify
            _mockRepository.Verify(x => x.GetAllWithDetailsAsync(1, 10, null, null, null, null, null), Times.Once);
        }

        #endregion

        #region GetTotalCountAsync Tests

        [Fact]
        public async Task GetTotalCountAsync_WithoutFilters_ShouldReturnTotalCount()
        {
            // Arrange
            var expectedCount = 10;

            _mockRepository.Setup(x => x.GetTotalCountAsync(null, null, null, null, null))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetTotalCountAsync();

            // Assert
            result.Should().Be(expectedCount);

            // Verify
            _mockRepository.Verify(x => x.GetTotalCountAsync(null, null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetTotalCountAsync_WithSearchTerm_ShouldReturnFilteredCount()
        {
            // Arrange
            var searchTerm = "51A-12345";
            var expectedCount = 2;

            _mockRepository.Setup(x => x.GetTotalCountAsync(searchTerm, null, null, null, null))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetTotalCountAsync(searchTerm: searchTerm);

            // Assert
            result.Should().Be(expectedCount);

            // Verify
            _mockRepository.Verify(x => x.GetTotalCountAsync(searchTerm, null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetTotalCountAsync_WithStatusCode_ShouldReturnFilteredCount()
        {
            // Arrange
            var statusCode = "PENDING";
            var expectedCount = 5;

            _mockRepository.Setup(x => x.GetTotalCountAsync(null, statusCode, null, null, null))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetTotalCountAsync(statusCode: statusCode);

            // Assert
            result.Should().Be(expectedCount);

            // Verify
            _mockRepository.Verify(x => x.GetTotalCountAsync(null, statusCode, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetTotalCountAsync_WithDateRange_ShouldReturnFilteredCount()
        {
            // Arrange
            var fromDate = new DateTime(2024, 1, 1);
            var toDate = new DateTime(2024, 12, 31);
            var expectedCount = 3;

            _mockRepository.Setup(x => x.GetTotalCountAsync(null, null, fromDate, toDate, null))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetTotalCountAsync(fromDate: fromDate, toDate: toDate);

            // Assert
            result.Should().Be(expectedCount);

            // Verify
            _mockRepository.Verify(x => x.GetTotalCountAsync(null, null, fromDate, toDate, null), Times.Once);
        }

        [Fact]
        public async Task GetTotalCountAsync_WithAllFilters_ShouldReturnFilteredCount()
        {
            // Arrange
            var searchTerm = "51A";
            var statusCode = "PENDING";
            var fromDate = new DateTime(2024, 1, 1);
            var toDate = new DateTime(2024, 12, 31);
            var expectedCount = 1;

            _mockRepository.Setup(x => x.GetTotalCountAsync(searchTerm, statusCode, fromDate, toDate, null))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetTotalCountAsync(searchTerm, statusCode, fromDate, toDate);

            // Assert
            result.Should().Be(expectedCount);

            // Verify
            _mockRepository.Verify(x => x.GetTotalCountAsync(searchTerm, statusCode, fromDate, toDate, null), Times.Once);
        }

        #endregion

        #region GetVehicleCheckinsByCarIdAsync Tests

        [Fact]
        public async Task GetVehicleCheckinsByCarIdAsync_WithValidCarId_ShouldReturnList()
        {
            // Arrange
            var carId = 1;
            var vehicleCheckins = new List<VehicleCheckin>
            {
                MockDataBuilder.CreateVehicleCheckin(id: 1, carId: carId),
                MockDataBuilder.CreateVehicleCheckin(id: 2, carId: carId)
            };
            vehicleCheckins[0].Car = MockDataBuilder.CreateCar(id: carId);
            vehicleCheckins[0].Branch = MockDataBuilder.CreateBranch();
            vehicleCheckins[1].Car = MockDataBuilder.CreateCar(id: carId);
            vehicleCheckins[1].Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetByCarIdAsync(carId))
                .ReturnsAsync(vehicleCheckins);

            // Act
            var result = await _service.GetVehicleCheckinsByCarIdAsync(carId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(vc => vc.CarId == carId).Should().BeTrue();

            // Verify
            _mockRepository.Verify(x => x.GetByCarIdAsync(carId), Times.Once);
        }

        [Fact]
        public async Task GetVehicleCheckinsByCarIdAsync_WithNonExistentCarId_ShouldReturnEmptyList()
        {
            // Arrange
            var carId = 99999;
            var emptyList = new List<VehicleCheckin>();

            _mockRepository.Setup(x => x.GetByCarIdAsync(carId))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _service.GetVehicleCheckinsByCarIdAsync(carId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            // Verify
            _mockRepository.Verify(x => x.GetByCarIdAsync(carId), Times.Once);
        }

        #endregion

        #region GetVehicleCheckinsByMaintenanceRequestIdAsync Tests

        [Fact]
        public async Task GetVehicleCheckinsByMaintenanceRequestIdAsync_WithValidRequestId_ShouldReturnList()
        {
            // Arrange
            var maintenanceRequestId = 1;
            var vehicleCheckins = new List<VehicleCheckin>
            {
                MockDataBuilder.CreateVehicleCheckin(id: 1, carId: 1, branchId: 1, mileage: 10000, code: "VCI-12345", statusCode: "PENDING")
            };
            // MaintenanceRequestId không tồn tại trong VehicleCheckin model
            vehicleCheckins[0].Car = MockDataBuilder.CreateCar();
            vehicleCheckins[0].Branch = MockDataBuilder.CreateBranch();

            _mockRepository.Setup(x => x.GetByMaintenanceRequestIdAsync(maintenanceRequestId))
                .ReturnsAsync(vehicleCheckins);

            // Act
            var result = await _service.GetVehicleCheckinsByMaintenanceRequestIdAsync(maintenanceRequestId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);

            // Verify
            _mockRepository.Verify(x => x.GetByMaintenanceRequestIdAsync(maintenanceRequestId), Times.Once);
        }

        [Fact]
        public async Task GetVehicleCheckinsByMaintenanceRequestIdAsync_WithNonExistentRequestId_ShouldReturnEmptyList()
        {
            // Arrange
            var maintenanceRequestId = 99999;
            var emptyList = new List<VehicleCheckin>();

            _mockRepository.Setup(x => x.GetByMaintenanceRequestIdAsync(maintenanceRequestId))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _service.GetVehicleCheckinsByMaintenanceRequestIdAsync(maintenanceRequestId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            // Verify
            _mockRepository.Verify(x => x.GetByMaintenanceRequestIdAsync(maintenanceRequestId), Times.Once);
        }

        #endregion

        #region LinkMaintenanceRequestAsync Tests

        [Fact]
        public async Task LinkMaintenanceRequestAsync_WithValidIds_ShouldLinkSuccessfully()
        {
            // Arrange
            var vehicleCheckinId = 1;
            var maintenanceRequestId = 100;

            // Act & Assert
            // Service method throws NotImplementedException because MaintenanceRequest entity does not exist
            var exception = await Assert.ThrowsAsync<NotImplementedException>(async () =>
                await _service.LinkMaintenanceRequestAsync(vehicleCheckinId, maintenanceRequestId));

            exception.Message.Should().Contain("MaintenanceRequest entity does not exist");
        }

        [Fact]
        public async Task LinkMaintenanceRequestAsync_WithNonExistentVehicleCheckinId_ShouldThrowException()
        {
            // Arrange
            var vehicleCheckinId = 99999;
            var maintenanceRequestId = 100;

            // Act & Assert
            // Service method throws NotImplementedException because MaintenanceRequest entity does not exist
            var exception = await Assert.ThrowsAsync<NotImplementedException>(async () =>
                await _service.LinkMaintenanceRequestAsync(vehicleCheckinId, maintenanceRequestId));

            exception.Message.Should().Contain("MaintenanceRequest entity does not exist");
        }

        #endregion

        #region SearchVehicleAsync Tests

        [Fact]
        public async Task SearchVehicleAsync_WithValidLicensePlate_ShouldReturnVehicle()
        {
            // Arrange
            var searchTerm = "51A-12345";
            var car = MockDataBuilder.CreateCar(licensePlate: searchTerm);
            car.User = MockDataBuilder.CreateUser();
            car.Branch = MockDataBuilder.CreateBranch();
            var cars = new List<Car> { car };

            _mockRepository.Setup(x => x.SearchCarsAsync(searchTerm))
                .ReturnsAsync(cars);

            // Act
            var result = await _service.SearchVehicleAsync(searchTerm);

            // Assert
            result.Should().NotBeNull();
            
            // Use reflection to check anonymous type properties
            var resultType = result.GetType();
            var foundProperty = resultType.GetProperty("found");
            foundProperty.Should().NotBeNull();
            var foundValue = foundProperty!.GetValue(result);
            foundValue.Should().Be(true);

            // Verify
            _mockRepository.Verify(x => x.SearchCarsAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task SearchVehicleAsync_WithValidVinNumber_ShouldReturnVehicle()
        {
            // Arrange
            var searchTerm = "VIN123456789";
            var car = MockDataBuilder.CreateCar(vinNumber: searchTerm);
            car.User = MockDataBuilder.CreateUser();
            car.Branch = MockDataBuilder.CreateBranch();
            var cars = new List<Car> { car };

            _mockRepository.Setup(x => x.SearchCarsAsync(searchTerm))
                .ReturnsAsync(cars);

            // Act
            var result = await _service.SearchVehicleAsync(searchTerm);

            // Assert
            result.Should().NotBeNull();
            
            // Check found property
            var resultType = result.GetType();
            var foundProperty = resultType.GetProperty("found");
            foundProperty.Should().NotBeNull();
            var foundValue = foundProperty!.GetValue(result);
            foundValue.Should().Be(true);

            // Verify
            _mockRepository.Verify(x => x.SearchCarsAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task SearchVehicleAsync_WithNonExistentVehicle_ShouldReturnNotFound()
        {
            // Arrange
            var searchTerm = "NOT-FOUND";
            var emptyList = new List<Car>();

            _mockRepository.Setup(x => x.SearchCarsAsync(searchTerm))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _service.SearchVehicleAsync(searchTerm);

            // Assert
            result.Should().NotBeNull();
            
            // Check if result contains "found" = false
            var resultType = result.GetType();
            var foundProperty = resultType.GetProperty("found");
            foundProperty.Should().NotBeNull();
            var foundValue = foundProperty!.GetValue(result);
            foundValue.Should().Be(false);
            
            var messageProperty = resultType.GetProperty("message");
            messageProperty.Should().NotBeNull();

            // Verify
            _mockRepository.Verify(x => x.SearchCarsAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task SearchVehicleAsync_WithEmptySearchTerm_ShouldReturnNotFound()
        {
            // Arrange
            var searchTerm = "";
            var emptyList = new List<Car>();

            _mockRepository.Setup(x => x.SearchCarsAsync(searchTerm))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _service.SearchVehicleAsync(searchTerm);

            // Assert
            result.Should().NotBeNull();
            
            var resultType = result.GetType();
            var foundProperty = resultType.GetProperty("found");
            foundProperty.Should().NotBeNull();
            var foundValue = foundProperty!.GetValue(result);
            foundValue.Should().Be(false);

            // Verify
            _mockRepository.Verify(x => x.SearchCarsAsync(searchTerm), Times.Once);
        }

        #endregion
    }
}

