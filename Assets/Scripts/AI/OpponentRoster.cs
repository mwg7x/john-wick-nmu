using UnityEngine;

namespace LastStep.AI
{
    // Original prototype identities. The roster is ordered so a tier can be played as a campaign.
    public static class OpponentRoster
    {
        public readonly struct Entry
        {
            public readonly string Name;
            public readonly Color Skin;
            public readonly Color Suit;
            public readonly float Height;
            public readonly float Width;
            public Entry(string name, Color skin, Color suit, float height, float width)
            { Name = name; Skin = skin; Suit = suit; Height = height; Width = width; }
        }

        private static readonly Entry[][] Tiers =
        {
            new[] {
                new Entry("Ahmed Allam", new Color(.29f,.16f,.11f), new Color(.12f,.17f,.21f), 1f, 1f),
                new Entry("Mos3d", new Color(.83f,.63f,.49f), new Color(.23f,.25f,.29f), .86f, .86f) },
            new[] {
                new Entry("Mohie", new Color(.63f,.41f,.28f), new Color(.15f,.20f,.22f), 1.04f, 1.02f),
                new Entry("Ziad Soliman", new Color(.72f,.49f,.34f), new Color(.22f,.18f,.25f), 1.07f, .94f),
                new Entry("Omriya", new Color(.54f,.34f,.24f), new Color(.27f,.24f,.21f), .95f, 1.06f) },
            new[] {
                new Entry("Feqy", new Color(.66f,.44f,.30f), new Color(.16f,.16f,.19f), 1.02f, 1.12f),
                new Entry("Ahmed Ehab", new Color(.70f,.48f,.35f), new Color(.11f,.15f,.19f), 1.10f, .96f),
                new Entry("Ahmed Tarek", new Color(.60f,.39f,.27f), new Color(.20f,.17f,.17f), 1f, 1.03f) },
            new[] {
                new Entry("Mohamed Waleed", new Color(.65f,.43f,.30f), new Color(.10f,.10f,.13f), 1.08f, 1.07f),
                new Entry("Bashmohandesa Mirna", new Color(.75f,.52f,.39f), new Color(.16f,.15f,.22f), .98f, .88f),
                new Entry("Tarek Amr", new Color(.57f,.36f,.25f), new Color(.12f,.14f,.15f), 1.12f, 1.12f) }
        };
        public static int TierCount => Tiers.Length;
        public static int Count(int tier) => Tiers[Mathf.Clamp(tier, 0, TierCount - 1)].Length;
        public static Entry Get(int tier, int index)
        {
            var entries = Tiers[Mathf.Clamp(tier, 0, TierCount - 1)];
            return entries[Mathf.Clamp(index, 0, entries.Length - 1)];
        }
    }
}
