using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Application.Services;

public class CatalogServices : ICatalogServices
{
    private readonly ISexRepository _sexRepository;
    private readonly ICatBloodRepository _catBloodRepository;

    public CatalogServices(ISexRepository sexRepository, ICatBloodRepository catBloodRepository)
    {
        _sexRepository = sexRepository;
        _catBloodRepository = catBloodRepository;
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
    public async Task<bool> ExistSexId(int id)
    {
        return await _sexRepository.ExistAsync(s => s.Id == id);
    }

    public async Task<bool> ExistBloodId(int id)
    {
        return await _catBloodRepository.ExistAsync(s => s.Id == id);
    }
}