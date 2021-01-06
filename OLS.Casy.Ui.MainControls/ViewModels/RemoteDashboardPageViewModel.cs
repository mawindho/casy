using Newtonsoft.Json;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.Dtos;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using Polly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OLS.Casy.Base;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDashboardPageViewModel))]
    public class RemoteDashboardPageViewModel : ViewModelBase, IDashboardPageViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IEnvironmentService _environmentService;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly ICasyDetectionManager _casyDetectionManager;

        private string _casyWebServiceUrl;
        private ComboBoxItemWrapperViewModel<string> _selectedRemoteExperiment;
        private ComboBoxItemWrapperViewModel<string> _selectedRemoteGroup;
        private bool _isConnected;
        private CasyModel _selectedCasyModel;

        [ImportingConstructor]
        public RemoteDashboardPageViewModel(IEventAggregatorProvider eventAggregatorProvider,
            IEnvironmentService environmentService,
            IMeasureResultManager measureResultManager,
            ICasyDetectionManager casyDetectionManager)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _environmentService = environmentService;
            _measureResultManager = measureResultManager;
            _casyDetectionManager = casyDetectionManager;

            RemoteExperiments = new ObservableCollection<ComboBoxItemWrapperViewModel<string>>();
            RemoteGroups = new ObservableCollection<ComboBoxItemWrapperViewModel<string>>();
            RemoteMeasureResults = new ObservableCollection<ComboBoxItemWrapperViewModel<int>>();
        }

        public int Order => 1;

        //public string CasyWebServiceUrl
        //{
            //get => _casyWebServiceUrl;
            //set
            //{
                //if (value != _casyWebServiceUrl)
                //{
                    //_casyWebServiceUrl = value;
                    //NotifyOfPropertyChange();
                    //NotifyOfPropertyChange("HasValidUrl");
                //}
            //}
        //}

        public bool HasValidUrl
        {
            get
            {
                return SelectedCasyModel != null && !string.IsNullOrEmpty(SelectedCasyModel.IpAddress);
                //Uri uriResult;
                //return Uri.TryCreate(_casyWebServiceUrl, UriKind.Absolute, out uriResult)
                    //&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }
        }

        public ICommand ConnectCommand => new OmniDelegateCommand(OnConnect);

        public IEnumerable<CasyModel> CasyModels => _casyDetectionManager.CasyModels;
        public CasyModel SelectedCasyModel
        {
            get { return _selectedCasyModel; }
            set
            {
                _selectedCasyModel = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("HasValidUrl");
            }
        }

        private async void OnConnect()
        {
            _environmentService.SetEnvironmentInfo("IsBusy", true);

            Application.Current.Dispatcher.Invoke(() =>
            {
                RemoteExperiments.Clear();
                RemoteGroups.Clear();
                RemoteMeasureResults.Clear();
            });

            using (var handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var httpClient = new HttpClient(handler))
                {
                    var byteArray = Encoding.ASCII.GetBytes("casy:c4sy");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    var url = $"http://{SelectedCasyModel.IpAddress}:8536/";

                    var response = await Policy.Handle<HttpRequestException>().OrResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2), (result, timeSpan, retryCount, context) =>
                        {
                        })
                        .ExecuteAsync(() => httpClient.GetAsync($"{url}measureresults"));

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        var experiments = JsonConvert.DeserializeObject<IEnumerable<string>>(responseString);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var experiment in experiments)
                            {
                                if (experiment == null)
                                {
                                    RemoteExperiments.Add(new ComboBoxItemWrapperViewModel<string>("null") { DisplayItem = "[No Experiment]" });
                                }
                                RemoteExperiments.Add(new ComboBoxItemWrapperViewModel<string>(experiment) { DisplayItem = experiment });
                            }
                        });

                        _isConnected = true;
                        NotifyOfPropertyChange("IsConnected");
                    }
                    else
                    {
                        _isConnected = false;
                        NotifyOfPropertyChange("IsConnected");

                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new ManualResetEvent(false);

                            var showMessageBoxEventWrapper = new ShowMessageBoxDialogWrapper
                            {
                                Awaiter = awaiter,
                                Message = "Unable to connect to CASY remote web service",
                                Title = "Connection failed"
                            };

                            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(showMessageBoxEventWrapper);

                            awaiter.WaitOne();
                        });
                    }
                }
            }
            _environmentService.SetEnvironmentInfo("IsBusy", false);
        }

        public bool IsConnected
        {
            get => _isConnected;
        }

        public ObservableCollection<ComboBoxItemWrapperViewModel<string>> RemoteExperiments { get; }
        public ComboBoxItemWrapperViewModel<string> SelectedRemoteExperiment
        {
            get => _selectedRemoteExperiment;
            set
            {
                if (value != _selectedRemoteExperiment)
                {
                    _selectedRemoteExperiment = value;
                    NotifyOfPropertyChange();

                    if (_selectedRemoteExperiment != null)
                    {
                        LoadGroups();
                    }
                }
            }
        }

        private async void LoadGroups()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RemoteGroups.Clear();
                RemoteMeasureResults.Clear();
            });

            _environmentService.SetEnvironmentInfo("IsBusy", true);
            using (var handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var httpClient = new HttpClient(handler))
                {
                    var byteArray = Encoding.ASCII.GetBytes("casy:c4sy");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    var url = $"http://{SelectedCasyModel.IpAddress}:8536/";
                    if (!url.EndsWith("/"))
                    {
                        url += "/";
                    }

                    var response = await Policy.HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2), (result, timeSpan, retryCount, context) =>
                        {
                        })
                        .ExecuteAsync(() => httpClient.GetAsync($"{url}measureresults/{Uri.EscapeUriString(SelectedRemoteExperiment.ValueItem)}"));

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        var groups = JsonConvert.DeserializeObject<IEnumerable<string>>(responseString);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var group in groups)
                            {
                                if (group == null)
                                {
                                    RemoteGroups.Add(new ComboBoxItemWrapperViewModel<string>("null") { DisplayItem = "[No Group]" });
                                }
                                RemoteGroups.Add(new ComboBoxItemWrapperViewModel<string>(group) { DisplayItem = group });
                            }
                        });
                    }
                    else
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new ManualResetEvent(false);

                            var showMessageBoxEventWrapper = new ShowMessageBoxDialogWrapper
                            {
                                Awaiter = awaiter,
                                Message = "Unable to connect to CASY remote web service",
                                Title = "Connection failed"
                            };

                            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(showMessageBoxEventWrapper);

                            awaiter.WaitOne();
                        });
                        _isConnected = false;
                        NotifyOfPropertyChange("IsConnected");
                    }
                }
            }
            _environmentService.SetEnvironmentInfo("IsBusy", false);
        }

        public ObservableCollection<ComboBoxItemWrapperViewModel<string>> RemoteGroups { get; }
        public ComboBoxItemWrapperViewModel<string> SelectedRemoteGroup
        {
            get => _selectedRemoteGroup;
            set
            {
                if (value != _selectedRemoteGroup)
                {
                    _selectedRemoteGroup = value;
                    NotifyOfPropertyChange();

                    if (_selectedRemoteGroup != null)
                    {
                        LoadMeasureResults();
                    }
                }
            }
        }

        private async void LoadMeasureResults()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RemoteMeasureResults.Clear();
            });

            _environmentService.SetEnvironmentInfo("IsBusy", true);
            using (var handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var httpClient = new HttpClient(handler))
                {
                    var byteArray = Encoding.ASCII.GetBytes("casy:c4sy");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    var url = $"http://{SelectedCasyModel.IpAddress}:8536/";
                    if (!url.EndsWith("/"))
                    {
                        url += "/";
                    }

                    var response = await Policy.HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2), (result, timeSpan, retryCount, context) =>
                        {
                        })
                        .ExecuteAsync(() => httpClient.GetAsync($"{url}measureresults/{Uri.EscapeUriString(SelectedRemoteExperiment.ValueItem)}/{Uri.EscapeUriString(SelectedRemoteGroup.ValueItem)}"));

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        var measureResults = JsonConvert.DeserializeObject<IEnumerable<MeasureResulltInfoDto>>(responseString);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var measureResult in measureResults)
                            {
                                RemoteMeasureResults.Add(new ComboBoxItemWrapperViewModel<int>(measureResult.Id) { DisplayItem = measureResult.Name });
                            }
                        });
                    }
                    else
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new ManualResetEvent(false);

                            var showMessageBoxEventWrapper = new ShowMessageBoxDialogWrapper
                            {
                                Awaiter = awaiter,
                                Message = "Unable to connect to CASY remote web service",
                                Title = "Connection failed"
                            };

                            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(showMessageBoxEventWrapper);

                            awaiter.WaitOne();
                        });
                        _isConnected = false;
                        NotifyOfPropertyChange("IsConnected");
                    }
                }
            }
            _environmentService.SetEnvironmentInfo("IsBusy", false);
        }

        public ObservableCollection<ComboBoxItemWrapperViewModel<int>> RemoteMeasureResults { get; }

        public ICommand MeasureResultDoubleClickCommand => new OmniDelegateCommand<ComboBoxItemWrapperViewModel<int>>(OnMeasureResultDoubleClick);

        private async void OnMeasureResultDoubleClick(ComboBoxItemWrapperViewModel<int> selectedMeasurement)
        {
            _environmentService.SetEnvironmentInfo("IsBusy", true);
            using (var handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var httpClient = new HttpClient(handler))
                {
                    var byteArray = Encoding.ASCII.GetBytes("casy:c4sy");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    var url = $"http://{SelectedCasyModel.IpAddress}:8536/";
                    if (!url.EndsWith("/"))
                    {
                        url += "/";
                    }

                    var response = await Policy.HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2), (result, timeSpan, retryCount, context) =>
                        {
                        })
                        .ExecuteAsync(() => httpClient.GetAsync($"{url}measureresults/{SelectedRemoteExperiment.ValueItem}/{SelectedRemoteGroup.ValueItem}/{selectedMeasurement.DisplayItem}"));

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        var measureResultDto = JsonConvert.DeserializeObject<MeasureResultDto>(responseString);

                        var colorName = "ChartColor1";
                        var measureResult = new MeasureResult()
                        {
                            Color = ((SolidColorBrush)Application.Current.Resources[colorName]).Color.ToString(),
                            IsReadOnly = true,
                            Comment = measureResultDto.Comment,
                            CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultDto.CreatedAt),
                            CreatedBy = measureResultDto.CreatedBy,
                            Experiment = measureResultDto.Experiment,
                            Group = measureResultDto.Group,
                            IsCfr = measureResultDto.IsCfr,
                            LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultDto.LastModifiedAt),
                            LastModifiedBy = measureResultDto.LastModifiedBy,
                            LastWeeklyClean = DateTimeOffsetExtensions.ParseAny(measureResultDto.LastWeeklyClean),
                            MeasuredAt = measureResultDto.MeasuredAt,
                            MeasuredAtTimeZone = measureResultDto.MeasuredAtTimeZone,
                            MeasureResultGuid = measureResultDto.MeasureResultGuid,
                            Name = measureResultDto.Name,
                            Origin = measureResultDto.Origin,
                            SerialNumber = measureResultDto.SerialNumber,
                            IsTemporary = false
                        };

                        foreach (var dataItem in measureResultDto.MeasureResultDataItems)
                        {
                            measureResult.MeasureResultDatas.Add(new MeasureResultData()
                            {
                                AboveCalibrationLimitCount = dataItem.AboveCalibrationLimitCount,
                                BelowCalibrationLimitCount = dataItem.BelowCalibrationLimitCount,
                                BelowMeasureLimtCount = dataItem.BelowMeasureLimitCount,
                                ConcentrationTooHigh = dataItem.ConcentrationTooHigh,
                                InternalDataBlock = dataItem.DataBlock,
                                MeasureResult = measureResult
                            });
                        }

                        foreach (var auditTrailItem in measureResultDto.AuditTrailItems)
                        {
                            measureResult.AuditTrailEntries.Add(new AuditTrailEntry()
                            {
                                Action = auditTrailItem.Action,
                                ComputerName = auditTrailItem.ComputerName,
                                DateChanged = DateTimeOffsetExtensions.ParseAny(auditTrailItem.DateChanged),
                                EntityName = auditTrailItem.EntityName,
                                MeasureResult = measureResult,
                                NewValue = auditTrailItem.NewValue,
                                OldValue = auditTrailItem.OldValue,
                                PrimaryKeyValue = auditTrailItem.PrimaryKeyValue,
                                PropertyName = auditTrailItem.PropertyName,
                                SoftwareVersion = auditTrailItem.SoftwareVersion,
                                UserChanged = auditTrailItem.UserChanged
                            });
                        }

                        measureResult.MeasureSetup = new MeasureSetup()
                        {
                            AggregationCalculationMode = (AggregationCalculationModes)Enum.Parse(typeof(AggregationCalculationModes), measureResultDto.AggregationCalculationMode),
                            CapillarySize = measureResultDto.CapillarySize,
                            ChannelCount = measureResultDto.ChannelCount,
                            CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultDto.CreatedAt),
                            CreatedBy = measureResultDto.CreatedBy,
                            DilutionCasyTonVolume = measureResultDto.DilutionCasyTonVolume,
                            DilutionFactor = measureResultDto.DilutionFactor,
                            DilutionSampleVolume = measureResultDto.DilutionSampleVolume,
                            FromDiameter = measureResultDto.FromDiameter,
                            HasSubpopulations = measureResultDto.HasSubpopulations,
                            IsDeviationControlEnabled = measureResultDto.IsDeviationControlEnabled,
                            IsSmoothing = measureResultDto.IsSmoothing,
                            LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultDto.LastModifiedAt),
                            LastModifiedBy = measureResultDto.LastModifiedBy,
                            ManualAggregationCalculationFactor = measureResultDto.ManualAggrgationCalculationFactor,
                            MeasureMode = (MeasureModes)Enum.Parse(typeof(MeasureModes), measureResultDto.MeasureMode),
                            MeasureResult = measureResult,
                            Name = measureResultDto.Name,
                            Repeats = measureResultDto.Repeats,
                            ScalingMaxRange = measureResultDto.ScalingMaxRange,
                            ScalingMode = (ScalingModes)Enum.Parse(typeof(ScalingModes), measureResultDto.ScalingMode),
                            ToDiameter = measureResultDto.ToDiameter,
                            SmoothingFactor = measureResultDto.SmoothingFactor,
                            UnitMode = (UnitModes) Enum.Parse(typeof(UnitModes), measureResultDto.UnitMode),
                            Volume = (Volumes) Enum.Parse(typeof(Volumes), measureResultDto.Volume),
                            VolumeCorrectionFactor = measureResultDto.VolumeCorrectionFactor
                        };

                        List<MeasureResultItemTypes> types = new List<MeasureResultItemTypes>();
                        foreach (var type in Enum.GetNames(typeof(MeasureResultItemTypes)))
                        {
                            types.Add((MeasureResultItemTypes)Enum.Parse(typeof(MeasureResultItemTypes), type));
                        }

                        measureResult.MeasureSetup.ResultItemTypes = string.Join(";", types);

                        measureResult.OriginalMeasureSetup = measureResult.MeasureSetup;

                        foreach (var deviationItem in measureResultDto.DeviationConrolItems)
                        {
                            measureResult.MeasureSetup.DeviationControlItems.Add(new DeviationControlItem()
                            {
                                MaxLimit = deviationItem.MaxLimit,
                                MinLimit = deviationItem.MinLimit,
                                MeasureResultItemType = (MeasureResultItemTypes) Enum.Parse(typeof(MeasureResultItemTypes), deviationItem.MeasureResultItemType),
                                MeasureSetup = measureResult.MeasureSetup
                            });
                        }

                        foreach(var rangeItem in measureResultDto.Ranges)
                        {
                            measureResult.MeasureSetup.AddCursor(new Casy.Models.Cursor()
                            {
                                IsDeadCellsCursor = rangeItem.IsDeadCellsCursor,
                                LastModifiedAt = DateTimeOffsetExtensions.ParseAny(rangeItem.LastModifiedAt),
                                LastModifiedBy = rangeItem.LastModifiedBy,
                                CreatedAt = DateTimeOffsetExtensions.ParseAny(rangeItem.CreatedAt),
                                CreatedBy = rangeItem.CreatedBy,
                                MaxLimit = rangeItem.MaxLimit,
                                MeasureSetup = measureResult.MeasureSetup,
                                MinLimit = rangeItem.MinLimit,
                                Name = rangeItem.Name,
                                Subpopulation = rangeItem.Subpopulation
                            });
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph)
                            {
                                Parameter = true
                            });
                            _measureResultManager.AddSelectedMeasureResults(new[] { measureResult });
                        });
                    }
                    else
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new ManualResetEvent(false);

                            var showMessageBoxEventWrapper = new ShowMessageBoxDialogWrapper
                            {
                                Awaiter = awaiter,
                                Message = "Unable to connect to CASY remote web service",
                                Title = "Connection failed"
                            };

                            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(showMessageBoxEventWrapper);

                            awaiter.WaitOne();
                        });
                        _isConnected = false;
                        NotifyOfPropertyChange("IsConnected");
                    }
                }
            }
            _environmentService.SetEnvironmentInfo("IsBusy", false);
        }

        public void OnImportsSatisfied()
        {
            
        }
    }
}
