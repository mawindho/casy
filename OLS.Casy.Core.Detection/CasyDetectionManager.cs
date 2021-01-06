using OLS.Casy.Core.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace OLS.Casy.Core.Detection
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ICasyDetectionManager))]
    public class CasyDetectionManager : ICasyDetectionManager
    {
        private Dictionary<string, CasyModel> _casyModels;

        public CasyDetectionManager()
        {
            _casyModels = new Dictionary<string, CasyModel>();
        }

        public IEnumerable<CasyModel> CasyModels => _casyModels.Values.ToList();

        public bool TryAddCasy(CasyModel casyModel)
        {
            if(!_casyModels.ContainsKey(casyModel.SerialNumber))
            {
                _casyModels.Add(casyModel.SerialNumber, casyModel);
                return true;
            }
            return false;
        }
    }
}
