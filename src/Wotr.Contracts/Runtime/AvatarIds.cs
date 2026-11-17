using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    // AVATAR-01 A1: the avatars an account may have, so the server validates and the client draws the same set. AVATAR-03
    // (owner, 2026-11-17): the eight nations' soldiers only. A view's empty avatar names nobody (an empty seat); an unknown
    // one, from a newer server, is drawn as the Eye.
    public static class AvatarIds
    {
        // AVATAR-03: retired as account avatars (the two were confused with each other); kept for the clients that draw
        // them, and the Eye as a client's picture for an id it does not know.
        public const string Eye = "eye";
        public const string Ring = "ring";

        private static readonly string[] Known =
        {
            "gondor", "rohan", "elves", "dwarves", "north", "sauron", "isengard", "southrons"
        };

        public static IReadOnlyList<string> All => Known;

        public static bool IsKnown(string id) => Array.IndexOf(Known, id) >= 0;
    }
}
