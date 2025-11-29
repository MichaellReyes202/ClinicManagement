using Application.DTOs.Consultation;
using Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateConsultationReport(ConsultationDetailDto consultation)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(header => ComposeHeader(header, consultation));
                page.Content().Element(content => ComposeContent(content, consultation));
                page.Footer().Element(footer => ComposeFooter(footer));
            });
        })
        .GeneratePdf();
    }

    void ComposeHeader(IContainer container, ConsultationDetailDto consultation)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Clinica Management").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().Text("Salud y Gestión Eficiente").FontSize(10).FontColor(Colors.Grey.Medium);
                column.Item().Text(text =>
                {
                    text.Span("Doctor: ").SemiBold();
                    text.Span(consultation.DoctorName);
                });
                column.Item().Text(text =>
                {
                    text.Span("Fecha: ").SemiBold();
                    text.Span($"{consultation.CreatedAt:dd/MM/yyyy}");
                });
            });

            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Infrastructure.Assets.clinic.png");
            if (stream != null)
            {
                row.ConstantItem(100).Height(100).Image(stream);
            }
            else
            {
                 row.ConstantItem(100).Height(50).Placeholder();
            }
        });
    }

    void ComposeContent(IContainer container, ConsultationDetailDto consultation)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(20);

            // Patient Info
            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Información del Paciente").FontSize(16).SemiBold();
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Nombre: ").SemiBold();
                    text.Span(consultation.PatientName);
                });
                // Add Age/Sex if available in DTO, otherwise skip or add placeholders
            });

            // Consultation Details
            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Detalles de la Consulta").FontSize(16).SemiBold();
            
            column.Item().Text(text =>
            {
                text.Span("Motivo de Consulta: ").SemiBold();
                text.Span(consultation.Reason ?? "N/A");
            });

            column.Item().Text(text =>
            {
                text.Span("Diagnóstico: ").SemiBold();
                text.Span(consultation.Diagnosis ?? "N/A");
            });

            column.Item().Text(text =>
            {
                text.Span("Tratamiento / Notas: ").SemiBold();
                text.Span(consultation.TreatmentNotes ?? "N/A");
            });

            column.Item().Text(text =>
            {
                text.Span("Examen Físico: ").SemiBold();
                text.Span(consultation.PhysicalExam ?? "N/A");
            });

            // Prescriptions Table
            if (consultation.Prescriptions != null && consultation.Prescriptions.Any())
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Receta Médica").FontSize(16).SemiBold();
                
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Medicamento");
                        header.Cell().Element(CellStyle).Text("Dosis");
                        header.Cell().Element(CellStyle).Text("Frecuencia");
                        header.Cell().Element(CellStyle).Text("Duración");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var prescription in consultation.Prescriptions)
                    {
                        foreach (var item in prescription.Items)
                        {
                            table.Cell().Element(CellStyle).Text(item.MedicationName);
                            table.Cell().Element(CellStyle).Text(item.Dose);
                            table.Cell().Element(CellStyle).Text(item.Frequency);
                            table.Cell().Element(CellStyle).Text(item.Duration);

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(prescription.Notes))
                        {
                             table.Cell().ColumnSpan(4).Element(CellStyle).Text($"Nota: {prescription.Notes}").Italic().FontSize(10);
                             static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        }
                    }
                });
            }

            // Exams Table
            if (consultation.Exams != null && consultation.Exams.Any())
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Exámenes Solicitados").FontSize(16).SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(3); // Column for Results
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Examen");
                        header.Cell().Element(CellStyle).Text("Estado");
                        header.Cell().Element(CellStyle).Text("Resultados / Observaciones");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var exam in consultation.Exams)
                    {
                        table.Cell().Element(CellStyle).Text(exam.ExamTypeName);
                        table.Cell().Element(CellStyle).Text(exam.Status);
                        table.Cell().Element(CellStyle).Text(exam.Results ?? "Pendiente");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }
                });
            }
        });
    }

    void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text("Generado automáticamente por ClinicaManagement").FontSize(10).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        });
    }
}
