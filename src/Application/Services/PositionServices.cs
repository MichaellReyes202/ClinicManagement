using Application.DTOs;
using Application.DTOs.Position;
using Application.DTOs.specialty;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;


namespace Application.Services;

public class PositionServices : IPositionServices
{
    private readonly IPositionRepository _positionRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<PositionCreationDto> _validatorCreate;
    private readonly IValidator<PositionUpdateDto> _validatorUpdate;

    public PositionServices(
        IPositionRepository positionRepository,
        IMapper mapper,
        IValidator<PositionCreationDto> validatorCreate,
        IValidator<PositionUpdateDto> validatorUpdate
    )
    {
        _positionRepository = positionRepository;
        _mapper = mapper;
        _validatorUpdate = validatorUpdate;
        _validatorCreate = validatorCreate;
    }
    public async Task<List<OptionDto>> GetAllPositionOptions()
    {
        var positions = await _positionRepository.GetAllAsync();
        var options = positions.Select(p => new OptionDto
        {
            Id = p.Id,
            Name = p.Name,
        }).ToList();
        return options;
    }
    public async Task<Result<PaginatedResponseDto<PositionListDto>>> GetAllPosition(PaginationDto pagination)
    {
        try
        {
            var (baseQuery, total) = await _positionRepository.GetQueryAndTotal();
            var projectedQuery = baseQuery
                    .Select(e => new PositionListDto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        IsActive = e.IsActive,
                        Description = e.Description,
                        Employees = e.Employees.Count
                    })
                    .OrderBy(e => e.Id);
            var items = await projectedQuery
                    .Skip(pagination.Offset)
                    .Take(pagination.Limit)
                    .ToListAsync();
            var paginatedResponse = new PaginatedResponseDto<PositionListDto>(total, items);
            return Result<PaginatedResponseDto<PositionListDto>>.Success(paginatedResponse);
        }
        catch (Exception ex)
        {
            return Result<PaginatedResponseDto<PositionListDto>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }

    public async Task<Result<Position>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<Position>.Failure(new Error(ErrorCodes.BadRequest, "The id must be a positive integer."));
            var position = await _positionRepository.GetByIdAsync(id);
            if (position is null)
            {
                return Result<Position>.Failure(new Error(ErrorCodes.NotFound, $"The Specialty with the id {id} was not found"));
            }
            return Result<Position>.Success(position);
        }
        catch (Exception ex)
        {
            return Result<Position>.Failure(
                new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }


    public async Task<Result<Position>> AddPositionAsync(PositionCreationDto positionDto)
    {
        var validationResult = await _validatorCreate.ValidateAsync(positionDto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<Position>.Failure(errors);
        }
        try
        {
            var existingPosition = await _positionRepository.ExistAsync(p => p.Name.ToUpper() == positionDto.Name.ToUpper());
            if (existingPosition)
            {
                return Result<Position>.Failure(
                    new Error(ErrorCodes.Conflict, $"The position with the name '{positionDto.Name}' already exists."));
            }
            var position = _mapper.Map<Position>(positionDto);
            await _positionRepository.AddAsync(position);
            await _positionRepository.SaveChangesAsync();
            return Result<Position>.Success(position);
        }
        catch (Exception ex)
        {
            return Result<Position>.Failure(
                new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }

    public async Task<Result> UpdatePositionAsync(int id, PositionUpdateDto  positionUpdate)
    {
        var validationResult = await _validatorUpdate.ValidateAsync(positionUpdate);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result.Failure(errors);
        }
        
        var position = await _positionRepository.GetByIdAsync(id);
        if (position == null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound, "Position not found"));
        }
        //if(id !== position.id) // The ID in the route does not match the ID in the request body.

        var existingSpecialty = await _positionRepository.ExistAsync(ps => ps.Name.ToUpper() == positionUpdate.Name.ToUpper() && ps.Id != position.Id);
        
        if (existingSpecialty )
        {
            return Result.Failure(new Error(ErrorCodes.Conflict, $"A postion with the name '{positionUpdate.Name}' already exists."));
        }
        _mapper.Map(positionUpdate, position);
        position.UpdatedAt = DateTime.UtcNow; // Establecer UpdatedAt en UTC
        try
        {
            await _positionRepository.UpdateAsync(position);
            await _positionRepository.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result<Position>.Failure(
                new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }
}