using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Appinventario.Services
{
    public static class CrashReportService
    {
        private static readonly string LogDirectory = Path.Combine(FileSystem.AppDataDirectory, "Logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "CrashReport.log");

        public static string GetLogFilePath() => LogFilePath;

        private static readonly object FileLock = new object();

        /// <summary>
        /// Escribe el error de forma sincrona. Es la que debe usarse en los manejadores de
        /// excepciones no controladas: el proceso muere al salir del manejador, asi que una
        /// escritura asincrona sin esperar casi nunca llega a disco.
        /// </summary>
        public static void LogException(Exception exception, string source = null)
        {
            try
            {
                var message = BuildLogMessage(exception, source);
                lock (FileLock)
                {
                    Directory.CreateDirectory(LogDirectory);
                    File.AppendAllText(LogFilePath, message, Encoding.UTF8);
                }
            }
            catch
            {
                // If logging fails, do not crash the app.
            }
        }

        public static Task LogExceptionAsync(Exception exception, string source = null)
        {
            // Mismo camino sincrono (y el mismo lock) para que no compitan por el archivo.
            LogException(exception, source);
            return Task.CompletedTask;
        }

        private static string BuildLogMessage(Exception exception, string source)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================ Crashed on " + DateTime.UtcNow.ToString("u") + " ================");
            if (!string.IsNullOrWhiteSpace(source))
                sb.AppendLine("Source: " + source);

            var current = exception;
            var level = 0;

            while (current != null)
            {
                sb.AppendLine($"Exception Level {level}: {current.GetType().FullName}");
                sb.AppendLine("Message: " + current.Message);
                sb.AppendLine("StackTrace:");
                sb.AppendLine(current.StackTrace ?? "<no stack trace>");
                sb.AppendLine();
                current = current.InnerException;
                level++;
            }

            sb.AppendLine();
            return sb.ToString();
        }
    }
}
