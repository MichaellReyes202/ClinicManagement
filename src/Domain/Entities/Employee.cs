using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Employee
{
    public int Id { get; set; }
    public int? UserId { get; set; }

    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string? SecondLastName { get; set; }
    public int Age { get; set; }

    public int PositionId { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime HireDate { get; set; }
    public string? Dni { get; set; }
    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;
    public bool? IsActive { get; set; }
    public int? SpecialtyId { get; set; }
    public int? CreatedByUserId { get; set; } // Usuario que creo el registro   
    public int? UpdatedByUserId { get; set; } // Usuario que actualizo el registro




    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }
    public virtual Position Position { get; set; } = null!;

    public virtual Specialty? Specialty { get; set; }

    public virtual User? UpdatedByUser { get; set; }

    public virtual User? User { get; set; }
}
