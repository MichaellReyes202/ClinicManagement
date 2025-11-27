using Application.DTOs;
using Application.DTOs.ExamType;
using Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IExamTypeServices
{
    Task<Result<ExamTypeResponseDto>> Add(ExamTypeCreateDto examTypeDto);
    Task<Result<PaginatedResponseDto<ExamTypeListDto>>> GetAll(PaginationDto pagination);
    Task<Result<ExamTypeResponseDto>> GetById(int Id);
    Task<Result> Update(ExamTypeUpdateDto examTypeDto, int examTypeId);
    // actualiza el estado de un examen --
    Task<Result> UpdateState(int examTypeId);
}
