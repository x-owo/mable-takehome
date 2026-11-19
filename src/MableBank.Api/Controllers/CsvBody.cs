namespace MableBank.Api.Controllers;

internal static class CsvBody
{
    /// <summary>
    /// Reads the raw request body asynchronously (Kestrel forbids synchronous reads)
    /// and returns it as a reader the CSV parsers can consume.
    /// </summary>
    public static async Task<TextReader> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body);
        var csv = await reader.ReadToEndAsync(cancellationToken);
        return new StringReader(csv);
    }
}
