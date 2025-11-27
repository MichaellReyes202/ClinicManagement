using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AuditLog;
public class AuditLogListDto
{
    public int Id { get; set; }
    public int? UserId { get; set; } // Id del usuario que realizó la acción
    public string UserEmail { get; set; } = string.Empty; // Email del usuario
    public AuditModuletype Module { get; set; } // Tipo de módulo al que pertenece la acción
    public ActionType ActionType { get; set; } // Tipo de acción realizada (Ej: CREATE, UPDATE, STATUS_CHANGE)
    public DateTime CreatedAtLocal { get; set; } // Fecha y hora de la acción,
    public AuditStatus Status { get; set; } //Estado de la operación (SUCCESS, FAILURE)
    public int? RecordId { get; set; } // ID del registro afectado 
    public string RecordDisplay { get; set; } = string.Empty; // Descripción corta del registro afectado
    public string ChangeDetail { get; set; } = string.Empty; // Detalles del cambio o razón de la acción
}
