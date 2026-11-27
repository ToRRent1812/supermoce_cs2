namespace Supermoce
{
    public static class HudPalette
    {
        public const int SemanticTones = 4;
        public const int SkillTones = 28;
        public const int TotalTones = SemanticTones + SkillTones;

        public const int Danger = 0;
        public const int Warn = 1;
        public const int Info = 2;
        public const int Success = 3;

        public static readonly string[] Hex =
        [
            "#E5484D", "#F5A524", "#3B82F6", "#30A46C",
            "#0E02E6", "#5D00FF", "#981912", "#FC0206",
            "#C52425", "#655050", "#7C2EE0", "#FF00AA",
            "#2667F3", "#3E842E", "#DA26D6", "#04C515",
            "#A68720", "#A666E6", "#E844F7", "#F26630",
            "#928E8E", "#48ADEC", "#FF5CD9", "#31E654",
            "#00EAFF", "#FF8C92", "#32EEB7", "#C0BEC6",
            "#B2EC37", "#5BF4FF", "#F6D81C", "#F1EBFF",
        ];

        public static string ClassFor(int tone) => $"tone-{Math.Clamp(tone, 0, TotalTones - 1)}";

        public static int NearestSkillTone(string? hex)
        {
            if (!TryParse(hex, out int r, out int g, out int b))
                return SemanticTones;

            int best = SemanticTones;
            int bestDistance = int.MaxValue;
            for (int i = SemanticTones; i < TotalTones; i++)
            {
                ParseKnown(i, out int tr, out int tg, out int tb);
                int distance = RedMean(r, g, b, tr, tg, tb);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private static bool TryParse(string? hex, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(hex) || hex.Length != 7 || hex[0] != '#')
                return false;
            return int.TryParse(hex.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out r)
                && int.TryParse(hex.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out g)
                && int.TryParse(hex.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out b);
        }

        private static void ParseKnown(int tone, out int r, out int g, out int b)
        {
            TryParse(Hex[tone], out r, out g, out b);
        }

        private static int RedMean(int r1, int g1, int b1, int r2, int g2, int b2)
        {
            int rMean = (r1 + r2) / 2;
            int r = r1 - r2;
            int g = g1 - g2;
            int b = b1 - b2;
            return (((512 + rMean) * r * r) >> 8) + 4 * g * g + (((767 - rMean) * b * b) >> 8);
        }
    }
}
