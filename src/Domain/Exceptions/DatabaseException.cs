using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Exceptions
{
    // Excepción base para errores relacionados con la base de datos
    public class DatabaseException : Exception
    {
        public string ErrorCode { get; }

        public DatabaseException(string message, string errorCode, Exception? innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    // Violación de restricción de unicidad (por ejemplo, nombre duplicado)
    public class DuplicateEntryException : DatabaseException
    {
        public DuplicateEntryException(string message, string errorCode = "DuplicateEntry")
            : base(message, errorCode)
        {
        }
    }

    // Violación de restricción de clave foránea
    public class ForeignKeyViolationException : DatabaseException
    {
        public ForeignKeyViolationException(string message, string errorCode = "ForeignKeyViolation")
            : base(message, errorCode)
        {
        }
    }

    // Recurso no encontrado en la base de datos
    public class NotFoundException : DatabaseException
    {
        public NotFoundException(string message, string errorCode = "NotFound")
            : base(message, errorCode)
        {
        }
    }

    // Error de concurrencia (por ejemplo, al intentar actualizar una entidad que ha cambiado)
    public class ConcurrencyException : DatabaseException
    {
        public ConcurrencyException(string message, string errorCode = "ConcurrencyError")
            : base(message, errorCode)
        {
        }
    }

    // Error de conexión a la base de datos
    public class DatabaseConnectionException : DatabaseException
    {
        public DatabaseConnectionException(string message, string errorCode = "DatabaseConnectionError")
            : base(message, errorCode)
        {
        }
    }

    // Error de tiempo de espera en la base de datos
    public class DatabaseTimeoutException : DatabaseException
    {
        public DatabaseTimeoutException(string message, string errorCode = "DatabaseTimeout")
            : base(message, errorCode)
        {
        }
    }

    // Error de validación de datos en la base de datos (por ejemplo, datos inválidos)
    public class DataValidationException : DatabaseException
    {
        public DataValidationException(string message, string errorCode = "DataValidationError")
            : base(message, errorCode)
        {
        }
    }

    // Error genérico para excepciones no manejadas específicamente
    public class GeneralDatabaseException : DatabaseException
    {
        public GeneralDatabaseException(string message, string errorCode = "GeneralDatabaseError", Exception? innerException = null)
            : base(message, errorCode, innerException)
        {
        }
    }
}
