using System.Net;

namespace Wotr.Server;

// AUTH-02: the two account emails, in English like the game. Links point to the game's web pages, with the token
// in the fragment so it never reaches a server log or a Referer header.
internal static class AccountMail
{
    public const string Game = "War of the Ring Online";

    public static EmailMessage Verification(string username, string email, Uri link) => Message(email, "verify-email",
        $"Confirm your email for {Game}",
        $"Hello {username},",
        "Confirm this address for your War of the Ring Online account. You will need it to reset your password.",
        "Confirm email", link,
        "The link is valid for 7 days. If you did not create this account, ignore this message.");

    public static EmailMessage PasswordReset(string username, string email, Uri link) => Message(email, "reset-password",
        $"Reset your {Game} password",
        $"Hello {username},",
        "Someone asked to reset the password of your War of the Ring Online account. Choose a new password here:",
        "Choose a new password", link,
        "The link is valid for 60 minutes and works once. If it was not you, ignore this message; your password stays the same.");

    private static EmailMessage Message(string to, string kind, string subject, string greeting, string body, string action,
        Uri link, string footer)
    {
        var url = link.AbsoluteUri;
        var text = $"{greeting}\n\n{body}\n{url}\n\n{footer}\n\n{Game}\n";
        var html = $"<p>{Encode(greeting)}</p><p>{Encode(body)}</p>" +
                   $"<p><a href=\"{Encode(url)}\">{Encode(action)}</a></p>" +
                   $"<p style=\"color:#666;font-size:13px\">{Encode(footer)}<br>{Encode(url)}</p>" +
                   $"<p style=\"color:#666;font-size:13px\">{Encode(Game)}</p>";
        return new EmailMessage(to, subject, text, html, kind);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
