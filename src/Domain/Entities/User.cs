using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string NormalizedEmail { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? LastLogin { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool? LockoutEnabled { get; set; }

    public int  AccessFailedCount { get; set; } = 0;

    public DateTime? LockoutEnd { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<Employee> EmployeeCreatedByUsers { get; set; } = new List<Employee>();

    public virtual ICollection<Employee> EmployeeUpdatedByUsers { get; set; } = new List<Employee>();

    public virtual Employee? EmployeeUser { get; set; }

    public virtual ICollection<User> InverseCreatedByUser { get; set; } = new List<User>();

    public virtual ICollection<User> InverseUpdatedByUser { get; set; } = new List<User>();

    public virtual User? UpdatedByUser { get; set; }

    public virtual ICollection<UserRole> UserRoleCreatedByUsers { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();
}
