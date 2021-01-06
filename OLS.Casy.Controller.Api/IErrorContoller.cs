using OLS.Casy.Models;
using System.Collections.Generic;

namespace OLS.Casy.Controller.Api
{
    /// <summary>
    /// Interface for a controller that can handle and translate response string of the casy device
    /// </summary>
    public interface IErrorContoller
    {
        /// <summary>
        /// Parses a response string from casy device and returns a corresponding <see cref="ErrorResult"/>
        /// </summary>
        /// <param name="casyResponse">The response of the casy device</param>
        /// <returns>Corresponing instance of <see cref="ErrorResult"/></returns>
        ErrorResult ParseError(string casyResponse);

        ErrorDetails[] ErrorDetails { get; }
    }
}
