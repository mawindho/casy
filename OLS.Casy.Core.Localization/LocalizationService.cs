using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.IO;
using System.Reflection;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Localization.Api;
using System.Resources;
using System.Text.RegularExpressions;
using System.Collections;
using System.Windows.Input;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Localization
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ILocalizationService))]
    public class LocalizationService : ILocalizationService, IPartImportsSatisfiedNotification
    {
        private const string ASSEMBLY_SEARCH_PATTERN_DLL = "OLS.Casy.*.dll";
        private const string ASSEMBLY_SEARCH_PATTERN_EXE = "OLS.Casy.*.exe";
        private static readonly CultureInfo DEFAULT_CULTURE_INFO = CultureInfo.GetCultureInfo("en-US");

        private readonly IEnumerable<IAssetsProvider> _assetsProvider;
        private readonly List<CultureInfo> _possibleLanguages = new List<CultureInfo>();
        private readonly Dictionary<string, object> _resourceMap = new Dictionary<string, object>();
        private CultureInfo _currentCulture;
        //private CultureInfo _currentKeyboardCulture;
        private IEventAggregatorProvider _eventAggregatorProvider;

        /// <summary>
        ///     Importing constructor.
        /// </summary>
        [ImportingConstructor]
        public LocalizationService([ImportMany] IEnumerable<IAssetsProvider> assetsProvider,
            IEventAggregatorProvider eventAggregatorProvider)
        {
            this._currentCulture = System.Windows.Application.Current.Dispatcher.Thread.CurrentCulture;
            this._assetsProvider = assetsProvider;
            this._eventAggregatorProvider = eventAggregatorProvider;
            this.CurrentCulture = DEFAULT_CULTURE_INFO;
        }

        #region ILocalizationService Members

        /// <summary>
        ///     Language changed event definition.
        /// </summary>
        public event EventHandler<LocalizationEventArgs> LanguageChanged;

        /// <summary>
        ///     Getter for list of possible languages.
        /// </summary>
        public IEnumerable<CultureInfo> PossibleLanguages
        {
            get { return this._possibleLanguages.AsEnumerable(); }
        }

        /// <summary>
        ///     Getter for current application culture.
        /// </summary>
        public CultureInfo CurrentCulture
        {
            get { return this._currentCulture; }
            set
            {
                if (!this._currentCulture.Equals(value))
                {
                    /*Task.Factory.StartNew(() =>
                    {
                        var awaiter = new System.Threading.ManualResetEvent(false);

                        ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Title = "ChangeLanguageInfo_Title",
                            Message = "ChangeLanguageInfo_Content"
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                        if (awaiter.WaitOne())
                        {
                            return;
                        }
                    });*/

                    this._currentCulture = value;
                    Thread.CurrentThread.CurrentCulture = this._currentCulture;

                    this.RefreshRessource();
                    this.RaiseLanguageChanged();
                }
            }
        }

        /*
        public CultureInfo CurrentKeyboardCulture
        {
            get { return this._currentKeyboardCulture; }
            set
            {
                if (_currentKeyboardCulture == null || !this._currentKeyboardCulture.Equals(value))
                {
                    this._currentKeyboardCulture = value;

                    if (_currentKeyboardCulture != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            InputLanguageManager.Current.CurrentInputLanguage = this._currentKeyboardCulture;
                            RefreshRessource();
                        });
                    }
                }
            }
        }
        */

        /// <summary>
        ///     Method tries to retrieve localization.
        /// </summary>
        /// <param name="localizationKey">Element identfier.</param>
        /// <param name="localizedObject">Localized Object</param>
        /// <param name="parameter">Parameter of localized object.</param>
        /// <returns></returns>
        public bool TryGetLocalizedObject(string localizationKey, out object localizedObject, params object[] parameter)
        {
            object result;
            _resourceMap.TryGetValue(localizationKey, out result);

            if(result != null)
            {
                localizedObject = result;

                if (localizedObject is string && parameter != null && parameter.Any())
                {
                    localizedObject = string.Format(this._currentCulture, (string)localizedObject, parameter);
                }
            }
            else
            {
                if (parameter != null && parameter.Any())
                {
                    localizedObject = string.Format(this._currentCulture, localizationKey, parameter);
                }
                else
                {
                    localizedObject = localizationKey;
                }
            }

            return true;
        }

        public string GetLocalizedString(string localizationKey, params string[] parameter)
        {
            object result;
            this.TryGetLocalizedObject(localizationKey, out result, parameter);
            return result as string;
        }

        #endregion

        #region IPartImportsSatisfiedNotification Members

        /// <summary>
        ///     <see cref="IPartImportsSatisfiedNotification.OnImportsSatisfied" />
        /// </summary>
        public void OnImportsSatisfied()
        {
            //System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //{
            //    this.CurrentKeyboardCulture = InputLanguageManager.Current.CurrentInputLanguage;
            //});

            this.DeterminateLanguageList();
            this.RefreshRessource();
        }

        #endregion

        
        /// <summary>
        ///     This method propagates the Current Culture to every GUI project assembly by using the AssetsProvider classes.
        /// </summary>
        private void RefreshRessource()
        {
            _resourceMap.Clear();

            string executionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (null == executionDir)
            {
                return;
            }

            string[] dllAssemblies = Directory.GetFiles(executionDir, ASSEMBLY_SEARCH_PATTERN_DLL, SearchOption.TopDirectoryOnly);
            string[] exeAssemblies = Directory.GetFiles(executionDir, ASSEMBLY_SEARCH_PATTERN_EXE, SearchOption.TopDirectoryOnly);

            foreach (var assemblyPath in exeAssemblies)
            {
                MapResourceEntries(assemblyPath);
            }

            foreach (var assemblyPath in dllAssemblies)
            {
                MapResourceEntries(assemblyPath);
            }

            foreach (IAssetsProvider assetsProvider in this._assetsProvider)
            {
                assetsProvider.SetCurrentCulture(this._currentCulture);
                //assetsProvider.SetCurrentKeybordCulture(this._currentKeyboardCulture);
            }
        }

        private void MapResourceEntries(string assemblyPath)
        {
            Assembly assembly = Assembly.LoadFile(assemblyPath);

            var assemblyNameObject = new AssemblyName(assembly.FullName);

            var resourceNames = assembly.GetManifestResourceNames();
            string sPattern = string.Format("{0}.Assets.Resources.ApplicationStrings_{1}", assemblyNameObject.Name, _currentCulture.Name);

            foreach (var resourceName in resourceNames)
            {
                if (Regex.IsMatch(resourceName, sPattern))
                {
                    var resourceManager = GetResourceManager(assembly);

                    var resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);

                    foreach (DictionaryEntry entry in resourceSet)
                    {
                        _resourceMap.Add(entry.Key.ToString(), entry.Value);
                    }
                }
            }
        }
        

        /// <summary>
        ///     Searches all subdirectories in the startup path, which contains an assambly
        /// </summary>
        private void DeterminateLanguageList()
        {
            this._possibleLanguages.Clear();

            var assembly = Assembly.GetEntryAssembly();
            var assemblyNameObject = new AssemblyName(assembly.FullName);

            var resourceNames = assembly.GetManifestResourceNames();
            string sPattern = string.Format("{0}.Assets.Resources.ApplicationStrings_*-*", assemblyNameObject.Name);

            foreach (var resourceName in resourceNames)
            {
                if(Regex.IsMatch(resourceName, sPattern))
                {
                    var cultureName = resourceName.Replace(string.Format("{0}.Assets.Resources.ApplicationStrings_", assemblyNameObject.Name), "");
                    cultureName = cultureName.Replace(".resources", "");
                    _possibleLanguages.Add(new CultureInfo(cultureName));
                }
            }
        }

        private void RaiseLanguageChanged()
        {
            if (this.LanguageChanged != null)
            {
                this.LanguageChanged.Invoke(this, new LocalizationEventArgs());
            }
        }

        private ResourceManager GetResourceManager(Assembly assembly)
        {
            string cultureName = string.Format("_{0}", _currentCulture.Name);
            var assemblyNameObject = new AssemblyName(assembly.FullName);

            // Der Assembly name der Ressourcedateien endet immer mit "_[CultureName]". (Z.B. ApplicationString_de-De)
            string assemblyName = string.Format("{0}.Assets.Resources.ApplicationStrings{1}", assemblyNameObject.Name, cultureName);
            return new ResourceManager(assemblyName, assembly);
        }

        public DateTime ParseString(string s)
        {
            DateTime outDate;
            if (DateTime.TryParse(s, out outDate))
            {
                return outDate;
            }

            foreach (var ci in this.PossibleLanguages)
            {
                if(DateTime.TryParse(s, ci, DateTimeStyles.None, out outDate))
                {
                    return outDate;
                }
            }

            return DateTime.Now;
        }
    }
}
