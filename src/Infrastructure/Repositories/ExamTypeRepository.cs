using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;
public class ExamTypeRepository(ClinicDbContext context) : GenericRepository<ExamType>(context), IExamTypeRepository
{
    public Task Update(ExamType examType)
    {
        dbSet.Entry(examType).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
