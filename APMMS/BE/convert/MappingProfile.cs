using AutoMapper;
using BE.models;
using BE.DTOs.Auth;
using BE.DTOs.AutoOwner;
using BE.DTOs.Component;
using BE.DTOs.Employee;
using BE.DTOs.Feedback;
using BE.DTOs.HistoryLog;
using BE.DTOs.MaintenanceTicket;
using BE.DTOs.Profile;
using BE.DTOs.Report;
using BE.DTOs.Role;
using BE.DTOs.ServicePackage;
using BE.DTOs.ServiceSchedule;
using BE.DTOs.ServiceTask;
using BE.DTOs.TotalReceipt;
using BE.DTOs.TypeComponent;
using BE.DTOs.VehicleCheckin;
using BE.DTOs.Branch;
using BE.DTOs.StockInRequest;
using BE.DTOs.StockIn;
using System.Linq;

namespace BE.convert
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Auth mappings removed - using database entities directly

            // AutoOwner mappings
            CreateMap<User, BE.DTOs.AutoOwner.ResponseDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : null))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null))
                
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone));

            CreateMap<BE.DTOs.AutoOwner.RequestDto, User>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Password, opt => opt.Ignore()); // Password được xử lý riêng trong service để tránh bị mất

            // Component mappings
            CreateMap<BE.DTOs.Component.RequestDto, Component>()
                .ForMember(dest => dest.Id, opt => opt.Condition(src => src.Id.HasValue))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? 0L));

            CreateMap<Component, BE.DTOs.Component.ResponseDto>()
                .ForMember(dest => dest.TypeComponentName, opt => opt.Ignore())
                .ForMember(dest => dest.BranchName, opt => opt.Ignore());


            // Employee mappings
            CreateMap<User, BE.DTOs.Employee.EmployeeResponseDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : null))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.BranchId))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null))
                .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Dob, opt => opt.MapFrom(src => src.Dob.HasValue ? new DateTime(src.Dob.Value.Year, src.Dob.Value.Month, src.Dob.Value.Day) : (DateTime?)null));
            CreateMap<BE.DTOs.Employee.EmployeeRequestDto, User>()
                .ForMember(dest => dest.Dob, opt => opt.MapFrom(src => ParseDobString(src.Dob)));

            // Feedback mappings

            CreateMap<Feedback, BE.DTOs.Feedback.ResponseDto>()
     .ForMember(dest => dest.UserName,
        opt => opt.MapFrom(src =>
            src.User != null
                ? $"{src.User.FirstName} {src.User.LastName}".Trim()
                : null))
    .ForMember(dest => dest.Image,
        opt => opt.MapFrom(src => src.User != null ? src.User.Image : null));

            CreateMap<BE.DTOs.Feedback.RequestDto, Feedback>();

            // HistoryLog mappings
            CreateMap<HistoryLog, BE.DTOs.HistoryLog.ResponseDto>();
            CreateMap<BE.DTOs.HistoryLog.RequestDto, HistoryLog>();

            // MaintenanceTicket mappings
            CreateMap<MaintenanceTicket, BE.DTOs.MaintenanceTicket.ResponseDto>()
                // Basic fields
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ServiceCategoryId, opt => opt.MapFrom(src => src.ServiceCategoryId))
                .ForMember(dest => dest.ServiceCategoryName, opt => opt.MapFrom(src => src.ServiceCategory != null ? src.ServiceCategory.Name : null))
                .ForMember(dest => dest.CarName, opt => opt.MapFrom(src => src.SnapshotCarName ?? (src.Car != null ? src.Car.CarName : null)))
                .ForMember(dest => dest.ConsulterName, opt => opt.MapFrom(src => src.SnapshotConsulterName ?? (src.Consulter != null ? ($"{src.Consulter.FirstName} {src.Consulter.LastName}").Trim() : null)))
                .ForMember(dest => dest.TechnicianName, opt => opt.MapFrom(src => src.Technician != null ? ($"{src.Technician.FirstName} {src.Technician.LastName}").Trim() : null))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.SnapshotBranchName ?? (src.Branch != null ? src.Branch.Name : null)))
                .ForMember(dest => dest.BranchAddress, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Address : null))
                .ForMember(dest => dest.ScheduleServiceName, opt => opt.MapFrom(src => src.ScheduleService != null ? src.ScheduleService.ScheduledDate.ToString("dd/MM/yyyy") : null))
                // Customer info from car owner
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.SnapshotCustomerName ?? (src.Car != null && src.Car.User != null ? ($"{src.Car.User.FirstName} {src.Car.User.LastName}").Trim() : null)))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.SnapshotCustomerPhone ?? (src.Car != null && src.Car.User != null ? src.Car.User.Phone : null)))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.SnapshotCustomerEmail ?? (src.Car != null && src.Car.User != null ? src.Car.User.Email : null)))
                .ForMember(dest => dest.CustomerAddress, opt => opt.MapFrom(src => src.SnapshotCustomerAddress ?? (src.Car != null && src.Car.User != null && !string.IsNullOrWhiteSpace(src.Car.User.Address) ? src.Car.User.Address : null)))
                // Vehicle info
                .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.SnapshotLicensePlate ?? (src.Car != null ? src.Car.LicensePlate : null)))
                .ForMember(dest => dest.CarModel, opt => opt.MapFrom(src => src.SnapshotCarModel ?? (src.Car != null ? src.Car.CarModel : null)))
                .ForMember(dest => dest.VinNumber, opt => opt.MapFrom(src => src.SnapshotVinNumber ?? (src.Car != null ? src.Car.VinNumber : null)))
                .ForMember(dest => dest.VehicleEngineNumber, opt => opt.MapFrom(src => src.SnapshotEngineNumber ?? (src.Car != null ? src.Car.VehicleEngineNumber : null)))
                .ForMember(dest => dest.YearOfManufacture, opt => opt.MapFrom(src => src.SnapshotYearOfManufacture ?? (src.Car != null ? src.Car.YearOfManufacture : null)))
                .ForMember(dest => dest.VehicleType, opt => opt.MapFrom(src => src.SnapshotVehicleType ?? (src.Car != null && src.Car.VehicleType != null ? src.Car.VehicleType.Name : null)))
                .ForMember(dest => dest.VehicleTypeId, opt => opt.MapFrom(src => src.SnapshotVehicleTypeId ?? (src.Car != null ? src.Car.VehicleTypeId : null)))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.SnapshotColor ?? (src.Car != null ? src.Car.Color : null)))
                // Vehicle checkin info
                .ForMember(dest => dest.Mileage, opt => opt.MapFrom(src => src.SnapshotMileage ?? (src.VehicleCheckin != null ? src.VehicleCheckin.Mileage : null)))
                .ForMember(dest => dest.CheckinNotes, opt => opt.MapFrom(src => src.VehicleCheckin != null ? src.VehicleCheckin.Notes : null))
                .ForMember(dest => dest.CheckinImages, opt => opt.MapFrom(src => src.VehicleCheckin != null && src.VehicleCheckin.VehicleCheckinImages != null ? src.VehicleCheckin.VehicleCheckinImages.Select(i => i.ImageUrl).ToList() : new List<string>()))
                // Service Package info
                .ForMember(dest => dest.ServicePackageId, opt => opt.MapFrom(src => src.ServicePackageId))
                .ForMember(dest => dest.ServicePackageName, opt => opt.MapFrom(src => src.ServicePackage != null ? src.ServicePackage.Name : null))
                .ForMember(dest => dest.ServicePackagePrice, opt => opt.MapFrom(src => src.ServicePackagePrice));
            CreateMap<MaintenanceTicket, BE.DTOs.MaintenanceTicket.ListResponseDto>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.CarName, opt => opt.MapFrom(src => src.SnapshotCarName ?? (src.Car != null ? src.Car.CarName : null)))
                .ForMember(dest => dest.ConsulterName, opt => opt.MapFrom(src => src.SnapshotConsulterName ?? (src.Consulter != null ? $"{src.Consulter.FirstName} {src.Consulter.LastName}".Trim() : null)))
                .ForMember(dest => dest.TechnicianName, opt => opt.MapFrom(src => src.Technician != null ? $"{src.Technician.FirstName} {src.Technician.LastName}".Trim() : null))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.SnapshotBranchName ?? (src.Branch != null ? src.Branch.Name : null)))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.SnapshotCustomerName ?? (src.Car != null && src.Car.User != null ? $"{src.Car.User.FirstName} {src.Car.User.LastName}".Trim() : null)))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code));
            CreateMap<BE.DTOs.MaintenanceTicket.RequestDto, MaintenanceTicket>();

            // Profile mappings
            CreateMap<User, BE.DTOs.Profile.ResponseDto>();
            CreateMap<BE.DTOs.Profile.RequestDto, User>();

            // Report mappings
            CreateMap<TotalReceipt, BE.DTOs.Report.ResponseDto>();
            CreateMap<BE.DTOs.Report.RequestDto, TotalReceipt>();

            // Role mappings
            CreateMap<Role, BE.DTOs.Role.ResponseDto>();
            CreateMap<BE.DTOs.Role.RequestDto, Role>();

            // ServicePackage mappings
            CreateMap<BE.DTOs.ServicePackage.RequestDto, ServicePackage>()
             .ForMember(dest => dest.Id, opt => opt.Condition(src => src.Id.HasValue))
             .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? 0L))
             .ForMember(dest => dest.ComponentPackages, opt => opt.Ignore()); // handle manually in service

            CreateMap<ServicePackage, BE.DTOs.ServicePackage.ResponseDto>()
                .ForMember(dest => dest.Components, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.BranchName, opt => opt.Ignore()); // fill manually

            // ServiceSchedule mappings
            CreateMap<ScheduleService, BE.DTOs.ServiceSchedule.ResponseDto>();
            CreateMap<BE.DTOs.ServiceSchedule.RequestDto, ScheduleService>();

            // ServiceTask mappings
            CreateMap<ServiceTask, BE.DTOs.ServiceTask.ServiceTaskResponseDto>();
            CreateMap<ServiceTask, BE.DTOs.ServiceTask.ServiceTaskListResponseDto>();
            CreateMap<BE.DTOs.ServiceTask.ServiceTaskRequestDto, ServiceTask>();
            CreateMap<BE.DTOs.ServiceTask.ServiceTaskUpdateDto, ServiceTask>();

            // TotalReceipt mappings
            CreateMap<TotalReceipt, BE.DTOs.TotalReceipt.ResponseDto>();
            CreateMap<BE.DTOs.TotalReceipt.RequestDto, TotalReceipt>();
            
            // TypeComponent mappings
            CreateMap<BE.DTOs.TypeComponent.RequestDto, TypeComponent>()
            .ForMember(dest => dest.Id, opt => opt.Condition(src => src.Id.HasValue))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? 0L));

            CreateMap<TypeComponent, BE.DTOs.TypeComponent.ResponseDto>()
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null));

            // VehicleCheckin mappings
            CreateMap<VehicleCheckin, BE.DTOs.VehicleCheckin.ResponseDto>();
            CreateMap<BE.DTOs.VehicleCheckin.VehicleCheckinRequestDto, VehicleCheckin>();

            // CarOfOutoOwner mappings
            CreateMap<Car, BE.DTOs.CarOfAutoOwner.ResponseDto>()
                .ForMember(dest => dest.VehicleTypeName, opt => opt.MapFrom(src => src.VehicleType != null ? src.VehicleType.Name : null));
            CreateMap<BE.DTOs.CarOfAutoOwner.RequestDto, Car>();

            // Branch mappings
            CreateMap<Branch, BranchResponseDto>();
            CreateMap<BranchRequestDto, Branch>();

            // StockInRequest mappings
            CreateMap<StockInRequestRequestDto, StockInRequest>()
                .ForMember(dest => dest.Id, opt => opt.Condition(src => src.StockInRequestId.HasValue))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.StockInRequestId ?? 0L))
                .ForMember(dest => dest.StockInRequestDetails, opt => opt.Ignore()); // handle manually in service

            CreateMap<StockInRequest, StockInRequestResponseDto>()
                .ForMember(dest => dest.StockInRequestId, opt => opt.MapFrom(src => src.Id)) // Map Id to StockInRequestId
                .ForMember(dest => dest.Details, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.BranchName, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.StatusName, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.LastModifiedByName, opt => opt.Ignore()); // fill manually

            // StockIn mappings
            CreateMap<StockInRequestDto, StockIn>()
                .ForMember(dest => dest.Id, opt => opt.Condition(src => src.Id.HasValue))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? 0L))
                .ForMember(dest => dest.StockInDetails, opt => opt.Ignore()); // handle manually in service

            CreateMap<StockIn, StockInResponseDto>()
                .ForMember(dest => dest.Details, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.StockInRequestCode, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.StatusName, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.ApprovedByName, opt => opt.Ignore()) // fill manually
                .ForMember(dest => dest.LastModifiedByName, opt => opt.Ignore()); // fill manually

        }

        private static DateOnly? ParseDobString(string? dobString)
        {
            if (string.IsNullOrWhiteSpace(dobString))
                return null;
            
            // Parse dd-MM-yyyy format
            if (DateOnly.TryParseExact(dobString, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateOnly date))
                return date;
            
            // Fallback: try standard formats
            if (DateTime.TryParse(dobString, out DateTime dt))
                return DateOnly.FromDateTime(dt);
            
            return null;
        }
    }
}