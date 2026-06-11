using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string NormalizedEmail { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? LastLogin { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool? LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public bool RequiresPasswordChange { get; set; }

    public virtual ICollection<Appointment> AppointmentCreatedByUsers { get; set; } = new List<Appointment>();

    public virtual ICollection<Appointment> AppointmentUpdatedByUsers { get; set; } = new List<Appointment>();

    public virtual ICollection<Auditlog> Auditlogs { get; set; } = new List<Auditlog>();

    public virtual ICollection<ClinicSchedule> ClinicScheduleCreatedByUsers { get; set; } = new List<ClinicSchedule>();

    public virtual ICollection<ClinicSchedule> ClinicScheduleUpdatedByUsers { get; set; } = new List<ClinicSchedule>();

    public virtual ICollection<Consultation> ConsultationCreatedByUsers { get; set; } = new List<Consultation>();

    public virtual ICollection<Consultation> ConsultationUpdatedByUsers { get; set; } = new List<Consultation>();

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<Employee> EmployeeCreatedByUsers { get; set; } = new List<Employee>();

    public virtual ICollection<Employee> EmployeeUpdatedByUsers { get; set; } = new List<Employee>();

    public virtual Employee? EmployeeUser { get; set; }

    public virtual ICollection<ExamType> ExamTypeCreatedByUsers { get; set; } = new List<ExamType>();

    public virtual ICollection<ExamType> ExamTypeUpdatedByUsers { get; set; } = new List<ExamType>();

    public virtual ICollection<User> InverseCreatedByUser { get; set; } = new List<User>();

    public virtual ICollection<User> InverseUpdatedByUser { get; set; } = new List<User>();

    public virtual ICollection<Patient> PatientCreatedByUsers { get; set; } = new List<Patient>();

    public virtual ICollection<Patient> PatientUpdatedByUsers { get; set; } = new List<Patient>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<RoleView> RoleViews { get; set; } = new List<RoleView>();

    public virtual User? UpdatedByUser { get; set; }

    public virtual ICollection<UserRole> UserRoleCreatedByUsers { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();

    public virtual ICollection<UserView> UserViewGrantedByUsers { get; set; } = new List<UserView>();

    public virtual ICollection<UserView> UserViewUsers { get; set; } = new List<UserView>();
}
