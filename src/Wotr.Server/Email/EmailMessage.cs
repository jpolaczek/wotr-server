namespace Wotr.Server;

// AUTH-02: one account message. Kind names it in logs, which never carry the address or the link.
public sealed record EmailMessage(string To, string Subject, string Text, string Html, string Kind);

public interface IEmailSender
{
    Task Send(EmailMessage message, CancellationToken cancellation);
}
