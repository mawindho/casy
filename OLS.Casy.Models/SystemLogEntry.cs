using OLS.Casy.Models.Enums;
using System;
using System.ComponentModel;

namespace OLS.Casy.Models
{
    public class SystemLogEntry
    {

        public LogCategory Category { get; set; }
        public DateTimeOffset Date { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string DateDisplay
        {
            get;
            set; //{ return Date.UtcDateTime.ToString("dd.MM.yyyy HH:mm:ss"); }
        }
        public string Level { get; set; }

        public string User { get; set; }

        public string Message { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string CategoryDisplay
        {
            get { return Enum.GetName(typeof(LogCategory), Category); }
        }

    }
}
