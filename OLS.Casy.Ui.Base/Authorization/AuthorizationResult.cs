using OLS.Casy.Ui.Base.DyamicUiHelper;

namespace OLS.Casy.Ui.Base.Authorization
{
    internal sealed class AuthorizationResult : Result
    {
        /// <summary>
        ///     Leere Instanz vom Typ <see cref="AuthorizationResult" /> representiert ein valides Ergebnis ohne Fehler.
        /// </summary>
        public static readonly AuthorizationResult Allowed;

        public AuthorizationResult(string errorMessage)
            : base(errorMessage)
        {
        }
    }
}
