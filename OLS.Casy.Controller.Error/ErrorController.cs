using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Config.Api;
using System.ComponentModel.Composition;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.IO.Api;
using System.Globalization;

namespace OLS.Casy.Controller.Error
{
    /// <summary>
    /// Implementation of <see cref="IErrorContoller"/>.
    /// Parses the casy responses and tries to find corresponding error messages and details in database.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IErrorContoller))]
    public class ErrorController : AbstractService, IErrorContoller, IPartImportsSatisfiedNotification
    {
        private IDatabaseStorageService _databaseStorageService;
        private readonly Dictionary<string, ErrorDetails> _errorDetails;

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="logger">Implementation of <see cref="ILogger"/></param>
        /// <param name="configService">Implementation of <see cref="IConfigService"/></param>
        /// <param name="databaseStorageService">Implementation of <see cref="IDatabaseStorageService"/></param>
        [ImportingConstructor]
        public ErrorController(IConfigService configService,
            IDatabaseStorageService databaseStorageService)
            :base(configService)
        {
            this._errorDetails = new Dictionary<string, ErrorDetails>();
            this._databaseStorageService = databaseStorageService;
        }

        public ErrorDetails[] ErrorDetails
        {
            get { return this._errorDetails.Values.ToArray(); }
        }

        /// <summary>
        /// Parses a response string from casy device and returns a corresponding <see cref="ErrorResult"/>
        /// </summary>
        /// <param name="casyResponse">The response of the casy device</param>
        /// <returns>Corresponing instance of <see cref="ErrorResult"/></returns>
        public ErrorResult ParseError(string casyResponse)
        {
            var result = new ErrorResult();
            if (!string.IsNullOrEmpty(casyResponse))
            {
                var split = casyResponse.Split(',');
                if (split.Length >= 3)
                {
                    var errorPieces = split.Take(3).ToArray();
                    Array.Reverse(errorPieces);

                    if (errorPieces.All(item => !string.IsNullOrEmpty(item) && item == "0000"))
                    {
                        result.ErrorResultType = ErrorResultType.NoError;
                        return result;
                    }

                    ErrorCategory[] errorCategories = (ErrorCategory[])Enum.GetValues(typeof(ErrorCategory));
                    for (int i = 0; i < errorCategories.Length; i++)
                    {
                        var flags = BitConverter.GetBytes((int)errorCategories[i]);

                        IList<ErrorDetails> errorDetails = new List<ErrorDetails>();
                        if (GetErrorDetails(errorPieces, flags[2], flags[1], flags[0], errorDetails))
                        {
                            foreach (var errorDetail in errorDetails)
                            {
                                errorDetail.ErrorCategory = errorCategories[i];

                                switch (errorDetail.ErrorNumber)
                                {
                                    case "0":
                                    case "1":
                                    case "2":
                                    case "4":
                                        result.SoftErrorDetails.Add(errorDetail);
                                        break;
                                    default:
                                        result.FatalErrorDetails.Add(errorDetail);
                                        break;
                                }

                            }
                        }
                    }
                    result.ErrorResultType = (result.SoftErrorDetails.Count + result.FatalErrorDetails.Count) > 1 ? ErrorResultType.MutipleError : ErrorResultType.SingleError;
                }
            }
            
            return result;
        }

