namespace Wotr.Server;

public static class EmailSenders
{
    public static IEmailSender Create(IServiceProvider services)
    {
        var options = services.GetRequiredService<EmailOptions>();
        var logs = services.GetRequiredService<ILoggerFactory>();
        return options.Provider switch
        {
            EmailProvider.Resend => new ResendEmailSender(options, logs.CreateLogger<ResendEmailSender>()),
            EmailProvider.Log => new LogEmailSender(logs.CreateLogger<LogEmailSender>()),
            _ => new DisabledEmailSender(logs.CreateLogger<DisabledEmailSender>())
        };
    }
}
