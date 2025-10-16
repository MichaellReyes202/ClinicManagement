using Domain.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs;
public class EmployesUpdateDto
{
    public int Id { get; set; }
    public required  string FirstName { get; set; }
    public   string? MiddleName { get; set; }
    public required string LastName  { get; set; }
    public  string? SecondLastName { get; set; }
    public  int Age { get; set; }
    public  int PositionId { get; set; }
    public  string? ContactPhone { get; set; }
    public DateTime HireDate { get; set; } 
    public  string Dni { get; set; }
    public required string Email { get; set; }
    public  bool IsActive { get; set; }
    public  int? SpecialtyId  { get; set; }
}
