using System.Reflection;

namespace WorkBoard.Api.Extensions;

/// <summary>The running assembly's version, used as the OpenTelemetry service version.</summary>
/// <remarks>
/// A tiny type in its own file rather than a helper tacked onto the end of <c>Program.cs</c>. It is
/// read once at startup, and having it here keeps the composition root free of anything that is not
/// composition.
/// </remarks>
internal static class ThisAssembly
{
    /// <summary>Assembly version, or <c>0.0.0</c> when the assembly carries none.</summary>
    public static string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
}
