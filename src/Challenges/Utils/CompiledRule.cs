using System.Globalization;

namespace Challenges.Utils
{
    /// <summary>Rule operators; parsed once at load by <see cref="ChallengeRules.ParseOperator"/>.</summary>
    internal enum RuleOperator : byte
    {
        Invalid,
        Equal,
        NotEqual,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual,
        BoolEqual,
        BoolNotEqual,
        Contains,
        NotContains,
    }

    /// <summary>
    /// One <see cref="ChallengeRule"/> prepared at load: operator, lookup key and value are parsed once so
    /// the per-event check only reads the current value. Immutable; shared with the progress worker.
    /// </summary>
    internal sealed class CompiledRule
    {
        public CompiledRule(ChallengeRule source, string key, RuleOperator op)
        {
            Source = source;
            Key = key;
            Op = op;
            Text = source.Value ?? string.Empty;
            Number = float.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)
                ? number
                : null;
            Bool = bool.TryParse(Text, out bool flag) ? flag : null;
            SkipWhenBotsAllowed = op == RuleOperator.BoolEqual
                && key.EndsWith(".isbot", StringComparison.Ordinal)
                && Text.Equals("false", StringComparison.OrdinalIgnoreCase);

            bool isAce = string.Equals(key, AceBounds.RuleKey, StringComparison.Ordinal);
            LookupKey = isAce ? AceBounds.DataKey(source) : key;
            RequiredType = isAce ? AceBounds.RequiredType : null;
        }

        /// <summary>The rule as loaded from YAML; its key is the one shown in debug output.</summary>
        public ChallengeRule Source { get; }
        /// <summary>Key the rule reads. Rebuild-time consumers (demand, gates, situations) use this.</summary>
        public string Key { get; }
        /// <summary>Event data key looked up per check; equals <see cref="Key"/> except for <c>isace.{min}.{max}</c>.</summary>
        public string LookupKey { get; }
        public RuleOperator Op { get; }
        /// <summary>Rule value as written; compared case-insensitively by the text operators.</summary>
        public string Text { get; }
        /// <summary>
        /// Rule value parsed with single precision like the numeric event values, widened exactly; null when
        /// the value is not a number (numeric operators then never match).
        /// </summary>
        public double? Number { get; }
        /// <summary>Rule value as a bool; null when it is neither <c>true</c> nor <c>false</c>.</summary>
        public bool? Bool { get; }
        /// <summary>A <c>{prefix}.isbot bool== false</c> rule; ignored while bots are allowed for testing.</summary>
        public bool SkipWhenBotsAllowed { get; }
        /// <summary>Challenge type the key is only valid for (<c>isace</c>: <c>player_kill</c>); null when any.</summary>
        public string? RequiredType { get; }
    }
}
