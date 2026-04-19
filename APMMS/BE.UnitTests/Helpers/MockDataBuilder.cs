using BE.vn.fpt.edu.models;
using BE.vn.fpt.edu.DTOs.VehicleCheckin;
using BE.vn.fpt.edu.DTOs.Employee;

namespace BE.UnitTests.Helpers
{
    /// <summary>
    /// Helper class để tạo mock data cho Unit Tests
    /// </summary>
    public static class MockDataBuilder
    {
        /// <summary>
        /// Tạo mock VehicleCheckinRequestDto
        /// </summary>
        public static VehicleCheckinRequestDto CreateVehicleCheckinRequestDto(
            long carId = 1,
            long branchId = 1,
            int mileage = 10000,
            string? notes = "Test notes",
            List<string>? imageUrls = null)
        {
            return new VehicleCheckinRequestDto
            {
                CarId = carId,
                BranchId = branchId,
                Mileage = mileage,
                Notes = notes,
                ImageUrls = imageUrls ?? new List<string> { "https://example.com/image1.jpg" },
                MaintenanceRequestId = null
            };
        }

        /// <summary>
        /// Tạo mock VehicleCheckin entity
        /// </summary>
        public static VehicleCheckin CreateVehicleCheckin(
            long id = 1,
            long carId = 1,
            long branchId = 1,
            int mileage = 10000,
            string? code = "VCI-12345",
            string statusCode = "PENDING")
        {
            return new VehicleCheckin
            {
                Id = id,
                CarId = carId,
                BranchId = branchId,
                Mileage = mileage,
                Code = code,
                StatusCode = statusCode,
                Notes = "Test notes",
                CreatedAt = DateTime.UtcNow,
                VehicleCheckinImages = new List<VehicleCheckinImage>
                {
                    new VehicleCheckinImage
                    {
                        Id = 1,
                        VehicleCheckinId = id,
                        ImageUrl = "https://example.com/image1.jpg",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };
        }

        /// <summary>
        /// Tạo mock Car entity
        /// </summary>
        public static Car CreateCar(
            long id = 1,
            string carName = "Test Car",
            string licensePlate = "51A-12345",
            string vinNumber = "VIN123456789",
            long userId = 1,
            long branchId = 1)
        {
            return new Car
            {
                Id = id,
                CarName = carName,
                LicensePlate = licensePlate,
                VinNumber = vinNumber,
                CarModel = "Toyota Camry",
                YearOfManufacture = 2020,
                Color = "White",
                UserId = userId,
                BranchId = branchId,
                User = new User
                {
                    Id = userId,
                    Username = "testuser",
                    Password = "TestPassword123",
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test@test.com",
                    Phone = "0987654321"
                },
                Branch = new Branch
                {
                    Id = branchId,
                    Name = "Test Branch",
                    Address = "123 Test Street",
                    Phone = "0123456789"
                }
            };
        }

        /// <summary>
        /// Tạo mock Branch entity
        /// </summary>
        public static Branch CreateBranch(long id = 1, string name = "Test Branch", string? address = "123 Test Street")
        {
            return new Branch
            {
                Id = id,
                Name = name,
                Address = address,
                Phone = "0123456789"
            };
        }

        /// <summary>
        /// Tạo mock User entity
        /// </summary>
        public static User CreateUser(
            long id = 1,
            string firstName = "Test",
            string lastName = "User",
            string email = "test@test.com",
            string phone = "0987654321",
            string username = "testuser",
            string password = "TestPassword123")
        {
            return new User
            {
                Id = id,
                Username = username,
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone
            };
        }

        /// <summary>
        /// Tạo mock Employee (User với RoleId employee)
        /// </summary>
        public static User CreateEmployee(
            long id = 1,
            string username = "employee01",
            string password = "Password123",
            string firstName = "Nguyen",
            string lastName = "Van A",
            string email = "employee@test.com",
            string phone = "0987654321",
            long? roleId = 3,
            long? branchId = 1,
            string? code = "TC00001",
            string? citizenId = "123456789012",
            string? taxCode = "1234567890",
            string? address = "123 Test Street",
            string? statusCode = "ACTIVE",
            DateOnly? dob = null,
            string? gender = "Nam")
        {
            return new User
            {
                Id = id,
                Code = code,
                Username = username,
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                Gender = gender,
                RoleId = roleId,
                BranchId = branchId,
                CitizenId = citizenId,
                TaxCode = taxCode,
                Address = address,
                StatusCode = statusCode ?? "ACTIVE",
                Dob = dob ?? new DateOnly(1990, 1, 1),
                IsDelete = false,
                CreatedDate = DateTime.UtcNow,
                Role = roleId.HasValue ? new Role { Id = roleId.Value, Name = GetRoleName(roleId.Value) } : null,
                Branch = branchId.HasValue ? new Branch { Id = branchId.Value, Name = "Test Branch" } : null
            };
        }

        /// <summary>
        /// Tạo mock EmployeeRequestDto
        /// </summary>
        public static EmployeeRequestDto CreateEmployeeRequestDto(
            string username = "employee01",
            string password = "Password123",
            string firstName = "Nguyen",
            string lastName = "Van A",
            string email = "employee@test.com",
            string phone = "0987654321",
            long? roleId = 3,
            long? branchId = 1,
            string? citizenId = "123456789012",
            string? taxCode = "1234567890",
            string? address = "123 Test Street",
            string? statusCode = "ACTIVE",
            string? dob = "01-01-1990",
            string? gender = "Nam")
        {
            return new EmployeeRequestDto
            {
                Username = username,
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                Gender = gender,
                RoleId = roleId,
                BranchId = branchId,
                CitizenId = citizenId,
                TaxCode = taxCode,
                Address = address,
                StatusCode = statusCode,
                Dob = dob
            };
        }

        /// <summary>
        /// Tạo mock Role entity
        /// </summary>
        public static Role CreateRole(long id = 3, string name = "Technician")
        {
            return new Role
            {
                Id = id,
                Name = name
            };
        }

        /// <summary>
        /// Get role name by role ID
        /// </summary>
        private static string GetRoleName(long roleId)
        {
            return roleId switch
            {
                3 => "Accountant",
                4 => "Technician",
                5 => "Warehouse Keeper",
                6 => "Consulter",
                _ => "Employee"
            };
        }
    }
}

