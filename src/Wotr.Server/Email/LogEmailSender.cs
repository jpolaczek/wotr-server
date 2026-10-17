namespace Wotr.Server;

// AUTH-02: Development only (EmailOptions refuses it elsewhere) — the whole message, link included, in the log.
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task Send(EmailMessage message, CancellationToken cancellation)
    {
        logger.LogInformation("Email ({Kind}) to {To}: {Subject}\n{Text}", message.Kind, message.To, message.Subject, message.Text);
        return Task.CompletedTask;
    }
}

// AUTH-02: delivery switched off (the default until Resend is configured).
public sealed class DisabledEmailSender(ILogger<DisabledEmailSender> logger) : IEmailSender
{
    public Task Send(EmailMessage message, CancellationToken cancellation)
    {
        logger.LogWarning("Email delivery is off (Wotr:Email:Provider); a {Kind} email was not sent.", message.Kind);
        return Task.CompletedTask;
    }
}
