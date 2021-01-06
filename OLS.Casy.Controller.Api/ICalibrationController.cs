using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.Api
{
    /// <summary>
    /// Interface for a controller class responsible for all calibration work.
    /// The calibration of the casy device depends on cappillary size and corresponding diameter.
    /// </summary>
    public interface ICalibrationController
    {
        uint MaxCounts { get; }

        /// <summary>
        /// Property returning all known cappillary sizes
        /// </summary>
        IEnumerable<int> KnownCappillarySizes { get; }

        /// <summary>
        /// Property returning corresponding diameters for the passed cappillary size
        /// </summary>
        /// <param name="cappillarySize">Cappillary size</param>
        /// <returns>Correxponding diameters</returns>
        IEnumerable<int> GetDiametersByCappillarySize(int cappillarySize);

        /// <summary>
        /// Returns the list of names of all known calibrations of the system
        /// </summary>
        IEnumerable<string> KnownCalibrationNames { get; }

        string TransferCalibration(IProgress<string> progress, MeasureSetup measureSetup, bool allowDefault);

        bool VerifyActiveCalibration(IProgress<string> progress);

        /// <summary>
        /// Event will be raised when all calibration data has been loaded and is available to use
        /// </summary>
        event EventHandler CaibrationDataLoadedEvent;

        double GetVolumeCorrection(MeasureSetup measureSetup);

        bool IsValidCalibratrion(int capillarySize, int diameter);
    }
}
