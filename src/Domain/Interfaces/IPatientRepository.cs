using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPatientRepository : IGenericRepository<Patient>
    {
        Task UpdatePatientAsync(Patient employee);
    }
}
