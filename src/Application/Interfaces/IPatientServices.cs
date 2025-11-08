using Application.DTOs;
using Application.DTOs.Patient;
using Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPatientServices
    {
        Task<Result<PatientResponseDto>> AddPatientAsync(PatientCreateDto patient);
        Task<Result<PaginatedResponseDto<PatientResponseDto>>> GetAllPatients(PaginationDto pagination);
        Task<Result<PatientResponseDto>> GetPatientById(int Id);
        Task<Result<PaginatedResponseDto<PatientSearchDto>>> SearchPatient(PaginationDto pagination);
        Task<Result> UpdatePatientAsync(PatientUpdateDto dto, int patientId);
    }
}
