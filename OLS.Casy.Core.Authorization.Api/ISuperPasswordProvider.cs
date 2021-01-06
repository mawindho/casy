using System;

namespace OLS.Casy.Core.Authorization.Api
{
    public interface ISuperPasswordProvider
    {
        string GenerateSessionId();
        string GenerateSuperPassword(string serialNumber, DateTime dateTime);
    }
}
