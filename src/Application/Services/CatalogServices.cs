using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Application.Services;

public class CatalogServices : ICatalogServices
{
    private readonly ISexRepository _sexRepository;
    private readonly ICatBloodRepository _catBloodRepository;
    private readonly ICatAppointmentStatusRepository _catAppointmentStatusRepository;

    public CatalogServices
    (
        ISexRepository sexRepository, 
        ICatBloodRepository catBloodRepository ,
        ICatAppointmentStatusRepository catAppointmentStatusRepository
    )
    {
        _sexRepository = sexRepository;
        _catBloodRepository = catBloodRepository;
        _catAppointmentStatusRepository = catAppointmentStatusRepository;
    }
    public async Task<List<OptionDto>> GetAllSexOptions()
    {
        var query = await _sexRepository.GetQuery();
        return await query.Select(s => new OptionDto
        {
            Id = s.Id,
            Name = s.Name,
        })
        .OrderBy(s => s.Id)
        .ToListAsync();

    }
    public async Task<List<OptionDto>> GetAllbloodOptions()
    {
        var query = await _catBloodRepository.GetQuery();
        return await query.Select(s => new OptionDto
        {
            Id = s.Id,
            Name = s.Name,
        })
        .OrderBy(s => s.Id)
        .ToListAsync();

    }

    public async Task<List<OptionDto>> GetAllAppointmentStatusOptions()
    {
        var query = await _catAppointmentStatusRepository.GetQuery();
        return await query.Select(s => new OptionDto
        {
            Id = s.Id,
            Name = s.Name,
        })
        .OrderBy(s => s.Id)
        .ToListAsync();
    }

    public async Task<bool> ExistAppointmentStatus(int id )
    {
        return await _catAppointmentStatusRepository.ExistAsync(s => s.Id == id);
    }
    public async Task<bool> ExistSexId(int id)
    {
        return await _sexRepository.ExistAsync(s => s.Id == id);
    }

    public async Task<bool> ExistBloodId(int id)
    {
        return await _catBloodRepository.ExistAsync(s => s.Id == id);
    }
}