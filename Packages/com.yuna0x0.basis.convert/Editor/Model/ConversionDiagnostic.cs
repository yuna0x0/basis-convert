using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Model
{
    public enum DiagnosticSeverity
    {
        /// <summary>Mapped cleanly. Recorded so the report can show the whole picture.</summary>
        Mapped = 0,

        /// <summary>Mapped, but the result is a judgement call and should be checked by eye.</summary>
        Approximated = 1,

        /// <summary>The source setting has no equivalent and was not carried over.</summary>
        Dropped = 2,

        /// <summary>Something is wrong or suspicious about the source data.</summary>
        Warning = 3,
    }

    /// <summary>
    /// One thing the converter did, or refused to do, to one piece of source data. Codes are
    /// stable so the report can group by them and tests can assert on them without matching
    /// prose.
    /// </summary>
    public sealed class ConversionDiagnostic
    {
        public readonly DiagnosticSeverity Severity;
        public readonly string Code;
        public readonly string Message;

        public ConversionDiagnostic(DiagnosticSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public override string ToString() => $"[{Severity}] {Code}: {Message}";
    }

    public static class DiagnosticListExtensions
    {
        public static void Add(this List<ConversionDiagnostic> diagnostics,
            DiagnosticSeverity severity, string code, string message)
        {
            diagnostics.Add(new ConversionDiagnostic(severity, code, message));
        }

        public static bool HasCode(this IEnumerable<ConversionDiagnostic> diagnostics, string code)
        {
            foreach (ConversionDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Code == code)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
