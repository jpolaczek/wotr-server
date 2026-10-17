using System.Threading.Channels;

namespace Wotr.Server;

// AUTH-02: account endpoints queue their message and answer at once, so a reply takes as long for an unknown
// account as for a real one and Resend's latency never holds a request. One worker sends in order; a failed
// message is logged and dropped (the player asks for a new link).
public sealed class EmailOutbox(IEmailSender sender, ILogger<EmailOutbox> logger) : BackgroundService
{
    private readonly Channel<EmailMessage> queue = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(256) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public void Enqueue(EmailMessage message)
    {
        if (!queue.Writer.TryWrite(message))
            logger.LogWarning("The email queue is full; a {Kind} email was dropped.", message.Kind);
    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stopping))
        {
            try
            {
                await sender.Send(message, stopping);
            }
            catch (Exception error) when (!stopping.IsCancellationRequested)
            {
                logger.LogWarning("A {Kind} email could not be sent: {Error}.", message.Kind, error.GetType().Name);
            }
        }
    }
}
