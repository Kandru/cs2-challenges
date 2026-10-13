using System.Globalization;

namespace Challenges.Utils
{
    public enum EventValueKind : byte
    {
        Text,
        Number,
        Bool,
    }

    /// <summary>
    /// One event data value as read from the game: a number, a bool or a text. Nothing is formatted when it is
    /// written; rules compare numbers and bools directly and format a number only where they compare its text
    /// (<see cref="TextEquals"/>, <see cref="TryTextContains"/>). <see cref="ToString"/> is the invariant text the
    /// value had when event data was a string dictionary.
    /// </summary>
    public readonly struct EventValue
    {
        /// <summary>Longest invariant text of any stored number (<c>-1.7976931348623157E+308</c>).</summary>
        private const int MaxNumberChars = 32;
        /// <summary>Integers up to this magnitude convert to double exactly.</summary>
        private const long ExactIntegerLimit = 1L << 53;

        private readonly string? _text;
        /// <summary>Integer, or the bits of the double / widened float, per <see cref="_format"/>.</summary>
        private readonly long _bits;
        private readonly EventValueKind _kind;
        private readonly NumberFormat _format;

        private EventValue(EventValueKind kind, NumberFormat format, long bits, string? text)
        {
            _kind = kind;
            _format = format;
            _bits = bits;
            _text = text;
        }

        /// <summary>Source type of a number; its text is formatted like that type.</summary>
        private enum NumberFormat : byte
        {
            None,
            Int64,
            UInt64,
            Single,
            Double,
        }

        public EventValueKind Kind => _kind;

        /// <summary>The number as double (exact except integers beyond 2^53); 0 unless <see cref="Kind"/> is <see cref="EventValueKind.Number"/>.</summary>
        public double Number =>
            _format switch
            {
                NumberFormat.Int64 => _bits,
                NumberFormat.UInt64 => (ulong)_bits,
                NumberFormat.Single or NumberFormat.Double => BitConverter.Int64BitsToDouble(_bits),
                _ => 0,
            };

        /// <summary>The flag; false unless <see cref="Kind"/> is <see cref="EventValueKind.Bool"/>.</summary>
        public bool Bool => _kind == EventValueKind.Bool && _bits != 0;

        /// <summary>The text; null unless <see cref="Kind"/> is <see cref="EventValueKind.Text"/>.</summary>
        public string? Text => _text;

        public static EventValue Of(bool value) => new(EventValueKind.Bool, NumberFormat.None, value ? 1 : 0, null);

        public static EventValue Of(int value) => new(EventValueKind.Number, NumberFormat.Int64, value, null);

        public static EventValue Of(uint value) => new(EventValueKind.Number, NumberFormat.Int64, value, null);

        public static EventValue Of(long value) => new(EventValueKind.Number, NumberFormat.Int64, value, null);

        public static EventValue Of(ulong value) =>
            new(EventValueKind.Number, NumberFormat.UInt64, unchecked((long)value), null);

        public static EventValue Of(float value) =>
            new(EventValueKind.Number, NumberFormat.Single, BitConverter.DoubleToInt64Bits(value), null);

        public static EventValue Of(double value) =>
            new(EventValueKind.Number, NumberFormat.Double, BitConverter.DoubleToInt64Bits(value), null);

        /// <summary>Text value; enum and other names are passed as their <c>ToString()</c>.</summary>
        public static EventValue Of(string? value) => new(EventValueKind.Text, NumberFormat.None, 0, value);

        /// <summary>
        /// Value as single precision number, exactly as <c>float.TryParse</c> (invariant) reads <see cref="ToString"/>.
        /// False for bools and for texts that are no number.
        /// </summary>
        public bool TryGetSingle(out float value)
        {
            switch (_format)
            {
                case NumberFormat.Single:
                    // Shortest round-trip text parses back to the same float.
                    value = (float)BitConverter.Int64BitsToDouble(_bits);
                    return true;
                case NumberFormat.Int64 when _bits is >= -ExactIntegerLimit and <= ExactIntegerLimit:
                    // Exact in double, then one correctly rounded step, like parsing the decimal text.
                    value = (float)(double)_bits;
                    return true;
                case NumberFormat.UInt64 when (ulong)_bits <= ExactIntegerLimit:
                    value = (float)(double)(ulong)_bits;
                    return true;
                case NumberFormat.None:
                    value = 0;
                    return _kind == EventValueKind.Text
                        && float.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }

            Span<char> buffer = stackalloc char[MaxNumberChars];
            int written = FormatNumber(buffer);
            return float.TryParse(buffer[..written], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Value as bool, exactly as <c>bool.TryParse</c> reads <see cref="ToString"/>; numbers never are.</summary>
        public bool TryGetBool(out bool value)
        {
            switch (_kind)
            {
                case EventValueKind.Bool:
                    value = _bits != 0;
                    return true;
                case EventValueKind.Text:
                    return bool.TryParse(_text, out value);
                default:
                    value = false;
                    return false;
            }
        }

        /// <summary><see cref="ToString"/> equals <paramref name="other"/> ignoring case; never allocates.</summary>
        public bool TextEquals(string other)
        {
            switch (_kind)
            {
                case EventValueKind.Text:
                    return string.Equals(_text, other, StringComparison.OrdinalIgnoreCase);
                case EventValueKind.Bool:
                    return string.Equals(BoolText, other, StringComparison.OrdinalIgnoreCase);
            }

            Span<char> buffer = stackalloc char[MaxNumberChars];
            int written = FormatNumber(buffer);
            return MemoryExtensions.Equals(buffer[..written], other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Whether <see cref="ToString"/> contains <paramref name="other"/> ignoring case; never allocates.
        /// False (no answer) for a null text.
        /// </summary>
        public bool TryTextContains(string other, out bool contains)
        {
            switch (_kind)
            {
                case EventValueKind.Text:
                    contains = _text != null && _text.Contains(other, StringComparison.OrdinalIgnoreCase);
                    return _text != null;
                case EventValueKind.Bool:
                    contains = BoolText.Contains(other, StringComparison.OrdinalIgnoreCase);
                    return true;
            }

            Span<char> buffer = stackalloc char[MaxNumberChars];
            int written = FormatNumber(buffer);
            contains = ((ReadOnlySpan<char>)buffer[..written]).Contains(other, StringComparison.OrdinalIgnoreCase);
            return true;
        }

        /// <summary>
        /// Invariant text: <c>true</c>/<c>false</c>, numbers as their source type formats them, texts as given
        /// (empty for null). Allocates; for debug output.
        /// </summary>
        public override string ToString()
        {
            switch (_kind)
            {
                case EventValueKind.Text:
                    return _text ?? string.Empty;
                case EventValueKind.Bool:
                    return BoolText;
            }

            Span<char> buffer = stackalloc char[MaxNumberChars];
            return new string(buffer[..FormatNumber(buffer)]);
        }

        private string BoolText => _bits != 0 ? "true" : "false";

        private int FormatNumber(Span<char> destination)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            int written;
            bool formatted = _format switch
            {
                NumberFormat.Int64 => _bits.TryFormat(destination, out written, default, culture),
                NumberFormat.UInt64 => ((ulong)_bits).TryFormat(destination, out written, default, culture),
                NumberFormat.Single => ((float)BitConverter.Int64BitsToDouble(_bits))
                    .TryFormat(destination, out written, default, culture),
                _ => BitConverter.Int64BitsToDouble(_bits).TryFormat(destination, out written, default, culture),
            };
            return formatted ? written : 0;
        }
    }
}
