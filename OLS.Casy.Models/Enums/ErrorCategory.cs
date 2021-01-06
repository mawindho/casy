namespace OLS.Casy.Models.Enums
{
    /// <summary>
    /// Format 0x0(PieceIndex)(SubstringIndex)(SubstringLength)
    /// </summary>
    public enum ErrorCategory : int
    {
        None = 0,
        Handling = 0x00020202,
        Hardware = 0x00020002,
        Memory = 0x00010301,
        PressureSystem = 0x00010003,
        Signal = 0x00000202,
        Communication = 0x00000101,
        Calibration = 0x00000002
    }
}
