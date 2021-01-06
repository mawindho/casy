namespace OLS.Casy.Core
{
    /// <summary>
    /// Helper class for binary swap operations
    /// </summary>
    public static class SwapHelper
    {
        /// <summary>
        /// Swaps the bytes of a <see cref="ushort"/>
        /// </summary>
        /// <param name="x"><see cref="ushort"/> to be swapped</param>
        /// <returns>Swapped <see cref="ushort"/></returns>
        public static ushort SwapBytes(ushort x)
        {
            return (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        /// <summary>
        /// Swaps the bytes of a <see cref="uint"/>
        /// </summary>
        /// <param name="x"><see cref="uint"/> to be swapped</param>
        /// <returns>Swapped <see cref="uint"/></returns>
        public static uint SwapBytes(uint x)
        {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        /// <summary>
        /// Swaps the bytes of a <see cref="int"/>
        /// </summary>
        /// <param name="x"><see cref="int"/> to be swapped</param>
        /// <returns>Swapped <see cref="int"/></returns>
        public static int SwapBytes(int x)
        {
            return (x & 0x000000FF) << 24 |
                    (x & 0x0000FF00) << 8 |
                    (x & 0x00FF0000) >> 8 |
                    ((int)(x & 0xFF000000)) >> 24;
        }
    }
}
