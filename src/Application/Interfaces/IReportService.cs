using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IReportService
    {
        Task<(byte[] FileContent, string ContentType)> GetMedicalProductivityReportAsync(DateTime from, DateTime to, string format);
        Task<(byte[] FileContent, string ContentType)> GetMorbidityReportAsync(DateTime from, DateTime to, string format);
        Task<(byte[] FileContent, string ContentType)> GetLabVolumeReportAsync(DateTime from, DateTime to, string format);
        Task<(byte[] FileContent, string ContentType)> GetPatientAbsenteeismReportAsync(DateTime from, DateTime to, string format);
    }
}
