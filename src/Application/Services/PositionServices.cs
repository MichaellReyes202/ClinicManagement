using Application.Interfaces;
using Domain.Interfaces;
using Domain.Errors;
using Application.DTOs;


namespace Application.Services
{
    public class PositionServices : IPositionServices
    {
        private readonly IPositionRepository _positionRepository;

        public PositionServices(IPositionRepository positionRepository)
        {
            _positionRepository = positionRepository;
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
    }

}
