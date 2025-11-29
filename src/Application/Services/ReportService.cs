using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IGenericRepository<Appointment> _appointmentRepository;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<Consultation> _consultationRepository;
        private readonly IGenericRepository<Exam> _examRepository;

        public ReportService(
            IGenericRepository<Appointment> appointmentRepository,
            IGenericRepository<Employee> employeeRepository,
            IGenericRepository<Consultation> consultationRepository,
            IGenericRepository<Exam> examRepository)
        {
            _appointmentRepository = appointmentRepository;
            _employeeRepository = employeeRepository;
            _consultationRepository = consultationRepository;
            _examRepository = examRepository;
            
            // License configuration for QuestPDF (Community)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region Medical Productivity
        public async Task<(byte[] FileContent, string ContentType)> GetMedicalProductivityReportAsync(DateTime from, DateTime to, string format)
        {
            var query = await _appointmentRepository.GetQuery();
            var appointments = await query
                .Include(a => a.Employee)
                .Where(a => a.StartTime >= from && a.StartTime <= to)
                .AsNoTracking()
                .ToListAsync();

            var doctorStats = appointments
                .GroupBy(a => a.EmployeeId)
                .Select(g => new MedicalProductivityDto
                {
                    DoctorName = g.First().Employee != null ? $"{g.First().Employee.FirstName} {g.First().Employee.LastName}" : "Desconocido",
                    TotalAppointments = g.Count(),
                    Attended = g.Count(a => a.StatusId == 3), // Completed
                    Cancelled = g.Count(a => a.StatusId == 4), // Cancelled
                    AvgDurationMinutes = g.Where(a => a.StatusId == 3 && a.EndTime > a.StartTime)
                        .Select(a => (a.EndTime.Value - a.StartTime).TotalMinutes)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .ToList();

            if (format.ToLower() == "excel") return GenerateMedicalProductivityExcel(doctorStats, from, to);
            return GenerateMedicalProductivityPdf(doctorStats, from, to);
        }

        private (byte[], string) GenerateMedicalProductivityExcel(IEnumerable<MedicalProductivityDto> data, DateTime from, DateTime to)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Productividad Médica");

            worksheet.Cell(1, 1).Value = "Reporte de Productividad Médica";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(2, 1).Value = $"Desde: {from:dd/MM/yyyy} Hasta: {to:dd/MM/yyyy}";

            var headers = new[] { "Doctor", "Total Citas", "Atendidas", "Canceladas", "Duración Promedio (min)" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(4, i + 1).Value = headers[i];
                worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 5;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.DoctorName;
                worksheet.Cell(row, 2).Value = item.TotalAppointments;
                worksheet.Cell(row, 3).Value = item.Attended;
                worksheet.Cell(row, 4).Value = item.Cancelled;
                worksheet.Cell(row, 5).Value = Math.Round(item.AvgDurationMinutes, 0);
                row++;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        private (byte[], string) GenerateMedicalProductivityPdf(IEnumerable<MedicalProductivityDto> data, DateTime from, DateTime to)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    SetupPage(page, "Reporte de Productividad Médica", from, to);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Doctor");
                            header.Cell().Element(CellStyle).Text("Total");
                            header.Cell().Element(CellStyle).Text("Atendidas");
                            header.Cell().Element(CellStyle).Text("Canceladas");
                            header.Cell().Element(CellStyle).Text("Duración Prom. (min)");
                            static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Element(CellStyle).Text(item.DoctorName);
                            table.Cell().Element(CellStyle).Text(item.TotalAppointments.ToString());
                            table.Cell().Element(CellStyle).Text(item.Attended.ToString());
                            table.Cell().Element(CellStyle).Text(item.Cancelled.ToString());
                            table.Cell().Element(CellStyle).Text(Math.Round(item.AvgDurationMinutes, 0).ToString());
                            static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    });
                    SetupFooter(page);
                });
            });
            return (document.GeneratePdf(), "application/pdf");
        }
        #endregion

        #region Morbidity
        public async Task<(byte[] FileContent, string ContentType)> GetMorbidityReportAsync(DateTime from, DateTime to, string format)
        {
            var query = await _consultationRepository.GetQuery();
            var consultations = await query
                .Include(c => c.Patient)
                .ThenInclude(p => p.Sex)
                .Where(c => c.CreatedAt >= from && c.CreatedAt <= to && !string.IsNullOrEmpty(c.Diagnosis))
                .AsNoTracking()
                .ToListAsync();

            var morbidityStats = consultations
                .GroupBy(c => c.Diagnosis)
                .Select(g => new MorbidityDto
                {
                    Diagnosis = g.Key ?? "Sin Diagnóstico",
                    TotalCases = g.Count(),
                    Male = g.Count(c => c.Patient.SexId == 1), // Assuming 1 is Male
                    Female = g.Count(c => c.Patient.SexId == 2), // Assuming 2 is Female
                    Age0to5 = g.Count(c => GetAge(c.Patient.DateOfBirth) <= 5),
                    Age6to12 = g.Count(c => GetAge(c.Patient.DateOfBirth) >= 6 && GetAge(c.Patient.DateOfBirth) <= 12),
                    Age13to18 = g.Count(c => GetAge(c.Patient.DateOfBirth) >= 13 && GetAge(c.Patient.DateOfBirth) <= 18),
                    Age19to60 = g.Count(c => GetAge(c.Patient.DateOfBirth) >= 19 && GetAge(c.Patient.DateOfBirth) <= 60),
                    Age60Plus = g.Count(c => GetAge(c.Patient.DateOfBirth) > 60)
                })
                .OrderByDescending(x => x.TotalCases)
                .Take(20) // Top 20 diagnoses
                .ToList();

            if (format.ToLower() == "excel") return GenerateMorbidityExcel(morbidityStats, from, to);
            return GenerateMorbidityPdf(morbidityStats, from, to);
        }

        private int GetAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age)) age--;
            return age;
        }

        private (byte[], string) GenerateMorbidityExcel(IEnumerable<MorbidityDto> data, DateTime from, DateTime to)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Morbilidad");

            worksheet.Cell(1, 1).Value = "Reporte de Morbilidad (Top 20)";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(2, 1).Value = $"Desde: {from:dd/MM/yyyy} Hasta: {to:dd/MM/yyyy}";

            var headers = new[] { "Diagnóstico", "Total Casos", "Masc.", "Fem.", "0-5", "6-12", "13-18", "19-60", ">60" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(4, i + 1).Value = headers[i];
                worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 5;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Diagnosis;
                worksheet.Cell(row, 2).Value = item.TotalCases;
                worksheet.Cell(row, 3).Value = item.Male;
                worksheet.Cell(row, 4).Value = item.Female;
                worksheet.Cell(row, 5).Value = item.Age0to5;
                worksheet.Cell(row, 6).Value = item.Age6to12;
                worksheet.Cell(row, 7).Value = item.Age13to18;
                worksheet.Cell(row, 8).Value = item.Age19to60;
                worksheet.Cell(row, 9).Value = item.Age60Plus;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        private (byte[], string) GenerateMorbidityPdf(IEnumerable<MorbidityDto> data, DateTime from, DateTime to)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    SetupPage(page, "Reporte de Morbilidad (Top 20)", from, to);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4); // Diagnosis
                            columns.RelativeColumn(); // Total
                            columns.RelativeColumn(); // M
                            columns.RelativeColumn(); // F
                            columns.RelativeColumn(); // 0-5
                            columns.RelativeColumn(); // 6-12
                            columns.RelativeColumn(); // 13-18
                            columns.RelativeColumn(); // 19-60
                            columns.RelativeColumn(); // >60
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Diagnóstico");
                            header.Cell().Element(CellStyle).Text("Total");
                            header.Cell().Element(CellStyle).Text("M");
                            header.Cell().Element(CellStyle).Text("F");
                            header.Cell().Element(CellStyle).Text("0-5");
                            header.Cell().Element(CellStyle).Text("6-12");
                            header.Cell().Element(CellStyle).Text("13-18");
                            header.Cell().Element(CellStyle).Text("19-60");
                            header.Cell().Element(CellStyle).Text(">60");
                            static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold().FontSize(9)).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Element(CellStyle).Text(item.Diagnosis);
                            table.Cell().Element(CellStyle).Text(item.TotalCases.ToString());
                            table.Cell().Element(CellStyle).Text(item.Male.ToString());
                            table.Cell().Element(CellStyle).Text(item.Female.ToString());
                            table.Cell().Element(CellStyle).Text(item.Age0to5.ToString());
                            table.Cell().Element(CellStyle).Text(item.Age6to12.ToString());
                            table.Cell().Element(CellStyle).Text(item.Age13to18.ToString());
                            table.Cell().Element(CellStyle).Text(item.Age19to60.ToString());
                            table.Cell().Element(CellStyle).Text(item.Age60Plus.ToString());
                            static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.FontSize(9)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    });
                    SetupFooter(page);
                });
            });
            return (document.GeneratePdf(), "application/pdf");
        }
        #endregion

        #region Lab Volume
        public async Task<(byte[] FileContent, string ContentType)> GetLabVolumeReportAsync(DateTime from, DateTime to, string format)
        {
            var query = await _examRepository.GetQuery();
            var exams = await query
                .Include(e => e.ExamType)
                .Include(e => e.Status)
                .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
                .AsNoTracking()
                .ToListAsync();

            var labStats = exams
                .GroupBy(e => e.ExamType.Name)
                .Select(g => new LabVolumeDto
                {
                    ExamType = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(e => e.StatusId == 3), // Assuming 3 is Completed
                    Pending = g.Count(e => e.StatusId == 1)   // Assuming 1 is Pending
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            if (format.ToLower() == "excel") return GenerateLabVolumeExcel(labStats, from, to);
            return GenerateLabVolumePdf(labStats, from, to);
        }

        private (byte[], string) GenerateLabVolumeExcel(IEnumerable<LabVolumeDto> data, DateTime from, DateTime to)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Volumen Laboratorio");

            worksheet.Cell(1, 1).Value = "Reporte de Volumen de Laboratorio";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(2, 1).Value = $"Desde: {from:dd/MM/yyyy} Hasta: {to:dd/MM/yyyy}";

            var headers = new[] { "Tipo de Examen", "Total Solicitados", "Realizados", "Pendientes" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(4, i + 1).Value = headers[i];
                worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 5;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.ExamType;
                worksheet.Cell(row, 2).Value = item.Total;
                worksheet.Cell(row, 3).Value = item.Completed;
                worksheet.Cell(row, 4).Value = item.Pending;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        private (byte[], string) GenerateLabVolumePdf(IEnumerable<LabVolumeDto> data, DateTime from, DateTime to)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    SetupPage(page, "Reporte de Volumen de Laboratorio", from, to);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Tipo de Examen");
                            header.Cell().Element(CellStyle).Text("Total");
                            header.Cell().Element(CellStyle).Text("Realizados");
                            header.Cell().Element(CellStyle).Text("Pendientes");
                            static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Element(CellStyle).Text(item.ExamType);
                            table.Cell().Element(CellStyle).Text(item.Total.ToString());
                            table.Cell().Element(CellStyle).Text(item.Completed.ToString());
                            table.Cell().Element(CellStyle).Text(item.Pending.ToString());
                            static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    });
                    SetupFooter(page);
                });
            });
            return (document.GeneratePdf(), "application/pdf");
        }
        #endregion

        #region Patient Absenteeism
        public async Task<(byte[] FileContent, string ContentType)> GetPatientAbsenteeismReportAsync(DateTime from, DateTime to, string format)
        {
            var query = await _appointmentRepository.GetQuery();
            var appointments = await query
                .Include(a => a.Patient)
                .Include(a => a.Status)
                .Where(a => a.StartTime >= from && a.StartTime <= to && (a.StatusId == 4 || a.StatusId == 5)) // Cancelled or No-Show
                .AsNoTracking()
                .ToListAsync();

            var absenteeismData = appointments
                .Select(a => new AbsenteeismDto
                {
                    Date = a.StartTime,
                    PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "Desconocido",
                    Status = a.Status?.Name ?? "Desconocido",
                    Reason = a.Reason ?? "No especificado"
                })
                .OrderBy(a => a.Date)
                .ToList();

            if (format.ToLower() == "excel") return GenerateAbsenteeismExcel(absenteeismData, from, to);
            return GenerateAbsenteeismPdf(absenteeismData, from, to);
        }

        private (byte[], string) GenerateAbsenteeismExcel(IEnumerable<AbsenteeismDto> data, DateTime from, DateTime to)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ausentismo");

            worksheet.Cell(1, 1).Value = "Reporte de Ausentismo de Pacientes";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(2, 1).Value = $"Desde: {from:dd/MM/yyyy} Hasta: {to:dd/MM/yyyy}";

            var headers = new[] { "Fecha", "Paciente", "Estado", "Motivo" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(4, i + 1).Value = headers[i];
                worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 5;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Date;
                worksheet.Cell(row, 2).Value = item.PatientName;
                worksheet.Cell(row, 3).Value = item.Status;
                worksheet.Cell(row, 4).Value = item.Reason;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        private (byte[], string) GenerateAbsenteeismPdf(IEnumerable<AbsenteeismDto> data, DateTime from, DateTime to)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    SetupPage(page, "Reporte de Ausentismo de Pacientes", from, to);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Fecha");
                            header.Cell().Element(CellStyle).Text("Paciente");
                            header.Cell().Element(CellStyle).Text("Estado");
                            header.Cell().Element(CellStyle).Text("Motivo");
                            static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Element(CellStyle).Text(item.Date.ToString("dd/MM/yyyy"));
                            table.Cell().Element(CellStyle).Text(item.PatientName);
                            table.Cell().Element(CellStyle).Text(item.Status);
                            table.Cell().Element(CellStyle).Text(item.Reason);
                            static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    });
                    SetupFooter(page);
                });
            });
            return (document.GeneratePdf(), "application/pdf");
        }
        #endregion

        #region Helpers
        private void SetupPage(PageDescriptor page, string title, DateTime from, DateTime to)
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Clínica Médica").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                    col.Item().Text(title).FontSize(16);
                    col.Item().Text($"Período: {from:dd/MM/yyyy} - {to:dd/MM/yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private void SetupFooter(PageDescriptor page)
        {
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ");
                x.CurrentPageNumber();
            });
        }
        #endregion
    }

    // DTOs
    public class MedicalProductivityDto
    {
        public string DoctorName { get; set; } = string.Empty;
        public int TotalAppointments { get; set; }
        public int Attended { get; set; }
        public int Cancelled { get; set; }
        public double AvgDurationMinutes { get; set; }
    }

    public class MorbidityDto
    {
        public string Diagnosis { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public int Age0to5 { get; set; }
        public int Age6to12 { get; set; }
        public int Age13to18 { get; set; }
        public int Age19to60 { get; set; }
        public int Age60Plus { get; set; }
    }

    public class LabVolumeDto
    {
        public string ExamType { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
    }

    public class AbsenteeismDto
    {
        public DateTime Date { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
