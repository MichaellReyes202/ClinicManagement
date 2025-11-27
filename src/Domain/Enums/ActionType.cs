

namespace Domain.Enums;

public enum ActionType
{
    // 1. Acciones de Seguridad y Autorización
    AUTH_DENIED = 1,      // Intento de acceso fallido por rol o política
    LOGIN_SUCCESS = 2,    // Inicio de sesión exitoso
    LOGIN_FAILURE = 3,    // Intento de inicio de sesión fallido (credenciales)
    LOGOUT = 4,           // Cierre de sesión

    // 2. Acciones CRUD Generales
    CREATE = 10,
    UPDATE = 11,
    DELETE = 12,

    // 3. Acciones Específicas del Negocio
    STATUS_CHANGE = 20,   // Cambio de estado (Ej: Cita de Programada a Cancelada)
    REPORT_GENERATED = 21,
}
