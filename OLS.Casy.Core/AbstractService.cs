using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Core
{
    /// <summary>
    /// Base implementation of <see cref="IService"/> providing functionality needed for all
    /// further implementations
    /// </summary>
    public abstract class AbstractService : IService
    {
        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="logger">Implementation of <see cref="ILogger"/> </param>
        /// <param name="configService">Implementation of <see cref="IConfigService"/></param>
        [ImportingConstructor]
        protected AbstractService(IConfigService configService)
        {
            this.ConfigService = configService;
        }

        /// <summary>
        /// Property for getting the <see cref="IConfigService"/> implementation
        /// </summary>
        protected IConfigService ConfigService { get; private set; }

        /// <summary>
        ///     Pre-condition: MEF has satisfied all references.
        ///     This  method can be used to initialize the service and perform actions, which do
        ///     not expect other dependent services with OnReady state.
        /// </summary>
        public virtual void Prepare(IProgress<string> progress)
        {
            if (null != this.ConfigService)
            {
                this.ConfigService.InitializeByConfiguration(this);
            }
        }

        /// <summary>
        ///     The Deinitialize method is for cleaning up, storing and closing resouces.
        /// </summary>
        public virtual void Deinitialize(IProgress<string> progress)
        {
            if (null != this.ConfigService)
            {
                this.ConfigService.StoreToConfiguration(this);
            }
        }
    }
}
