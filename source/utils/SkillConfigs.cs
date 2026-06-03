namespace Supermoce
{
    public class PassiveSkillConfig
    {
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int Step { get; set; } = 1;
        public Func<int, string>? CustomValueFormatter { get; set; }
        public string FormatValue(int value)
        {
            return CustomValueFormatter != null ? CustomValueFormatter(value) : value.ToString();
        }
    }

    public class ActiveSkillConfig
    {
        public int MinCooldown { get; set; } = 15;
        public int MaxCooldown { get; set; } = 50;
        public int CooldownStep { get; set; } = 5;
        public bool UseCustomHud { get; set; }
        public int GenerateCooldown()
        {
            int range = (MaxCooldown - MinCooldown) / CooldownStep + 1;
            return MinCooldown + (Supermoce.Instance?.Random.Next(range) ?? 0) * CooldownStep;
        }
    }
}
