#nullable enable

namespace Wotr.Contracts
{
    public static class AccountInput
    {
        public const int MinimumUsernameLength = 3;
        public const int MaximumUsernameLength = 32;
        public const int MinimumPasswordLength = 12;
        public const int MaximumPasswordLength = 128;
        public const int MaximumEmailLength = 254;
        public const int MaximumEmailLocalLength = 64;

        public static bool ValidUsername(string? username)
        {
            if (username == null || username.Length < MinimumUsernameLength || username.Length > MaximumUsernameLength)
                return false;
            foreach (var character in username)
                if (!AsciiLetterOrDigit(character) && character != '_' && character != '-' && character != '.')
                    return false;
            return true;
        }

        public static bool ValidPassword(string? password) =>
            password != null && password.Length >= MinimumPasswordLength && password.Length <= MaximumPasswordLength;

        // AUTH-02: a plain shape check — one @, a dotted domain, no spaces or control characters. Delivery of the
        // confirmation link proves the address.
        public static bool ValidEmail(string? email)
        {
            if (email == null)
                return false;
            email = email.Trim();
            if (email.Length == 0 || email.Length > MaximumEmailLength)
                return false;
            var at = email.IndexOf('@');
            if (at < 1 || at > MaximumEmailLocalLength || at != email.LastIndexOf('@'))
                return false;
            var domain = email.Substring(at + 1);
            if (domain.Length < 3 || domain.IndexOf('.') < 1 || domain.StartsWith(".") || domain.EndsWith(".") || domain.Contains(".."))
                return false;
            foreach (var character in email)
                if (character <= ' ' || character == '\u007f' || character == ',' || character == ';' || character == '<' || character == '>')
                    return false;
            return true;
        }

        // The form the uniqueness check and login compare: trimmed, lower case.
        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static bool AsciiLetterOrDigit(char character) =>
            character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' ||
            character >= '0' && character <= '9';
    }
}
