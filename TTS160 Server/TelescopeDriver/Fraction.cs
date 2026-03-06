namespace ASCOM.TTS160
{
    /// <summary>
    /// Simple integer fraction with numerator and denominator.
    /// Used by <see cref="Telescope.TelescopeHardware.RealToFraction"/> to convert
    /// decimal guide rates into the LX200 fractional format the mount expects.
    /// </summary>
    public struct Fraction
    {
        public Fraction(int n, int d)
        {
            N = n;
            D = d;
        }

        public int N { get; private set; }
        public int D { get; private set; }
    }
}
