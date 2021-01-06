using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace OLS.Casy.Controller.Api
{
    /// <summary>
    /// Exception is thrown when wrong checksum has been detected
    /// </summary>
    [Serializable]
    public class WrongChecksumException : Exception
    {
        public WrongChecksumException()
        {
        }

        public WrongChecksumException(string message): base(message) 
        {
        }

        public WrongChecksumException(string message, Exception innerException)
            : base (message, innerException)
        {
        }

        protected WrongChecksumException(SerializationInfo info,
           StreamingContext context) 
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception is thrown when calibration file and casy device serial numer don't match
    /// </summary>
    [Serializable]
    public class InvalidSerialNumberException : Exception
    {
        private readonly string _calibrationName;

        public InvalidSerialNumberException()
        {
        }

        public InvalidSerialNumberException(string message): base(message) 
        {
        }

        public InvalidSerialNumberException(string message, Exception innerException): base (message, innerException)
        {
        }

        protected InvalidSerialNumberException(SerializationInfo info,
           StreamingContext context) : base(info, context)
        {
            _calibrationName = info.GetString("CalibrationName");
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="calibrationName">Name of the calibration with the invalid serial number</param>
        public InvalidSerialNumberException(string message, string calibrationName)
            :this(message)
        {
            this._calibrationName = calibrationName;
        }


        /// <summary>
        /// Property for the name of the calibration with the invalid serial number
        /// </summary>
        public string CalibrationName
        {
            get { return _calibrationName; }
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("CalibrationName", _calibrationName);
        }

    }
}
