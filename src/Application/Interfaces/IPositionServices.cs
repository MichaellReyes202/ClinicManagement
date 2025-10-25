using Application.DTOs;
using Application.DTOs.Position;
using Domain.Entities;
using Domain.Errors;


namespace Application.Interfaces
{
    public interface IPositionServices
    {
        Task<Result<Position>> AddPositionAsync(PositionCreationDto positionDto);
        Task<Result<PaginatedResponseDto<PositionListDto>>> GetAllPosition(PaginationDto pagination);
        Task<List<OptionDto>> GetAllPositionOptions();
        Task<Result<Position>> GetByIdAsync(int id);
        Task<Result> UpdatePositionAsync(int id, PositionUpdateDto positionUpdate);
    }
}
