using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BE.models;

public partial class CarMaintenanceDbContext : DbContext
{
    public CarMaintenanceDbContext()
    {
    }

    public CarMaintenanceDbContext(DbContextOptions<CarMaintenanceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<Component> Components { get; set; }

    public virtual DbSet<ComponentPackage> ComponentPackages { get; set; }

    public virtual DbSet<CustomerGuest> CustomerGuests { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<HistoryLog> HistoryLogs { get; set; }

    public virtual DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }

    public virtual DbSet<MaintenanceTicketTechnician> MaintenanceTicketTechnicians { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ScheduleService> ScheduleServices { get; set; }

    public virtual DbSet<ScheduleServiceNote> ScheduleServiceNotes { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServicePackage> ServicePackages { get; set; }

    public virtual DbSet<ServicePackageCategory> ServicePackageCategories { get; set; }

    public virtual DbSet<ServiceTask> ServiceTasks { get; set; }

    public virtual DbSet<ServiceTaskTechnician> ServiceTaskTechnicians { get; set; }

    public virtual DbSet<StatusLookup> StatusLookups { get; set; }

    public virtual DbSet<StockIn> StockIns { get; set; }

    public virtual DbSet<StockInDetail> StockInDetails { get; set; }

    public virtual DbSet<StockInRequest> StockInRequests { get; set; }

    public virtual DbSet<StockInRequestDetail> StockInRequestDetails { get; set; }

    public virtual DbSet<TicketComponent> TicketComponents { get; set; }

    public virtual DbSet<TotalReceipt> TotalReceipts { get; set; }

    public virtual DbSet<TypeComponent> TypeComponents { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VehicleCheckin> VehicleCheckins { get; set; }

    public virtual DbSet<VehicleCheckinImage> VehicleCheckinImages { get; set; }

    public virtual DbSet<VehicleType> VehicleTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Không để trống thì giữ nguyên, nhưng KHÔNG hard-code connection string
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__branch__3213E83F3F8BFA15");

            entity.ToTable("branch");

            entity.HasIndex(e => e.Name, "UQ__branch__72E12F1BE49CD06A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.LaborRate)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("labor_rate");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__car__3213E83FBDC59A40");

            entity.ToTable("car");

            entity.HasIndex(e => e.LicensePlate, "UQ__car__F72CD56E212CBB61").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CarModel)
                .HasMaxLength(100)
                .HasColumnName("car_model");
            entity.Property(e => e.CarName)
                .HasMaxLength(100)
                .HasColumnName("car_name");
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .HasColumnName("color");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
            entity.Property(e => e.LastModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("last_modified_date");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(20)
                .HasColumnName("license_plate");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VehicleEngineNumber)
                .HasMaxLength(100)
                .HasColumnName("vehicle_engine_number");
            entity.Property(e => e.VehicleTypeId).HasColumnName("vehicle_type_id");
            entity.Property(e => e.VinNumber)
                .HasMaxLength(100)
                .HasColumnName("vin_number");
            entity.Property(e => e.YearOfManufacture).HasColumnName("year_of_manufacture");

            entity.HasOne(d => d.Branch).WithMany(p => p.Cars)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__car__branch_id__6A30C649");

            entity.HasOne(d => d.User).WithMany(p => p.Cars)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__car__user_id__6B24EA82");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.Cars)
                .HasForeignKey(d => d.VehicleTypeId)
                .HasConstraintName("FK__car__vehicle_typ__6C190EBB");
        });

        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__componen__3213E83F3DD56015");

            entity.ToTable("component");

            entity.HasIndex(e => new { e.Code, e.BranchId }, "UQ_component_code_branch").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.MinimumQuantity).HasColumnName("minimum_quantity");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PurchasePrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("purchase_price");
            entity.Property(e => e.QuantityStock)
                .HasDefaultValue(0)
                .HasColumnName("quantity_stock");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");
            entity.Property(e => e.TypeComponentId).HasColumnName("type_component_id");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Branch).WithMany(p => p.Components)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__component__branc__6D0D32F4");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.Components)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_component_status");

            entity.HasOne(d => d.TypeComponent).WithMany(p => p.Components)
                .HasForeignKey(d => d.TypeComponentId)
                .HasConstraintName("FK__component__type___6E01572D");
        });

        modelBuilder.Entity<ComponentPackage>(entity =>
        {
            entity.HasKey(e => new { e.ComponentId, e.ServicePackageId }).HasName("PK__componen__C7D9836BCB3A01AF");

            entity.ToTable("component_package");

            entity.Property(e => e.ComponentId).HasColumnName("component_id");
            entity.Property(e => e.ServicePackageId).HasColumnName("service_package_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Component).WithMany(p => p.ComponentPackages)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__component__compo__6EF57B66");

            entity.HasOne(d => d.ServicePackage).WithMany(p => p.ComponentPackages)
                .HasForeignKey(d => d.ServicePackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__component__servi__6FE99F9F");
        });

        modelBuilder.Entity<CustomerGuest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__customer__3213E83FC5888854");

            entity.ToTable("customer_guest");

            entity.HasIndex(e => e.BranchId, "IX_customer_guest_branch_id");

            entity.HasIndex(e => e.CreatedAt, "IX_customer_guest_created_at");

            entity.HasIndex(e => e.LicensePlate, "IX_customer_guest_license_plate");

            entity.HasIndex(e => e.LinkedUserId, "IX_customer_guest_linked_user_id");

            entity.HasIndex(e => e.Phone, "IX_customer_guest_phone");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CarModel)
                .HasMaxLength(100)
                .HasColumnName("car_model");
            entity.Property(e => e.CarName)
                .HasMaxLength(100)
                .HasColumnName("car_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(20)
                .HasColumnName("license_plate");
            entity.Property(e => e.LinkedUserId).HasColumnName("linked_user_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");

            entity.HasOne(d => d.Branch).WithMany(p => p.CustomerGuests)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_guest_branch");

            entity.HasOne(d => d.LinkedUser).WithMany(p => p.CustomerGuests)
                .HasForeignKey(d => d.LinkedUserId)
                .HasConstraintName("FK_guest_linked_user");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__feedback__3213E83FCAEDCE67");

            entity.ToTable("feedback");

            entity.HasIndex(e => e.UserId, "IX_feedback_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comment)
                .HasMaxLength(255)
                .HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.MaintenanceTicketId).HasColumnName("maintenance_ticket_id");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.MaintenanceTicket).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.MaintenanceTicketId)
                .HasConstraintName("FK__feedback__mainte__70DDC3D8");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__feedback__parent__71D1E811");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__feedback__user_i__72C60C4A");
        });

        modelBuilder.Entity<HistoryLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__history___3213E83FEEA81A96");

            entity.ToTable("history_log");

            entity.HasIndex(e => e.MaintenanceTicketId, "IX_history_log_maintenance_ticket_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.MaintenanceTicketId).HasColumnName("maintenance_ticket_id");
            entity.Property(e => e.NewData).HasColumnName("new_data");
            entity.Property(e => e.OldData).HasColumnName("old_data");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.MaintenanceTicket).WithMany(p => p.HistoryLogs)
                .HasForeignKey(d => d.MaintenanceTicketId)
                .HasConstraintName("FK_history_log_maintenance_ticket");

            entity.HasOne(d => d.User).WithMany(p => p.HistoryLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__history_l__user___73BA3083");
        });

        modelBuilder.Entity<MaintenanceTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__maintena__3213E83F1B33439E");

            entity.ToTable("maintenance_ticket");

            entity.HasIndex(e => e.CarId, "IX_maintenance_ticket_car_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.ConsulterId).HasColumnName("consulter_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.PriorityLevel)
                .HasMaxLength(20)
                .HasColumnName("priority_level");
            entity.Property(e => e.ScheduleServiceId).HasColumnName("schedule_service_id");
            entity.Property(e => e.ServiceCategoryId).HasColumnName("service_category_id");
            entity.Property(e => e.ServicePackageId).HasColumnName("service_package_id");
            entity.Property(e => e.ServicePackagePrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("service_package_price");
            entity.Property(e => e.SnapshotBranchName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_branch_name");
            entity.Property(e => e.SnapshotCarModel)
                .HasMaxLength(255)
                .HasColumnName("snapshot_car_model");
            entity.Property(e => e.SnapshotCarName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_car_name");
            entity.Property(e => e.SnapshotColor)
                .HasMaxLength(50)
                .HasColumnName("snapshot_color");
            entity.Property(e => e.SnapshotConsulterName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_consulter_name");
            entity.Property(e => e.SnapshotCustomerAddress)
                .HasMaxLength(255)
                .HasColumnName("snapshot_customer_address");
            entity.Property(e => e.SnapshotCustomerEmail)
                .HasMaxLength(255)
                .HasColumnName("snapshot_customer_email");
            entity.Property(e => e.SnapshotCustomerName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_customer_name");
            entity.Property(e => e.SnapshotCustomerPhone)
                .HasMaxLength(50)
                .HasColumnName("snapshot_customer_phone");
            entity.Property(e => e.SnapshotEngineNumber)
                .HasMaxLength(50)
                .HasColumnName("snapshot_engine_number");
            entity.Property(e => e.SnapshotLicensePlate)
                .HasMaxLength(50)
                .HasColumnName("snapshot_license_plate");
            entity.Property(e => e.SnapshotMileage).HasColumnName("snapshot_mileage");
            entity.Property(e => e.SnapshotVehicleType)
                .HasMaxLength(100)
                .HasColumnName("snapshot_vehicle_type");
            entity.Property(e => e.SnapshotVehicleTypeId).HasColumnName("snapshot_vehicle_type_id");
            entity.Property(e => e.SnapshotVinNumber)
                .HasMaxLength(50)
                .HasColumnName("snapshot_vin_number");
            entity.Property(e => e.SnapshotYearOfManufacture).HasColumnName("snapshot_year_of_manufacture");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");
            entity.Property(e => e.TechnicianId).HasColumnName("technician_id");
            entity.Property(e => e.TotalEstimatedCost)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_estimated_cost");
            entity.Property(e => e.VehicleCheckinId).HasColumnName("vehicle_checkin_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__maintenan__branc__787EE5A0");

            entity.HasOne(d => d.Car).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("FK__maintenan__car_i__797309D9");

            entity.HasOne(d => d.Consulter).WithMany(p => p.MaintenanceTicketConsulters)
                .HasForeignKey(d => d.ConsulterId)
                .HasConstraintName("FK__maintenan__consu__7A672E12");

            entity.HasOne(d => d.ScheduleService).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.ScheduleServiceId)
                .HasConstraintName("FK__maintenan__sched__7B5B524B");

            entity.HasOne(d => d.ServiceCategory).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.ServiceCategoryId)
                .HasConstraintName("FK_ticket_category");

            entity.HasOne(d => d.ServicePackage).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.ServicePackageId)
                .HasConstraintName("FK_maintenance_ticket_service_package");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_maintenance_ticket_status");

            entity.HasOne(d => d.Technician).WithMany(p => p.MaintenanceTicketTechnicians)
                .HasForeignKey(d => d.TechnicianId)
                .HasConstraintName("FK__maintenan__techn__7C4F7684");

            entity.HasOne(d => d.VehicleCheckin).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.VehicleCheckinId)
                .HasConstraintName("FK_maintenance_ticket_vehicle_checkin");
        });

        modelBuilder.Entity<MaintenanceTicketTechnician>(entity =>
        {
            entity.HasKey(e => new { e.MaintenanceTicketId, e.TechnicianId }).HasName("PK__maintena__6AF1CD718CA4D122");

            entity.ToTable("maintenance_ticket_technician");

            entity.Property(e => e.MaintenanceTicketId).HasColumnName("maintenance_ticket_id");
            entity.Property(e => e.TechnicianId).HasColumnName("technician_id");
            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("assigned_date");
            entity.Property(e => e.RoleInTicket)
                .HasMaxLength(100)
                .HasColumnName("role_in_ticket");

            entity.HasOne(d => d.MaintenanceTicket).WithMany(p => p.MaintenanceTicketTechnicians)
                .HasForeignKey(d => d.MaintenanceTicketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__maintenan__maint__7F2BE32F");

            entity.HasOne(d => d.Technician).WithMany(p => p.MaintenanceTicketTechniciansNavigation)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__maintenan__techn__00200768");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__role__3213E83F74558E4F");

            entity.ToTable("role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ScheduleService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__schedule__3213E83F16CBCA4D");

            entity.ToTable("schedule_service");

            entity.HasIndex(e => e.CreatedAt, "IX_schedule_service_created_at");

            entity.HasIndex(e => e.GuestId, "IX_schedule_service_guest_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.GuestId).HasColumnName("guest_id");
            entity.Property(e => e.ScheduledDate)
                .HasColumnType("datetime")
                .HasColumnName("scheduled_date");
            entity.Property(e => e.ServiceCategoryId).HasColumnName("service_category_id");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.ScheduleServices)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__schedule___branc__02FC7413");

            entity.HasOne(d => d.Car).WithMany(p => p.ScheduleServices)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("FK__schedule___car_i__03F0984C");

            entity.HasOne(d => d.Guest).WithMany(p => p.ScheduleServices)
                .HasForeignKey(d => d.GuestId)
                .HasConstraintName("FK_schedule_guest");

            entity.HasOne(d => d.ServiceCategory).WithMany(p => p.ScheduleServices)
                .HasForeignKey(d => d.ServiceCategoryId)
                .HasConstraintName("FK_schedule_service_category");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.ScheduleServices)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_schedule_service_status");

            entity.HasOne(d => d.User).WithMany(p => p.ScheduleServices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__schedule___user___04E4BC85");
        });

        modelBuilder.Entity<ScheduleServiceNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__schedule__3213E83FAAD58A85");

            entity.ToTable("schedule_service_note");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConsultantId).HasColumnName("consultant_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.ScheduleServiceId).HasColumnName("schedule_service_id");

            entity.HasOne(d => d.Consultant).WithMany(p => p.ScheduleServiceNotes)
                .HasForeignKey(d => d.ConsultantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_note_user");

            entity.HasOne(d => d.ScheduleService).WithMany(p => p.ScheduleServiceNotes)
                .HasForeignKey(d => d.ScheduleServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_note_schedule");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.ToTable("service_category");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.StandardLaborTime)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("standard_labor_time");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.ServiceCategories)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_service_category_status");
        });

        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__service___3213E83FF129E971");

            entity.ToTable("service_package");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");

            entity.HasOne(d => d.Branch).WithMany(p => p.ServicePackages)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_service_package_branch");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.ServicePackages)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_service_package_status");
        });

        modelBuilder.Entity<ServicePackageCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__service___3213E83F076EF3A5");

            entity.ToTable("service_package_category");

            entity.HasIndex(e => e.ServiceCategoryId, "IX_service_package_category_category");

            entity.HasIndex(e => e.ServicePackageId, "IX_service_package_category_package");

            entity.HasIndex(e => new { e.ServicePackageId, e.ServiceCategoryId }, "UQ_service_package_category").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ServiceCategoryId).HasColumnName("service_category_id");
            entity.Property(e => e.ServicePackageId).HasColumnName("service_package_id");
            entity.Property(e => e.StandardLaborTime)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("standard_labor_time");

            entity.HasOne(d => d.ServiceCategory).WithMany(p => p.ServicePackageCategories)
                .HasForeignKey(d => d.ServiceCategoryId)
                .HasConstraintName("FK_service_package_category_category");

            entity.HasOne(d => d.ServicePackage).WithMany(p => p.ServicePackageCategories)
                .HasForeignKey(d => d.ServicePackageId)
                .HasConstraintName("FK_service_package_category_package");
        });

        modelBuilder.Entity<ServiceTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__service___3213E83F93ED7CE8");

            entity.ToTable("service_task");

            entity.HasIndex(e => e.DisplayOrder, "IX_service_task_display_order");

            entity.HasIndex(e => e.TechnicianId, "IX_service_task_technician_id");

            entity.HasIndex(e => e.MaintenanceTicketId, "IX_service_task_ticket_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualLaborTime)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("actual_labor_time");
            entity.Property(e => e.CompletionNote).HasColumnName("completion_note");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.LaborCost)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("labor_cost");
            entity.Property(e => e.MaintenanceTicketId).HasColumnName("maintenance_ticket_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.ServiceCategoryId).HasColumnName("service_category_id");
            entity.Property(e => e.StandardLaborTime)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("standard_labor_time");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");
            entity.Property(e => e.TaskName)
                .HasMaxLength(100)
                .HasColumnName("task_name");
            entity.Property(e => e.TechnicianId).HasColumnName("technician_id");

            entity.HasOne(d => d.MaintenanceTicket).WithMany(p => p.ServiceTasks)
                .HasForeignKey(d => d.MaintenanceTicketId)
                .HasConstraintName("FK__service_t__maint__07C12930");

            entity.HasOne(d => d.ServiceCategory).WithMany(p => p.ServiceTasks)
                .HasForeignKey(d => d.ServiceCategoryId)
                .HasConstraintName("FK_service_task_service_category");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.ServiceTasks)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_service_task_status");

            entity.HasOne(d => d.Technician).WithMany(p => p.ServiceTasks)
                .HasForeignKey(d => d.TechnicianId)
                .HasConstraintName("FK_service_task_technician");
        });

        modelBuilder.Entity<ServiceTaskTechnician>(entity =>
        {
            entity.HasKey(e => new { e.ServiceTaskId, e.TechnicianId });

            entity.ToTable("service_task_technician");

            entity.HasIndex(e => e.ServiceTaskId, "IX_service_task_technician_service_task_id");

            entity.HasIndex(e => e.TechnicianId, "IX_service_task_technician_technician_id");

            entity.Property(e => e.ServiceTaskId).HasColumnName("service_task_id");
            entity.Property(e => e.TechnicianId).HasColumnName("technician_id");
            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("assigned_date");
            entity.Property(e => e.RoleInTask)
                .HasMaxLength(100)
                .HasColumnName("role_in_task");

            entity.HasOne(d => d.ServiceTask).WithMany(p => p.ServiceTaskTechnicians)
                .HasForeignKey(d => d.ServiceTaskId)
                .HasConstraintName("FK_service_task_technician_service_task");

            entity.HasOne(d => d.Technician).WithMany(p => p.ServiceTaskTechnicians)
                .HasForeignKey(d => d.TechnicianId)
                .HasConstraintName("FK_service_task_technician_technician");
        });

        modelBuilder.Entity<StatusLookup>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__status_l__357D4CF8AECDDAF2");

            entity.ToTable("status_lookup");

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<StockIn>(entity =>
        {
            entity.ToTable("stock_in");

            entity.HasIndex(e => e.ApprovedBy, "IX_stock_in_approved_by");

            entity.HasIndex(e => e.CreatedAt, "IX_stock_in_created_at");

            entity.HasIndex(e => e.StatusCode, "IX_stock_in_status_code");

            entity.HasIndex(e => e.StockInRequestId, "IX_stock_in_stock_in_request_id");

            entity.HasIndex(e => e.StockInRequestId, "UQ_stock_in_request_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedAt)
                .HasColumnType("datetime")
                .HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
            entity.Property(e => e.LastModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("last_modified_date");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasDefaultValue("PENDING")
                .HasColumnName("status_code");
            entity.Property(e => e.StockInRequestId).HasColumnName("stock_in_request_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.StockInApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_stock_in_approved_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockInCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_stock_in_created_by");

            entity.HasOne(d => d.LastModifiedByNavigation).WithMany(p => p.StockInLastModifiedByNavigations)
                .HasForeignKey(d => d.LastModifiedBy)
                .HasConstraintName("FK_stock_in_last_modified_by");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.StockIns)
                .HasForeignKey(d => d.StatusCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_in_status");

            entity.HasOne(d => d.StockInRequest).WithOne(p => p.StockIn)
                .HasForeignKey<StockIn>(d => d.StockInRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_in_request");
        });

        modelBuilder.Entity<StockInDetail>(entity =>
        {
            entity.ToTable("stock_in_detail");

            entity.HasIndex(e => e.ComponentId, "IX_stock_in_detail_component_id");

            entity.HasIndex(e => e.StockInId, "IX_stock_in_detail_stock_in_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ComponentCode)
                .HasMaxLength(50)
                .HasColumnName("component_code");
            entity.Property(e => e.ComponentId).HasColumnName("component_id");
            entity.Property(e => e.ComponentName)
                .HasMaxLength(200)
                .HasColumnName("component_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ExportPricePerUnit)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("export_price_per_unit");
            entity.Property(e => e.ImportPricePerUnit)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("import_price_per_unit");
            entity.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
            entity.Property(e => e.LastModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("last_modified_date");
            entity.Property(e => e.MinQuantity).HasColumnName("min_quantity");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.QuantityAfterCheck).HasColumnName("quantity_after_check");
            entity.Property(e => e.StockInId).HasColumnName("stock_in_id");
            entity.Property(e => e.TypeComponent)
                .HasMaxLength(200)
                .HasColumnName("type_component");
            entity.Property(e => e.Vat)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("vat");

            entity.HasOne(d => d.Component).WithMany(p => p.StockInDetails)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_in_detail_component");

            entity.HasOne(d => d.StockIn).WithMany(p => p.StockInDetails)
                .HasForeignKey(d => d.StockInId)
                .HasConstraintName("FK_stock_in_detail_stock_in");
        });

        modelBuilder.Entity<StockInRequest>(entity =>
        {
            entity.ToTable("stock_in_request");

            entity.HasIndex(e => e.BranchId, "IX_stock_in_request_branch_id");

            entity.HasIndex(e => e.CreatedAt, "IX_stock_in_request_created_at");

            entity.HasIndex(e => e.CreatedBy, "IX_stock_in_request_created_by");

            entity.HasIndex(e => e.StatusCode, "IX_stock_in_request_status_code");

            entity.HasIndex(e => e.Code, "UQ_stock_in_request_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
            entity.Property(e => e.LastModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("last_modified_date");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasDefaultValue("PENDING")
                .HasColumnName("status_code");

            entity.HasOne(d => d.Branch).WithMany(p => p.StockInRequests)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_in_request_branch");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockInRequestCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_stock_in_request_created_by");

            entity.HasOne(d => d.LastModifiedByNavigation).WithMany(p => p.StockInRequestLastModifiedByNavigations)
                .HasForeignKey(d => d.LastModifiedBy)
                .HasConstraintName("FK_stock_in_request_last_modified_by");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.StockInRequests)
                .HasForeignKey(d => d.StatusCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_in_request_status");
        });

        modelBuilder.Entity<StockInRequestDetail>(entity =>
        {
            entity.HasKey(e => new { e.StockInRequestId, e.ComponentId });

            entity.ToTable("stock_in_request_detail");

            entity.HasIndex(e => e.ComponentId, "IX_stock_in_request_detail_component_id");

            entity.Property(e => e.StockInRequestId).HasColumnName("stock_in_request_id");
            entity.Property(e => e.ComponentId).HasColumnName("component_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Component).WithMany(p => p.StockInRequestDetails)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_in_request_detail_component");

            entity.HasOne(d => d.StockInRequest).WithMany(p => p.StockInRequestDetails)
                .HasForeignKey(d => d.StockInRequestId)
                .HasConstraintName("FK_stock_in_request_detail_request");
        });

        modelBuilder.Entity<TicketComponent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ticket_c__3213E83F939A6879");

            entity.ToTable("ticket_component");

            entity.HasIndex(e => e.BranchId, "IX_ticket_component_branch_id");

            entity.HasIndex(e => e.MaintenanceTicketId, "IX_ticket_component_ticket_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualQuantity)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("actual_quantity");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ComponentId).HasColumnName("component_id");
            entity.Property(e => e.MaintenanceTicketId).HasColumnName("maintenance_ticket_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ServicePackageId).HasColumnName("service_package_id");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Branch).WithMany(p => p.TicketComponents)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_ticket_component_branch");

            entity.HasOne(d => d.Component).WithMany(p => p.TicketComponents)
                .HasForeignKey(d => d.ComponentId)
                .HasConstraintName("FK__ticket_co__compo__09A971A2");

            entity.HasOne(d => d.MaintenanceTicket).WithMany(p => p.TicketComponents)
                .HasForeignKey(d => d.MaintenanceTicketId)
                .HasConstraintName("FK__ticket_co__maint__0A9D95DB");
        });

        modelBuilder.Entity<TotalReceipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__total_re__3213E83F2AC58E45");

            entity.ToTable("total_receipt");

            entity.HasIndex(e => e.BranchId, "IX_total_receipt_branch_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountantId).HasColumnName("accountant_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .HasDefaultValue("VND")
                .HasColumnName("currency_code");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.FinalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("final_amount");
            entity.Property(e => e.MaintenanceTicketId).HasColumnName("maintenance_ticket_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.PackageDiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("package_discount_amount");
            entity.Property(e => e.ServicePackageId).HasColumnName("service_package_id");
            entity.Property(e => e.ServicePackageName)
                .HasMaxLength(255)
                .HasColumnName("service_package_name");
            entity.Property(e => e.ServicePackagePrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("service_package_price");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.SurchargeAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("surcharge_amount");
            entity.Property(e => e.VatAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("vat_amount");
            entity.Property(e => e.VatPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("vat_percent");

            entity.HasOne(d => d.Accountant).WithMany(p => p.TotalReceipts)
                .HasForeignKey(d => d.AccountantId)
                .HasConstraintName("FK__total_rec__accou__0B91BA14");

            entity.HasOne(d => d.Branch).WithMany(p => p.TotalReceipts)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__total_rec__branc__0C85DE4D");

            entity.HasOne(d => d.Car).WithMany(p => p.TotalReceipts)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("FK__total_rec__car_i__0D7A0286");

            entity.HasOne(d => d.MaintenanceTicket).WithMany(p => p.TotalReceipts)
                .HasForeignKey(d => d.MaintenanceTicketId)
                .HasConstraintName("FK__total_rec__maint__0E6E26BF");

            entity.HasOne(d => d.ServicePackage).WithMany(p => p.TotalReceipts)
                .HasForeignKey(d => d.ServicePackageId)
                .HasConstraintName("FK_total_receipt_service_package");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.TotalReceipts)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_total_receipt_status");
        });

        modelBuilder.Entity<TypeComponent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__type_com__3213E83FE8D2B9FE");

            entity.ToTable("type_component");

            entity.HasIndex(e => new { e.Name, e.BranchId }, "UQ_type_component_name_branch").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");

            entity.HasOne(d => d.Branch).WithMany(p => p.TypeComponents)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_type_component_branch");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.TypeComponents)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_type_component_status");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__user__3213E83FB4BA35B1");

            entity.ToTable("user");

            entity.HasIndex(e => e.BranchId, "IX_user_branch_id");

            entity.HasIndex(e => e.Username, "UQ__user__F3DBC572E50C2831").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CitizenId)
                .HasMaxLength(20)
                .HasColumnName("citizen_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .HasColumnName("gender");
            entity.Property(e => e.Image)
                .HasMaxLength(255)
                .HasColumnName("image");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("is_delete");
            entity.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
            entity.Property(e => e.LastModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("last_modified_date");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.ResetDate)
                .HasColumnType("datetime")
                .HasColumnName("reset_date");
            entity.Property(e => e.ResetKey)
                .HasMaxLength(100)
                .HasColumnName("reset_key");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasColumnName("tax_code");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.Branch).WithMany(p => p.Users)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__user__branch_id__114A936A");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__user__role_id__123EB7A3");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_user_status");
        });

        modelBuilder.Entity<VehicleCheckin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vehicle___3213E83FDBED5AEA");

            entity.ToTable("vehicle_checkin");

            entity.HasIndex(e => e.CarId, "IX_vehicle_checkin_car_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Mileage).HasColumnName("mileage");
            entity.Property(e => e.Notes)
                .HasMaxLength(255)
                .HasColumnName("notes");
            entity.Property(e => e.SnapshotBranchName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_branch_name");
            entity.Property(e => e.SnapshotCarModel)
                .HasMaxLength(255)
                .HasColumnName("snapshot_car_model");
            entity.Property(e => e.SnapshotCarName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_car_name");
            entity.Property(e => e.SnapshotColor)
                .HasMaxLength(50)
                .HasColumnName("snapshot_color");
            entity.Property(e => e.SnapshotConsulterName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_consulter_name");
            entity.Property(e => e.SnapshotCustomerAddress)
                .HasMaxLength(255)
                .HasColumnName("snapshot_customer_address");
            entity.Property(e => e.SnapshotCustomerEmail)
                .HasMaxLength(255)
                .HasColumnName("snapshot_customer_email");
            entity.Property(e => e.SnapshotCustomerName)
                .HasMaxLength(255)
                .HasColumnName("snapshot_customer_name");
            entity.Property(e => e.SnapshotCustomerPhone)
                .HasMaxLength(50)
                .HasColumnName("snapshot_customer_phone");
            entity.Property(e => e.SnapshotEngineNumber)
                .HasMaxLength(50)
                .HasColumnName("snapshot_engine_number");
            entity.Property(e => e.SnapshotLicensePlate)
                .HasMaxLength(50)
                .HasColumnName("snapshot_license_plate");
            entity.Property(e => e.SnapshotMileage).HasColumnName("snapshot_mileage");
            entity.Property(e => e.SnapshotVehicleType)
                .HasMaxLength(100)
                .HasColumnName("snapshot_vehicle_type");
            entity.Property(e => e.SnapshotVehicleTypeId).HasColumnName("snapshot_vehicle_type_id");
            entity.Property(e => e.SnapshotVinNumber)
                .HasMaxLength(50)
                .HasColumnName("snapshot_vin_number");
            entity.Property(e => e.SnapshotYearOfManufacture).HasColumnName("snapshot_year_of_manufacture");
            entity.Property(e => e.StatusCode)
                .HasMaxLength(50)
                .HasColumnName("status_code");

            entity.HasOne(d => d.Branch).WithMany(p => p.VehicleCheckins)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__vehicle_c__branc__14270015");

            entity.HasOne(d => d.Car).WithMany(p => p.VehicleCheckins)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("FK__vehicle_c__car_i__151B244E");

            entity.HasOne(d => d.StatusCodeNavigation).WithMany(p => p.VehicleCheckins)
                .HasForeignKey(d => d.StatusCode)
                .HasConstraintName("FK_vehicle_checkin_status");
        });

        modelBuilder.Entity<VehicleCheckinImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vehicle___3213E83F9DF416E0");

            entity.ToTable("vehicle_checkin_image");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.VehicleCheckinId).HasColumnName("vehicle_checkin_id");

            entity.HasOne(d => d.VehicleCheckin).WithMany(p => p.VehicleCheckinImages)
                .HasForeignKey(d => d.VehicleCheckinId)
                .HasConstraintName("FK__vehicle_c__vehic__18EBB532");
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vehicle___3213E83F773A4907");

            entity.ToTable("vehicle_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
