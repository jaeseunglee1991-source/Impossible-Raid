using System.Globalization;

namespace BossRaid.Utils
{
    public static class CurrencyFormatter
    {
        /// <summary>
        /// ?«ìžë¥??”í ?•ì‹ ë¬¸ìž?´ë¡œ ë³€??(?? 1234 -> "1,234")
        /// </summary>
        public static string ToCurrencyString(this double amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string ToCurrencyString(this float amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string ToCurrencyString(this int amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
