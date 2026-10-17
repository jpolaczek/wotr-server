#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Wotr.Contracts
{
    // DIST-05: a client release version with SemVer 2.0 precedence. Build metadata is not part of an identity.
    public sealed class ClientVersion : IComparable<ClientVersion>, IEquatable<ClientVersion>
    {
        private const int MaximumLength = 64;
        private const string Identifier = @"(?:0|[1-9][0-9]*|[0-9]*[a-z-][0-9a-z-]*)";
        private static readonly Regex Format = new Regex(
            @"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-(" + Identifier + @"(?:\." + Identifier + @")*))?\z",
            RegexOptions.CultureInvariant);

        private readonly string[] prerelease;

        private ClientVersion(string text, int major, int minor, int patch, string[] prerelease)
        {
            Text = text;
            Major = major;
            Minor = minor;
            Patch = patch;
            this.prerelease = prerelease;
        }

        public string Text { get; }
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public bool IsPrerelease => prerelease.Length > 0;

        public static bool TryParse(string? text, [NotNullWhen(true)] out ClientVersion? version)
        {
            version = null;
            if (string.IsNullOrEmpty(text) || text.Length > MaximumLength)
                return false;
            var match = Format.Match(text);
            if (!match.Success)
                return false;
            if (!int.TryParse(match.Groups[1].Value, out var major) || !int.TryParse(match.Groups[2].Value, out var minor) ||
                !int.TryParse(match.Groups[3].Value, out var patch))
                return false;
            var labels = match.Groups[4].Success ? match.Groups[4].Value.Split('.') : Array.Empty<string>();
            version = new ClientVersion(text, major, minor, patch, labels);
            return true;
        }

        public static ClientVersion Parse(string text)
        {
            if (!TryParse(text, out var version))
                throw new FormatException("Client versions use MAJOR.MINOR.PATCH with an optional lowercase pre-release.");
            return version;
        }

        public int CompareTo(ClientVersion? other)
        {
            if (other is null)
                return 1;
            var core = Major != other.Major ? Major.CompareTo(other.Major)
                : Minor != other.Minor ? Minor.CompareTo(other.Minor)
                : Patch.CompareTo(other.Patch);
            return core != 0 ? core : ComparePrerelease(prerelease, other.prerelease);
        }

        // A release outranks its pre-releases; otherwise labels compare field by field.
        private static int ComparePrerelease(string[] left, string[] right)
        {
            if (left.Length == 0 || right.Length == 0)
                return right.Length.CompareTo(left.Length);
            for (var index = 0; index < Math.Min(left.Length, right.Length); index++)
            {
                var order = CompareLabel(left[index], right[index]);
                if (order != 0)
                    return order;
            }
            return left.Length.CompareTo(right.Length);
        }

        private static int CompareLabel(string left, string right)
        {
            var leftNumeric = IsNumeric(left);
            var rightNumeric = IsNumeric(right);
            if (leftNumeric && rightNumeric)
                return left.Length != right.Length ? left.Length.CompareTo(right.Length) : string.CompareOrdinal(left, right);
            if (leftNumeric != rightNumeric)
                return leftNumeric ? -1 : 1;
            return string.CompareOrdinal(left, right);
        }

        private static bool IsNumeric(string label)
        {
            foreach (var character in label)
                if (character < '0' || character > '9')
                    return false;
            return true;
        }

        public bool Equals(ClientVersion? other) => CompareTo(other) == 0;
        public override bool Equals(object? obj) => obj is ClientVersion other && Equals(other);
        public override int GetHashCode() => Text.GetHashCode();
        public override string ToString() => Text;
    }
}
