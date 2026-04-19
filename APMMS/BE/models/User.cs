using System;
using System.Collections.Generic;

namespace BE.models;

public partial class User
{
    public long Id { get; set; }

    public string? Code { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Gender { get; set; }

    public string? Image { get; set; }

    public string? TaxCode { get; set; }

    public string? StatusCode { get; set; }

    public bool? IsDelete { get; set; }

    public string? ResetKey { get; set; }

    public DateTime? ResetDate { get; set; }

    public long? RoleId { get; set; }

    public long? BranchId { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public DateOnly? Dob { get; set; }

    public string? CitizenId { get; set; }

    public string? Address { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();

    public virtual ICollection<CustomerGuest> CustomerGuests { get; set; } = new List<CustomerGuest>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<HistoryLog> HistoryLogs { get; set; } = new List<HistoryLog>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTicketConsulters { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTicketTechnicians { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<MaintenanceTicketTechnician> MaintenanceTicketTechniciansNavigation { get; set; } = new List<MaintenanceTicketTechnician>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<ScheduleServiceNote> ScheduleServiceNotes { get; set; } = new List<ScheduleServiceNote>();

    public virtual ICollection<ScheduleService> ScheduleServices { get; set; } = new List<ScheduleService>();

    public virtual ICollection<ServiceTaskTechnician> ServiceTaskTechnicians { get; set; } = new List<ServiceTaskTechnician>();

    public virtual ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();

    public virtual StatusLookup? StatusCodeNavigation { get; set; }

    public virtual ICollection<StockIn> StockInApprovedByNavigations { get; set; } = new List<StockIn>();

    public virtual ICollection<StockIn> StockInCreatedByNavigations { get; set; } = new List<StockIn>();

    public virtual ICollection<StockIn> StockInLastModifiedByNavigations { get; set; } = new List<StockIn>();

    public virtual ICollection<StockInRequest> StockInRequestCreatedByNavigations { get; set; } = new List<StockInRequest>();

    public virtual ICollection<StockInRequest> StockInRequestLastModifiedByNavigations { get; set; } = new List<StockInRequest>();

    public virtual ICollection<TotalReceipt> TotalReceipts { get; set; } = new List<TotalReceipt>();
}
