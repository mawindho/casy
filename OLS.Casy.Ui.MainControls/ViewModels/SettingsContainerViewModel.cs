using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using OLS.Casy.Core.Localization.Api;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(SettingsContainerViewModel))]
    public class SettingsContainerViewModel : DialogModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IEnumerable<ISettingsCategoryViewModel> _settingsCategoryViewModels;

        [ImportingConstructor]
        public SettingsContainerViewModel(
            ILocalizationService localizationService,
            [ImportMany(typeof(ISettingsCategoryViewModel))] IEnumerable<ISettingsCategoryViewModel> settingsCategoryViewModels)
        {
            _settingsCategoryViewModels = settingsCategoryViewModels;
            _localizationService = localizationService;

            SettingsCategories = new ObservableCollection<ISettingsCategoryViewModel>();
        }

        public ObservableCollection<ISettingsCategoryViewModel> SettingsCategories { get; }

        protected override void OnOk()
        {
            var result = true;
            foreach (var settingsCategoryViewModel in SettingsCategories)
            {
                result &= settingsCategoryViewModel.CanOk;
            }

            if (!result) return;

            foreach (var settingsCategoryViewModel in SettingsCategories)
            {
                settingsCategoryViewModel.OnOk();
            }
            base.OnOk();
        }

        protected override void OnCancel()
        {
            foreach (var settingsCategoryViewModel in SettingsCategories)
            {
                settingsCategoryViewModel.OnCancel();
            }
            base.OnCancel();
        }

        public void OnImportsSatisfied()
        {
            Title = _localizationService.GetLocalizedString("SettingsView_Header");
            if (_settingsCategoryViewModels.Any())
            {
                var active = _settingsCategoryViewModels.FirstOrDefault(s => s.Order == 0);
                if (active != null) active.IsActive = true;
            }

            var ordered = _settingsCategoryViewModels.OrderBy(s => s.Order);
            foreach (var item in ordered)
            {
                SettingsCategories.Add(item);
            }
        }
    }
}
