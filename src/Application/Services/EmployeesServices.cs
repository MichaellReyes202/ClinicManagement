using Application.Interfaces;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class EmployesServices : IEmployesServices
    {
        private readonly IEmployesRepository _employeesRepository;

        public EmployesServices(IEmployesRepository employeesRepository)
        {
            _employeesRepository = employeesRepository;
        }

        // Metodo para crear un empleado 
        //public async Task<Result<Employee>> CreateEmployeeAsync(EmployeeCreacionDto employee)
        //{
        //   return t
        //}
    }
}
