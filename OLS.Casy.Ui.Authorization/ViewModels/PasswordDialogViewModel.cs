using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Authorization.Local;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;

namespace OLS.Casy.Ui.Authorization.ViewModels
{
    /// <summary>
	///     View model for user password manipulation.
	/// </summary>
	[PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(PasswordDialogViewModel))]
    public class PasswordDialogViewModel : DialogModelBase
    {
        private readonly LocalAuthenticationService _authenticationService;
        private string _currentPassword;
        private User _currentUser;
        private bool _isCurrentPasswordInvalid;
        private bool _isNewPasswordInvalid;
        private bool _isRepeatedPasswordInvalid;
        private string _newPassword;
        private string _repeatNewPassword;
        private bool _isForceNewPassword;

        /// <summary>
        ///     Importing constructor
        /// </summary>
        /// <param name="authenticationService"></param>
        [ImportingConstructor]
        public PasswordDialogViewModel(LocalAuthenticationService authenticationService)
        {
            this._authenticationService = authenticationService;
            this.Prepare();
        }

        /// <summary>
        ///     Inout data to be processed by the view model.
        /// </summary>
        public User CurrentUser
        {
            set
            {
                this._currentUser = value;
                this.IsForceNewPassword = this._currentUser.ForceCreatePassword;
            }

            get { return this._currentUser; }
        }

        public bool IsForceNewPassword
        {
            get { return _isForceNewPassword; }
            set
            {
                if(value != _isForceNewPassword)
                {
                    this._isForceNewPassword = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        /// <summary>
        ///     Getter/setter for CurrentPassword property.
        /// </summary>
        [Required]
        public string CurrentPassword
        {
            get { return this._currentPassword; }
            set
            {
                this._currentPassword = value;
                this.NotifyOfPropertyChange();
            }
        }

        /// <summary>
        ///     Getter/setter for NewPassword property.
        /// </summary>
        [Required]
        public string NewPassword
        {
            get { return this._newPassword; }
            set
            {
                this._newPassword = value;
                this.NotifyOfPropertyChange();
            }
        }

        /// <summary>
        ///     Getter/setter for RepeatNewPassword property.
        /// </summary>
        [Required]
        public string RepeatNewPassword
        {
            get { return this._repeatNewPassword; }
            set
            {
                this._repeatNewPassword = value;
                this.NotifyOfPropertyChange();
            }
        }

        /// <summary>
        ///     Getter/setter for IsCurrentPasswordInvalid property.
        /// </summary>
        public bool IsCurrentPasswordInvalid
        {
            get { return this._isCurrentPasswordInvalid; }
            set
            {
                this._isCurrentPasswordInvalid = value;
                this.NotifyOfPropertyChange();
            }
        }

        /// <summary>
        ///     Getter/setter for IsNewPasswordInvalid property.
        /// </summary>
        public bool IsNewPasswordInvalid
        {
            get { return this._isNewPasswordInvalid; }
            set
            {
                this._isNewPasswordInvalid = value;
                this.NotifyOfPropertyChange();
            }
        }

        /// <summary>
        ///     Getter/setter for IsRepeatedPasswordInvalid property.
        /// </summary>
        public bool IsRepeatedPasswordInvalid
        {
            get { return this._isRepeatedPasswordInvalid; }
            set
            {
                this._isRepeatedPasswordInvalid = value;
                this.NotifyOfPropertyChange();
            }
        }

        
        public void ResetTextFields()
        {
            this.CurrentPassword = string.Empty;
            this.NewPassword = string.Empty;
            this.RepeatNewPassword = string.Empty;
        }

        private void Prepare()
        {
            this.IsCurrentPasswordInvalid = false;
            this.IsNewPasswordInvalid = false;
            this.ResetTextFields();
        }

        protected override void OnCancel()
        {
            this.ResetTextFields();
            base.OnCancel();
        }

        protected override void OnOk()
        {
            if (!this.NewPassword.Equals(this.RepeatNewPassword))
            {
                this.IsRepeatedPasswordInvalid = true;
                this.ResetTextFields();
                return;
            }

            if (this._authenticationService.TryGetUser(this.CurrentUser.Identity.Name) == null)
            {
                
            }
            else
            {
                if (this._authenticationService.CheckPassword(this.CurrentUser.Identity.Name,
                                                            this.CurrentPassword))
                {
                    this.IsCurrentPasswordInvalid = false;
                    if (this._authenticationService.ChangePassword(this.CurrentUser.Identity.Name,
                                                                    this.CurrentPassword, this.NewPassword))
                    {
                        base.OnOk();
                    }
                    else
                    {
                        this.IsNewPasswordInvalid = true;
                    }
                }
                else
                {
                    this.IsCurrentPasswordInvalid = true;
                }
            }
            this.RepeatNewPassword = String.Empty;
        }
    }
}