        private bool GetErrorDetails(string[] errorPieces, int pieceIndex, int substringIndex, int substringLength, IList<ErrorDetails> errorDetailss)
        {
            var error = errorPieces[pieceIndex].Substring(substringIndex, substringLength);

            var errorChars = error.ToCharArray();
            if (errorChars.All(c => c == '0'))
            {
                return false;
            }

            for(int i = 0; i < errorChars.Length; i++)
            {
                byte errorByte = (byte)HexToInt(errorChars[i]);

                for(int j = 0; j < 4; j++)
                {
                    if((errorByte & (1 << j)) != 0)
                    {
                        var test = Math.Pow(2, j);


                    

                StringBuilder builder = new StringBuilder();
                builder.Append(string.Concat(Enumerable.Repeat("0000-", pieceIndex)));
                builder.Append(string.Concat(Enumerable.Repeat("0", substringIndex)));
                        builder.Append(string.Concat(Enumerable.Repeat("0", i)));
                        builder.Append(test);
                        builder.Append(string.Concat(Enumerable.Repeat("0", errorChars.Length-1-i)));
                builder.Append(string.Concat(Enumerable.Repeat("0", 4 - error.Length - substringIndex)));
                builder.Append(string.Concat(Enumerable.Repeat("-0000", 2 - pieceIndex)));

                ErrorDetails errorDetails;
                if (!_errorDetails.TryGetValue(builder.ToString(), out errorDetails))
                {
                    //throw new ArgumentException("Unknown Error Code: " + builder.ToString());
                }
                errorDetailss.Add(errorDetails);
                    }
                }
            }
            return true;
        }

        private int HexToInt(char hexChar)
        {
            hexChar = char.ToUpper(hexChar, CultureInfo.InvariantCulture);  // may not be necessary

            return (int)hexChar < (int)'A' ?
                ((int)hexChar - (int)'0') :
                10 + ((int)hexChar - (int)'A');
        }

        public void OnImportsSatisfied()
        {
            var errorDetails = _databaseStorageService.GetErrorDetails();

            if (errorDetails.Count() == 0)
            {
                errorDetails = CreateErrorDetails();
            }

            foreach(var errorDetail in errorDetails)
            {
                _errorDetails.Add(errorDetail.ErrorCode, errorDetail);
            }
        }

        private IEnumerable<ErrorDetails> CreateErrorDetails()
        {
            var errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0001", ErrorNumber = "0" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0002", ErrorNumber = "1" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0004", ErrorNumber = "2" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0008", ErrorNumber = "3" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0010", ErrorNumber = "4" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0020", ErrorNumber = "5" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0040", ErrorNumber = "6" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0080", ErrorNumber = "7" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0100", ErrorNumber = "8" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0200", ErrorNumber = "9" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0400", ErrorNumber = "10" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-0800", ErrorNumber = "11" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-1000", ErrorNumber = "12" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-2000", ErrorNumber = "13" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-4000", ErrorNumber = "14" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0000-8000", ErrorNumber = "15" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0001-0000", ErrorNumber = "16" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0002-0000", ErrorNumber = "17" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0004-0000", ErrorNumber = "18" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0008-0000", ErrorNumber = "19" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0010-0000", ErrorNumber = "20" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0020-0000", ErrorNumber = "21" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0040-0000", ErrorNumber = "22" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0080-0000", ErrorNumber = "23" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0100-0000", ErrorNumber = "24" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0200-0000", ErrorNumber = "25" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0400-0000", ErrorNumber = "26" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-0800-0000", ErrorNumber = "27" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-1000-0000", ErrorNumber = "28" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-2000-0000", ErrorNumber = "29" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-4000-0000", ErrorNumber = "30" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0000-8000-0000", ErrorNumber = "31" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0001-0000-0000", ErrorNumber = "32" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0002-0000-0000", ErrorNumber = "33" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0004-0000-0000", ErrorNumber = "34" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0008-0000-0000", ErrorNumber = "35" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0010-0000-0000", ErrorNumber = "36" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0020-0000-0000", ErrorNumber = "37" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0040-0000-0000", ErrorNumber = "38" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0080-0000-0000", ErrorNumber = "37" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0100-0000-0000", ErrorNumber = "40" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0200-0000-0000", ErrorNumber = "41" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0400-0000-0000", ErrorNumber = "42" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "0800-0000-0000", ErrorNumber = "43" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "1000-0000-0000", ErrorNumber = "44" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "2000-0000-0000", ErrorNumber = "45" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "4000-0000-0000", ErrorNumber = "46" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            errorDetails = new ErrorDetails() { ErrorCode = "8000-0000-0000", ErrorNumber = "47" };
            _databaseStorageService.SaveErrorDetails(errorDetails);

            return _databaseStorageService.GetErrorDetails();
        }
    }
}
