

using Application.DTOs;

namespace Application.Interfaces
{
    public interface ICatalogServices
    {
        Task<bool> ExistAppointmentStatus(int id);
        Task<bool> ExistBloodId(int id);
        Task<bool> ExistSexId(int id);
        Task<List<OptionDto>> GetAllbloodOptions();
        Task<List<OptionDto>> GetAllSexOptions();
    }
}
