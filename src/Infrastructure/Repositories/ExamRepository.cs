using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class ExamRepository : GenericRepository<Exam>, IExamRepository
{
    public ExamRepository(ClinicDbContext context) : base(context)
    {
    }
}
