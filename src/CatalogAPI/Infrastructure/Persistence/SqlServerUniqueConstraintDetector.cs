using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Persistence
{
    internal static class SqlServerUniqueConstraintDetector
    {
        public static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            var baseException = exception.GetBaseException();

            if (baseException is SqlException sqlException)
                return sqlException.Number is 2601 or 2627;

            var message = baseException.Message;

            return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsUniqueConstraintViolation(
            DbUpdateException exception,
            string constraintName)
        {
            return IsUniqueConstraintViolation(exception) &&
                   exception.GetBaseException()
                       .Message
                       .Contains(constraintName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

