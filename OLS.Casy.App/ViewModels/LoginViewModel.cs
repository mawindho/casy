using OLS.Casy.App.Services.Settings;
using OLS.Casy.App.Validations;
using OLS.Casy.App.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using OLS.Casy.App.Dto;
using OLS.Casy.App.Exceptions;
using OLS.Casy.App.Services.RequestProvider;
using Xamarin.Forms;
using System.Net.Http;
using OLS.Casy.App.Services.Dialog;

namespace OLS.Casy.App.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private ValidatableObject<string> _userName;
        private ValidatableObject<string> _password;

        private bool _isValid;

        private readonly ISettingsService _settingsService;
        private readonly IRequestProvider _requestProvider;
        private readonly IDialogService _dialogService;

        public LoginViewModel(ISettingsService settingsService, IRequestProvider requestProvider, IDialogService dialogService)
        {
            _settingsService = settingsService;
            _requestProvider = requestProvider;
            _dialogService = dialogService;

            _userName = new ValidatableObject<string>();
            _password = new ValidatableObject<string>();

            AddValidations();
        }

        public ValidatableObject<string> UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                RaisePropertyChanged(() => UserName);
            }
        }

        public ValidatableObject<string> Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                RaisePropertyChanged(() => Password);
            }
        }

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
            set
            {
                _isValid = value;
                RaisePropertyChanged(() => IsValid);
            }
        }

        public ICommand SignInCommand => new Command(async () => await SignInAsync());
        public ICommand SettingsCommand => new Command(async () => await SettingsAsync());
        public ICommand ValidateUserNameCommand => new Command(() => ValidateUserName());
        public ICommand ValidatePasswordCommand => new Command(() => ValidatePassword());

        public override Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            if (navigationData != null && navigationData.ContainsKey("Logout"))
            {
                var logoutParameter = (bool)navigationData["Logout"];

                if (logoutParameter)
                {
                    Logout();
                }
            }

            return base.InitializeAsync(navigationData);
        }

        private async Task SignInAsync()
        {
            IsBusy = true;
            IsValid = true;
            bool isValid = Validate();
            bool isAuthenticated = false;

            UserDto userDto = null;
            if (isValid)
            {
                try
                {
                    await Task.Delay(10);
                    userDto = await _requestProvider.PostAsync<UserDto, UserDto>(
                        $"{_settingsService.CasyEndpointBase}/authentication",
                        new UserDto()
                        {
                            Username = UserName.Value,
                            Password = Password.Value
                        },
                        "casy", "c4sy");

                    isAuthenticated = true;
                }
                catch (ServiceAuthenticationException sae)
                {
                    isAuthenticated = false;
                    await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                }
                catch (HttpRequestExceptionEx hre)
                {
                    isAuthenticated = false;
                    await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                }
                catch(HttpRequestException re)
                {
                    isAuthenticated = false;
                    await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SignIn] Error signing in: {ex}");
                }
            }
            else
            {
                IsValid = false;
            }

            if (isAuthenticated)
            {
                _settingsService.AuthAccessToken = GlobalSetting.Instance.AuthToken;
                await _settingsService.AddOrUpdateValue("LoggedInUser", $"{userDto.FirstName} {userDto.LastName}");

                await NavigationService.NavigateToAsync<MainViewModel>();
                //await NavigationService.RemoveLastFromBackStackAsync();
            }

            IsBusy = false;
        }

        private void Logout()
        {
            _settingsService.AuthAccessToken = string.Empty;
            _settingsService.AuthIdToken = string.Empty;
        }

        private async Task SettingsAsync()
        {
            await NavigationService.NavigateToAsync<SettingsViewModel>();
        }

        private bool Validate()
        {
            bool isValidUser = ValidateUserName();
            bool isValidPassword = ValidatePassword();

            return isValidUser && isValidPassword;
        }

        private bool ValidateUserName()
        {
            return _userName.Validate();
        }

        private bool ValidatePassword()
        {
            return _password.Validate();
        }

        private void AddValidations()
        {
            _userName.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "A username is required." });
            _password.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "A password is required." });
        }
    }
}
