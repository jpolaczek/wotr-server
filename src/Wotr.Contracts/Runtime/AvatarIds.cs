using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    // AVATAR-01 A1: the ten avatars an account may have, so the server validates and the client draws the same set.
    // A view's empty avatar names nobody (an empty seat); an unknown one, from a newer server, is drawn as the Eye.
    public static class AvatarIds
    {
        public const string Eye = "eye";
        public const string Ring = "ring";

        private static readonly string[] Known =
        {
            Eye, Ring, "gondor", "rohan", "elves", "dwarves", "north", "sauron", "isengard", "southrons"
        };

        public static IReadOnlyList<string> All => Known;

        public static bool IsKnown(string id) => Array.IndexOf(Known, id) >= 0;
    }
}
