namespace Wotr.Server;

// Commands the operator runs instead of the server (infra/README.md): --migrate, and since AUTH-02
// --set-email <username> <address>, which gives an account from before AUTH-02 its address and sends the
// confirmation link. The host never sees these words or their values.
public static class OperatorCommands
{
    private const string SetEmailCommand = "--set-email";

    public static (string Username, string Email)? SetEmail(string[] args)
    {
        var index = Array.IndexOf(args, SetEmailCommand);
        if (index < 0)
            return null;
        if (index + 2 >= args.Length)
            throw new ArgumentException("Use --set-email <username> <email>.");
        return (args[index + 1], args[index + 2]);
    }

    public static string[] HostArguments(string[] args)
    {
        var index = Array.IndexOf(args, SetEmailCommand);
        var rest = index < 0 ? args : args.Take(index).Concat(args.Skip(index + 3));
        return rest.Where(arg => arg is not "--migrate").ToArray();
    }

    public static async Task<int> RunSetEmail(IServiceProvider services, string username, string email)
    {
        var (result, message) = await services.GetRequiredService<AccountService>().SetEmail(username, email);
        switch (result)
        {
            case SetEmailResult.NoAccount:
                Console.Error.WriteLine($"No account named {username}.");
                return 1;
            case SetEmailResult.InvalidEmail:
                Console.Error.WriteLine("That is not a valid email address.");
                return 1;
            case SetEmailResult.EmailTaken:
                Console.Error.WriteLine("Another account already uses that address.");
                return 1;
        }
        if (message is null)
        {
            Console.WriteLine($"Address set for {username}; the email limit stopped the confirmation link, ask for it later.");
            return 0;
        }
        await services.GetRequiredService<IEmailSender>().Send(message, CancellationToken.None);
        Console.WriteLine($"Address set for {username}; confirmation link sent (provider: {services.GetRequiredService<EmailOptions>().Provider}).");
        return 0;
    }
}
