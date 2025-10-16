using Application.DTOs;
using Domain.Entities;
using Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISpecialtiesServices
    {
        Task<Result<Specialty>> AddSpecialtyAsync(SpecialtiesDto specialtiesDto);
        Task<Result<PaginatedResponseDto<SpecialtyListDto>>> GetAllSpecialties(PaginationDto pagination);
        Task<List<OptionDto>> GetAllSpecialtiesOptions();
        Task<Result<Specialty>> GetByIdAsync(int id);
        Task<Result<Specialty>> UpdateSpecialtyAsync(int id, SpecialtiesDto specialtiesDto);
    }
}
