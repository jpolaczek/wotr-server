namespace Wotr.Server;

internal static class SaveFailureHandling
{
    public static void UseSaveFailureHandling(this WebApplication app) => app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (InvalidDataException error) when (error.Message is "CorruptSave" or "UnsupportedSaveVersion")
        {
            // Never include the snapshot, serializer path, session or exception in operational logs.
            app.Logger.LogWarning("Stored match cannot be opened: {Code}", error.Message);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = error.Message }, context.RequestAborted);
        }
    });
}
