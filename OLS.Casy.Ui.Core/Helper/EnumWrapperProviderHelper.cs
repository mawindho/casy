using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base.ViewModels;

namespace OLS.Casy.Ui.Core.Helper
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(EnumWrapperProviderHelper))]
    public class EnumWrapperProviderHelper : IPartImportsSatisfiedNotification
    {
        private readonly List<ComboBoxItemWrapperViewModel<AggregationCalculationModes>> _aggregationModeWrapper;
        private readonly ILocalizationService _localizationService;
        
        [ImportingConstructor]
        public EnumWrapperProviderHelper(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _aggregationModeWrapper = new List<ComboBoxItemWrapperViewModel<AggregationCalculationModes>>();
        }
        
        
        public IEnumerable<ComboBoxItemWrapperViewModel<AggregationCalculationModes>> GetAggregationModeWrapper(bool isOverlay)
        {
            if(_aggregationModeWrapper.Count == 0)
            {
                var aggregationCalculationModes = Enum.GetNames(typeof(AggregationCalculationModes));
                foreach (var aggregationCalculationMode in aggregationCalculationModes)
                {
                    var comboBoxWrapperItem = new ComboBoxItemWrapperViewModel<AggregationCalculationModes>(
                        (AggregationCalculationModes) Enum.Parse(typeof(AggregationCalculationModes),
                            aggregationCalculationMode))
                    {
                        DisplayItem = _localizationService.GetLocalizedString(
                            $"AggregationCalculationMode_{aggregationCalculationMode}_Name")
                    };

                    _aggregationModeWrapper.Add(comboBoxWrapperItem);
                }

                //if (aggregationCalculationMode != "FromParent" || this.IsOverlayMode)
                
            }

            if (!isOverlay)
            {
                return _aggregationModeWrapper.Where(w => w.ValueItem != AggregationCalculationModes.FromParent)
                    .AsEnumerable();
            }

            return _aggregationModeWrapper.AsEnumerable();
        }

        public void OnImportsSatisfied()
        {
            _localizationService.LanguageChanged += (sender, args) => _aggregationModeWrapper.Clear();
        }
    }
}