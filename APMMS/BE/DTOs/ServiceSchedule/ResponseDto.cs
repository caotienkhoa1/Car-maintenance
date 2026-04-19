using System;
using System.Collections.Generic;

namespace BE.DTOs.ServiceSchedule
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public long? CarId { get; set; }
        public string? CarName { get; set; }
        public string? LicensePlate { get; set; }
        public string? CarModel { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchPhone { get; set; }

        public long? AcceptedById { get; set; }

        public string? AcceptedByName { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public string? AcceptNote { get; set; }

        public long? ConsultantBranchId { get; set; }

        public string? ConsultantBranchName { get; set; }

        public long? ServiceCategoryId { get; set; }

        public string? ServiceCategoryName { get; set; }

        public bool IsPublicBooking { get; set; } // true nếu là khách đặt lịch công khai (không có tài khoản)

        public List<NoteResponseDto> Notes { get; set; } = new List<NoteResponseDto>();
        
        // Audit trail fields
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }
    }

    public class ListResponseDto
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public long? CarId { get; set; }
        public string? CarName { get; set; }
        public string? LicensePlate { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }

        public long? AcceptedById { get; set; }

        public string? AcceptedByName { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public string? AcceptNote { get; set; }

        public bool IsPublicBooking { get; set; } // true nếu là khách đặt lịch công khai (không có tài khoản)

        public List<NoteResponseDto> Notes { get; set; } = new List<NoteResponseDto>();
    }
}
