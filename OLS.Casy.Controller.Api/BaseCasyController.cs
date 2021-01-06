using OLS.Casy.Com.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Config.Api;

namespace OLS.Casy.Controller.Api
{
    /// <summary>
    /// ABstract base implementations for all controller using serial port to communicate with the casy sevice
    /// </summary>
    public abstract class BaseCasyController : AbstractService
    {
        private readonly ICasySerialPortDriver _casySerialPortDriver;

        /// <summary>
        /// COnstructor
        /// </summary>
        /// <param name="logger">Implementation of <see cref="ILogger"/></param>
        /// <param name="configService">Implementation of <see cref="IConfigService"/></param>
        /// <param name="casySerialPortDriver">Implementation of <see cref="ICasySerialPortDriver"/></param>
        public BaseCasyController(IConfigService configService, ICasySerialPortDriver casySerialPortDriver)
            : base(configService)
        {
            this._casySerialPortDriver = casySerialPortDriver;
        }

        /// <summary>
        /// Getter for the implementation of <see cref="ICasySerialPortDriver"/>
        /// </summary>
        public ICasySerialPortDriver CasySerialPortDriver { get { return _casySerialPortDriver; } }

    }
}
