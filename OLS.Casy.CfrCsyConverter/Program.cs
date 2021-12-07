using OLS.Casy.Base;
using OLS.Casy.Core;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.CfrCsyConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start converting CRF files");
            var files = Directory.GetFiles(@".", "*.crf");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var measureResult = Import(file, "#FFFF2F34");
                measureResult.MeasureResultGuid = Guid.NewGuid();
                measureResult.Name = Path.GetFileNameWithoutExtension(fileInfo.Name);
                measureResult.MeasureSetup.Name = string.Format("{0} Setup", measureResult.Name);
                ExportMeasureResult(new [] { measureResult }, measureResult.Name + ".csy");
            }
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        public static void ExportMeasureResult(MeasureResult[] measureResults, string filePath)
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, measureResults);

                memoryStream.Seek(0, SeekOrigin.Begin);

                var bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);

                bytes = Encrypt(bytes);

                var fileInfo = new FileInfo(filePath);

                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                var fileStream = File.Create(filePath);
                fileStream.Write(bytes, 0, bytes.Length);
            }

            Console.WriteLine(string.Format("File Export: Successfully exported measure result to file '{0}'. Measure result name: {1}", filePath, measureResults[0].Name));
        }

        private const string SECURITYKEY = "ThisIsCasyBamBamBam";

        public static byte[] Encrypt(byte[] data)
        {
            return Encrypt(data, SECURITYKEY);
        }

        public static byte[] Encrypt(byte[] data, string securityKey)
        {
            byte[] keyArray;

            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(securityKey));

            //Always release the resources and flush data
            // of the Cryptographic service provide. Best Practice
            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            byte[] resultArray =
                cTransform.TransformFinalBlock(data, 0,
                    data.Length);
            //Release resources held by TripleDes Encryptor
            tdes.Clear();

            return resultArray;
        }

        public static MeasureResult Import(string filePath, string color)
        {
            var measureResult = new MeasureResult()
            {
                MeasureResultGuid = Guid.NewGuid(),
                IsTemporary = false,
                Color = color,
                Origin = "CRF-Import",
                IsCfr = true
            };

            var measureResultData = new MeasureResultData();
            measureResult.MeasureResultDatas.Add(measureResultData);
            measureResultData.MeasureResult = measureResult;

            var measureSetup = new MeasureSetup()
            {
                MeasureSetupId = -1,
                IsTemplate = false,
                MeasureResult = measureResult
            };
            measureResult.MeasureSetup = measureSetup;

            var origMeasureSetup = new MeasureSetup()
            {
                MeasureSetupId = -1,
                IsTemplate = false,
                MeasureResult = measureResult
            };
            measureResult.OriginalMeasureSetup = origMeasureSetup;
            //

            FileStream fileStream = null;
            byte[] fileBytes = null;
            try
            {
                fileStream = File.OpenRead(filePath);

                fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, (int)fileStream.Length);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                
            }

            using (MemoryStream ms = new MemoryStream(fileBytes))
            {
                byte[] buf = new byte[32];
                ms.Read(buf, 0, 32);
                var securityCode = Encoding.Default.GetString(buf);

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var checksum = BitConverter.ToUInt32(buf, 0);

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var offsetPropertyDescriptors = BitConverter.ToUInt32(buf, 0);

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var numPropertyDescriptors = BitConverter.ToUInt32(buf, 0);

                buf = new byte[32];
                ms.Read(buf, 0, 32);
                var contentDesc = Encoding.Default.GetString(buf);

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var numAuditTrailEntries = BitConverter.ToUInt32(buf, 0);

                var keepAuditTrail = (byte)ms.ReadByte();

                byte[] reserved = new byte[47];
                ms.Read(reserved, 0, 47);

                if (ms.Position < offsetPropertyDescriptors)
                {
                    ms.Seek(offsetPropertyDescriptors, SeekOrigin.Begin);
                }

                for (int j = 0; j < numPropertyDescriptors; j++)
                {
                    buf = new byte[4];
                    ms.Read(buf, 0, 4);
                    var validity = BitConverter.ToUInt32(buf, 0);

                    /*
                     * 
                        const long internalNodeId = -1L;
                        const long auditTrailNodeId = -2L;
                    */
                    buf = new byte[4];
                    ms.Read(buf, 0, 4);
                    var node = BitConverter.ToInt16(buf, 0);

                    buf = new byte[4];
                    ms.Read(buf, 0, 4);
                    var id = BitConverter.ToUInt32(buf, 0);

                    /*
                     * Invalid = 0,
	Long = 1,
	Double = 2,
	Bool = 3,
	String = 4,
	ByteArr = 5,
	LongArr = 6,
	DoubleArr = 7,
	Time = 8
                     * 
                     * 
                     */

                    buf = new byte[4];
                    ms.Read(buf, 0, 4);
                    var type = BitConverter.ToUInt32(buf, 0);

                    buf = new byte[4];
                    ms.Read(buf, 0, 4);
                    var size = BitConverter.ToUInt32(buf, 0);

                    buf = new byte[4];
                    ms.Read(buf, 0, 4);
                    var sizeInStream = BitConverter.ToUInt32(buf, 0);

                    buf = new byte[4];
                    ms.Read(buf, 0, 4);
                    var reserved2 = BitConverter.ToUInt32(buf, 0);

                    buf = new byte[sizeInStream];
                    ms.Read(buf, 0, (int)sizeInStream);
                    dynamic val = GetValueFromBuffer(type, buf);

                    switch (node)
                    {
                        // Internal Node
                        case -1:
                            SetPropertyByInternalId((int)id, val, measureResult);
                            break;
                        // Normal
                        case 2:
                            SetPropertyById((int)id, val, measureResult);
                            break;
                        //Audit trail
                        case -2:
                            //if (isCfrEnabled)
                            //{
                            //CreateAuditTrailEntry(val, measureResult);
                            //}
                            break;
                    }
                }


            }

            var orderedCursor = measureResult.MeasureSetup.Cursors.OrderBy(c => c.MinLimit).ToArray();
            if (orderedCursor.Length == 1)
            {
                var single = orderedCursor.FirstOrDefault();
                single.Name = "Range 1";
                measureResult.MeasureSetup.MeasureMode = MeasureModes.MultipleCursor;
            }
            else if (orderedCursor.Length == 2)
            {
                if (orderedCursor[0].MinLimit == orderedCursor[1].MinLimit && orderedCursor[0].MaxLimit == orderedCursor[1].MaxLimit)
                {
                    var single = orderedCursor[0];
                    single.Name = "Range 1";
                    measureResult.MeasureSetup.MeasureMode = MeasureModes.MultipleCursor;

                    measureResult.MeasureSetup.Cursors.Remove(orderedCursor[1]);
                }
                else
                {
                    orderedCursor[0].Name = "Cursor_DeadCells_Name";
                    orderedCursor[0].IsDeadCellsCursor = true;
                    orderedCursor[1].Name = "Cursor_VitalCells_Name";
                    orderedCursor[0].MaxLimit = orderedCursor[1].MinLimit - 0.01;
                    measureResult.MeasureSetup.MeasureMode = MeasureModes.Viability;
                }
            }

            var orderedOrigCursor = measureResult.OriginalMeasureSetup.Cursors.OrderBy(c => c.MinLimit).ToArray();
            if (orderedOrigCursor.Length == 1)
            {
                var single = orderedOrigCursor.FirstOrDefault();
                single.Name = "Range 1";
                measureResult.OriginalMeasureSetup.MeasureMode = MeasureModes.MultipleCursor;
            }
            else if (orderedOrigCursor.Length == 2)
            {
                if (orderedOrigCursor[0].MinLimit == orderedOrigCursor[1].MinLimit && orderedOrigCursor[0].MaxLimit == orderedOrigCursor[1].MaxLimit)
                {
                    var single = orderedOrigCursor[0];
                    single.Name = "Range 1";
                    measureResult.OriginalMeasureSetup.MeasureMode = MeasureModes.MultipleCursor;

                    measureResult.OriginalMeasureSetup.Cursors.Remove(orderedCursor[1]);
                }
                else
                {
                    orderedOrigCursor[0].Name = "Cursor_DeadCells_Name";
                    orderedOrigCursor[0].IsDeadCellsCursor = true;
                    orderedOrigCursor[1].Name = "Cursor_VitalCells_Name";
                    orderedOrigCursor[0].MaxLimit = orderedCursor[1].MinLimit - 0.01;
                    measureResult.OriginalMeasureSetup.MeasureMode = MeasureModes.Viability;
                }
            }

            List<MeasureResultItemTypes> types = new List<MeasureResultItemTypes>();
            foreach (var type in Enum.GetNames(typeof(MeasureResultItemTypes)))
            {
                types.Add((MeasureResultItemTypes)Enum.Parse(typeof(MeasureResultItemTypes), type));
            }

            measureResult.MeasureSetup.ResultItemTypes = string.Join(";", types);
            measureResult.OriginalMeasureSetup.ResultItemTypes = string.Join(";", types);

            foreach (var c in measureResult.MeasureSetup.Cursors)
            {
                c.MinLimit = Calculations.CalcSmoothedDiameter(0, measureResult.MeasureSetup.ToDiameter, (int)c.MinLimit, 1024);
                c.MaxLimit = Calculations.CalcSmoothedDiameter(0, measureResult.MeasureSetup.ToDiameter, (int)c.MaxLimit, 1024);
                c.MeasureSetup = measureResult.MeasureSetup;
            }

            foreach (var c in measureResult.OriginalMeasureSetup.Cursors)
            {
                c.MinLimit = Calculations.CalcSmoothedDiameter(0, measureResult.OriginalMeasureSetup.ToDiameter, (int)c.MinLimit, 1024);
                c.MaxLimit = Calculations.CalcSmoothedDiameter(0, measureResult.OriginalMeasureSetup.ToDiameter, (int)c.MaxLimit, 1024);
                c.MeasureSetup = measureResult.OriginalMeasureSetup;
            }

            if (measureResult.MeasureSetup.ManualAggregationCalculationFactor != 0d)
            {
                measureResult.MeasureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.Manual;
            }

            if (measureResult.OriginalMeasureSetup.ManualAggregationCalculationFactor != 0d)
            {
                measureResult.OriginalMeasureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.Manual;
            }

            if (measureResult.MeasureSetup.DilutionFactor > 1d)
            {
                measureResult.MeasureSetup.DilutionCasyTonVolume = 10d;
                measureResult.MeasureSetup.DilutionSampleVolume = 1;
            }

            if (measureResult.OriginalMeasureSetup.DilutionFactor > 1d)
            {
                measureResult.OriginalMeasureSetup.DilutionCasyTonVolume = 10d;
                measureResult.OriginalMeasureSetup.DilutionSampleVolume = (1000 * measureResult.OriginalMeasureSetup.DilutionCasyTonVolume) / (measureResult.OriginalMeasureSetup.DilutionFactor - 1);
            }

            if (!measureResult.MeasureSetup.IsSmoothing)
            {
                measureResult.MeasureSetup.SmoothingFactor = 0d;
            }

            if (!measureResult.OriginalMeasureSetup.IsSmoothing)
            {
                measureResult.OriginalMeasureSetup.SmoothingFactor = 0d;
            }

            Console.WriteLine(string.Format("CRF-Import successful. File: {0}. Measure Result Name: {1}", filePath, measureResult.Name));

            return measureResult;
        }

        private static void CreateAuditTrailEntry(byte[] buffer, MeasureResult measureResult)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                var auditTrailEntry = new AuditTrailEntry()
                {
                    MeasureResult = measureResult
                };

                var buf = new byte[4];
                ms.Read(buf, 0, 4);
                var lengthModificator = BitConverter.ToUInt32(buf, 0);

                buf = new byte[lengthModificator];
                ms.Read(buf, 0, (int)lengthModificator);
                string modificator = (string)GetValueFromBuffer(4, buf);

                auditTrailEntry.UserChanged = modificator;

                //Old Value
                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var size = BitConverter.ToUInt32(buf, 0);

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var type = BitConverter.ToUInt32(buf, 0);

                buf = new byte[size - 8];
                ms.Read(buf, 0, (int)size - 8);
                var persPropOldBuf = GetValueFromBuffer(type, buf);

                auditTrailEntry.OldValue = persPropOldBuf.ToString();

                //New Value
                buf = new byte[4];
                ms.Read(buf, 0, 4);
                size = BitConverter.ToUInt32(buf, 0);

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                type = BitConverter.ToUInt32(buf, 0);

                buf = new byte[size - 8];
                ms.Read(buf, 0, (int)size - 8);
                var persPropNewBuf = GetValueFromBuffer(type, buf);

                auditTrailEntry.NewValue = persPropNewBuf.ToString();

                //enum EModification
                //{
                //  none = 0,
                //  added = 1,
                //  modified = 2,
                //  removed = 3
                //};
                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var modification = BitConverter.ToUInt32(buf, 0);

                switch (modification)
                {
                    case 1:
                        auditTrailEntry.Action = "Added";
                        break;
                    case 2:
                        auditTrailEntry.Action = "Modified";
                        break;
                    case 3:
                        auditTrailEntry.Action = "Deleted";
                        break;
                }

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var node = BitConverter.ToUInt32(buf, 0);

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var id = BitConverter.ToUInt32(buf, 0);

                string propertyName = string.Empty;
                GetFieldById(id, out propertyName);

                auditTrailEntry.EntityName = "CFR Import";
                auditTrailEntry.PropertyName = propertyName;

                buf = new byte[4];
                ms.Read(buf, 0, 4);
                var time = BitConverter.ToUInt32(buf, 0);

                auditTrailEntry.DateChanged = new DateTimeOffset((DateTime)GetValueFromBuffer(8, buf));

                measureResult.AuditTrailEntries.Add(auditTrailEntry);
            }
        }

        private static dynamic GetValueFromBuffer(uint type, byte[] buf)
        {
            switch (type)
            {
                case 1:
                    return BitConverter.ToUInt32(buf, 0);
                case 2:
                    return BitConverter.ToDouble(buf, 0);
                case 3:
                    return BitConverter.ToBoolean(buf, 0);
                case 4:
                    char[] asciiChars = new char[Encoding.Unicode.GetCharCount(buf, 0, buf.Length)];
                    Encoding.Unicode.GetChars(buf, 0, buf.Length, asciiChars, 0);
                    string stringValue = new string(asciiChars);
                    return stringValue.Replace("\0", string.Empty);
                case 5:
                    return buf;
                case 6:
                    var s = buf.Length / sizeof(int);
                    var result = new int[s];
                    for (var index = 0; index < s; index++)
                    {
                        result[index] = BitConverter.ToInt32(buf, index * sizeof(int));
                    }
                    return result;
                case 7:
                    var s1 = buf.Length / sizeof(double);
                    var result2 = new double[s1];
                    for (var index = 0; index < s1; index++)
                    {
                        result2[index] = BitConverter.ToDouble(buf, index * sizeof(double));
                    }
                    return result2;
                case 8:
                    var cTime = BitConverter.ToUInt32(buf, 0);
                    TimeSpan span = TimeSpan.FromTicks(cTime * TimeSpan.TicksPerSecond);
                    return new DateTime(1970, 1, 1).Add(span);
            }
            return null;
        }

        private static void SetPropertyByInternalId(int id, dynamic value, MeasureResult measureResult)
        {
            /*
            const long idCreator = 1L;
            const long idCreationTime = 2L;
            const long idModificator = 3L;
            const long idModificationTime = 4L;
            const long idDevice = 5L;
            */
            switch (id)
            {
                //Creator
                case 1:
                    measureResult.CreatedBy = value as string;
                    break;
                //Creation Time
                case 2:
                    measureResult.MeasuredAt = (DateTime)value;
                    measureResult.MeasuredAtTimeZone = TimeZoneInfo.Local;
                    measureResult.CreatedAt = DateTime.UtcNow;
                    break;
                //Modificator
                case 3:
                    measureResult.LastModifiedBy = value as string;
                    break;
                //Modification Time
                case 4:
                    measureResult.LastModifiedAt = (DateTime)value;
                    break;
            }
        }

        private static void SetPropertyById(int id, dynamic value, MeasureResult measureResult)
        {
            Cursor cursor;
            Cursor origCursor;

            switch (id)
            {
                //ACCUMULATED_CHANNEL_DATA
                case 1:
                    break;
                //AUTO_RETRY
                case 2:
                    break;
                //CALIBRATION_FILENAME
                case 3:
                    break;
                //CALIBRATION_FILENAME
                case 4:
                    measureResult.MeasureSetup.CapillarySize = (ushort)value;
                    measureResult.OriginalMeasureSetup.CapillarySize = (ushort)value;
                    break;
                //CHANNEL_DATA
                case 5:
                    measureResult.MeasureResultDatas.First().DataBlock = ((int[])value).Select(v => (double)v).ToArray();
                    break;
                //COMMENT
                case 6:
                    measureResult.Comment = value as string;
                    break;
                //CYCLES
                case 7:
                    measureResult.MeasureSetup.Repeats = (int)value;
                    measureResult.OriginalMeasureSetup.Repeats = (int)value;
                    break;
                //COUNT_RETRIES
                case 8:
                    break;
                //COUNTS_ABOVE_CALIB
                case 9:
                    measureResult.MeasureResultDatas.First().AboveCalibrationLimitCount = (int)value;
                    break;
                //COUNTS_BELOW_CALIB
                case 10:
                    measureResult.MeasureResultDatas.First().BelowCalibrationLimitCount = (int)value;
                    break;
                //COUNTS_BELOW_MEAS
                case 11:
                    measureResult.MeasureResultDatas.First().BelowMeasureLimtCount = (int)value;
                    break;
                //COUNTS_PREVIOUS_MEAS
                case 12:
                    break;
                //CURSOR_LEFT
                case 13:
                    cursor = measureResult.MeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Cursor");
                    if (cursor == null)
                    {
                        cursor = new Cursor();
                        cursor.Name = "Cursor";
                        cursor.Color = "#FFB21B1C";
                        measureResult.MeasureSetup.Cursors.Add(cursor);
                    }
                    cursor.MinLimit = (double)value;

                    origCursor = measureResult.OriginalMeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Cursor");
                    if (origCursor == null)
                    {
                        origCursor = new Cursor();
                        origCursor.Name = "Cursor";
                        origCursor.Color = "#FFB21B1C";
                        measureResult.OriginalMeasureSetup.Cursors.Add(origCursor);
                    }
                    origCursor.MinLimit = (double)value;
                    //cursor.MinLimit = Calculations.CalcSmoothedDiameter(0, measureResult.MeasureSetup.ToDiameter, value, 1024);
                    break;
                //NORM_CURSOR_LEFT
                case 20:
                    cursor = measureResult.MeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Norm");
                    if (cursor == null)
                    {
                        cursor = new Cursor();
                        cursor.Name = "Norm";
                        cursor.Color = "#FFB21B1C";
                        measureResult.MeasureSetup.Cursors.Add(cursor);
                    }
                    cursor.MinLimit = (double)value;

                    origCursor = measureResult.OriginalMeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Norm");
                    if (origCursor == null)
                    {
                        origCursor = new Cursor();
                        origCursor.Name = "Norm";
                        origCursor.Color = "#FFB21B1C";
                        measureResult.OriginalMeasureSetup.Cursors.Add(origCursor);
                    }
                    origCursor.MinLimit = (double)value;
                    //cursor.MinLimit = Calculations.CalcSmoothedDiameter(0, measureResult.MeasureSetup.ToDiameter, value, 1024);
                    break;
                //CURSOR_RIGHT
                case 14:
                    cursor = measureResult.MeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Cursor");
                    if (cursor == null)
                    {
                        cursor = new Cursor();
                        cursor.Name = "Cursor";
                        cursor.Color = "#FFB21B1C";
                        measureResult.MeasureSetup.Cursors.Add(cursor);
                    }
                    cursor.MaxLimit = (double)value;

                    origCursor = measureResult.OriginalMeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Cursor");
                    if (origCursor == null)
                    {
                        origCursor = new Cursor();
                        origCursor.Name = "Cursor";
                        origCursor.Color = "#FFB21B1C";
                        measureResult.OriginalMeasureSetup.Cursors.Add(origCursor);
                    }
                    origCursor.MaxLimit = (double)value;
                    //cursor.MaxLimit = Calculations.CalcSmoothedDiameter(0, measureResult.MeasureSetup.ToDiameter, value, 1024);
                    break;
                //NORM_CURSOR_RIGHT
                case 21:
                    cursor = measureResult.MeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Norm");
                    if (cursor == null)
                    {
                        cursor = new Cursor();
                        cursor.Name = "Norm";
                        cursor.Color = "#FFB21B1C";
                        measureResult.MeasureSetup.Cursors.Add(cursor);
                    }
                    cursor.MaxLimit = (double)value;

                    origCursor = measureResult.OriginalMeasureSetup.Cursors.FirstOrDefault(c => c.Name == "Norm");
                    if (origCursor == null)
                    {
                        origCursor = new Cursor();
                        origCursor.Name = "Norm";
                        origCursor.Color = "#FFB21B1C";
                        measureResult.OriginalMeasureSetup.Cursors.Add(origCursor);
                    }
                    origCursor.MaxLimit = (double)value;
                    //cursor.MaxLimit = Calculations.CalcSmoothedDiameter(0, measureResult.MeasureSetup.ToDiameter, value, 1024);
                    break;
                //DEVIATION_CONTROL
                case 15:
                    measureResult.MeasureSetup.IsDeviationControlEnabled = (bool)value;
                    measureResult.OriginalMeasureSetup.IsDeviationControlEnabled = (bool)value;
                    break;
                //DILUTION
                case 16:
                    measureResult.MeasureSetup.DilutionFactor = (int)value;
                    measureResult.OriginalMeasureSetup.DilutionFactor = (int)value;
                    break;
                //FILENAME
                case 17:
                    measureResult.Name = (string)value;
                    break;
                //FROM_DIAMETER
                case 18:
                    measureResult.MeasureSetup.FromDiameter = (ushort)value;
                    measureResult.OriginalMeasureSetup.FromDiameter = (ushort)value;
                    break;
                //MAXIMUM_DEVIATION
                case 19:
                    //measureResult.MeasureSetup.DeviationLimit = (double)value;
                    measureResult.MeasureSetup.AddDeviationControlItem(new DeviationControlItem()
                    {
                        MeasureResultItemType = MeasureResultItemTypes.Deviation,
                        MaxLimit = (double)value,
                        MeasureSetup = measureResult.MeasureSetup
                    });

                    measureResult.OriginalMeasureSetup.AddDeviationControlItem(new DeviationControlItem()
                    {
                        MeasureResultItemType = MeasureResultItemTypes.Deviation,
                        MaxLimit = (double)value,
                        MeasureSetup = measureResult.OriginalMeasureSetup
                    });
                    break;
                //SMOOTHING_ON
                case 22:
                    measureResult.MeasureSetup.IsSmoothing = (bool)value;
                    measureResult.OriginalMeasureSetup.IsSmoothing = (bool)value;
                    break;
                //SMOOTH_WIDTH
                case 23:
                    measureResult.MeasureSetup.SmoothingFactor = (double)value;
                    measureResult.OriginalMeasureSetup.SmoothingFactor = (double)value;
                    break;
                //TIME
                case 24:
                    break;
                //TO_DIAMETER
                case 25:
                    measureResult.MeasureSetup.ToDiameter = (ushort)value;
                    measureResult.OriginalMeasureSetup.ToDiameter = (ushort)value;
                    break;
                //VOLUME
                case 26:
                    measureResult.MeasureSetup.Volume = value == 200 ? Models.Enums.Volumes.TwoHundred : Models.Enums.Volumes.FourHundred;
                    measureResult.OriginalMeasureSetup.Volume = value == 200 ? Models.Enums.Volumes.TwoHundred : Models.Enums.Volumes.FourHundred;
                    break;
                //Y_SCALE_MANUAL
                case 27:
                    measureResult.MeasureSetup.ScalingMode = value == true ? Models.Enums.ScalingModes.MaxRange : Models.Enums.ScalingModes.Auto;
                    measureResult.OriginalMeasureSetup.ScalingMode = value == true ? Models.Enums.ScalingModes.MaxRange : Models.Enums.ScalingModes.Auto;
                    break;
                //Y_SCALE_MANUAL_MAXIMUM
                case 28:
                    measureResult.MeasureSetup.ScalingMaxRange = (int)value;
                    measureResult.OriginalMeasureSetup.ScalingMaxRange = (int)value;
                    break;
                //Y_SCALE_PAR
                case 29:
                    break;
                // Total couts
                case 30:
                    break;
                //LINK_CURSORS
                case 31:
                    break;
                //LINK_NORM_CURSORS
                case 32:
                    break;
                //ENABLE_PERCENT_CALCULATION
                case 33:
                    break;
                //PERCENT_CALCULATION_TYPE
                case 34:
                    break;
                //ENABLE_AGGREGATION_CORRECTION
                case 35:
                    measureResult.MeasureSetup.AggregationCalculationMode = value == true ? Models.Enums.AggregationCalculationModes.On : Models.Enums.AggregationCalculationModes.Off;
                    measureResult.OriginalMeasureSetup.AggregationCalculationMode = value == true ? Models.Enums.AggregationCalculationModes.On : Models.Enums.AggregationCalculationModes.Off;
                    break;
                //AGGREGATION_CORRECTION_TYPE
                case 36:
                    measureResult.MeasureSetup.ManualAggregationCalculationFactor = (double)value;
                    measureResult.OriginalMeasureSetup.ManualAggregationCalculationFactor = (double)value;
                    break;
                //AGGREGATION_CORRECTION_PEAK_VOLUME
                case 37:
                    break;
                //AUTO_EDIT_COMMENT_ON
                case 38:
                    break;
                //AUTO_PRINT_ON
                case 39:
                    break;
                //AUTO_SAVE_MEAS_ON
                case 40:
                    break;
                //AUTO_SAVE_EXPORT_ON
                case 41:
                    break;
                //AUTO_SAVE_PATH
                case 42:
                    break;
                //AUTO_SAVE_BASE_NAME
                case 43:
                    break;
                //Serial Number
                case 44:
                    measureResult.SerialNumber = (string)value;
                    break;
                //OPTION_FLAGS
                case 45:
                    break;
                //CONCENTRATION
                case 46:
                    measureResult.MeasureResultDatas.First().ConcentrationTooHigh = value > 0;
                    break;
                //DESCRIPTION
                case 47:
                    break;
                //VOLUME_CORR_FACTOR
                case 48:
                    measureResult.MeasureSetup.VolumeCorrectionFactor = (double)value;
                    measureResult.OriginalMeasureSetup.VolumeCorrectionFactor = (double)value;
                    break;
                //VOLUME_CORR_FACTOR_200
                case 49:
                    break;
                //VOLUME_CORR_FACTOR_400
                case 50:
                    break;
                //IMPORTED_RESULT
                case 51:
                    break;
                case 53:
                    measureResult.MeasuredAt = (DateTime)value;
                    measureResult.MeasuredAtTimeZone = TimeZoneInfo.Utc;
                    break;
                //SETUP_FILENAME
                case 56:
                    measureResult.MeasureSetup.Name = (string)value;
                    break;
                //HIGH_SECURITY_RESULT
                case 57:
                    break;
                //HIGH_SECURITY_SETUP
                case 58:
                    break;
            }


            /*
             * const int CREATOR = 52;
        const int CREATION_TIME = 53;
        const int MODIFICATOR = 54;
        const int MODIFICATION_TIME = 55;
            const int ACCUMULATED_CHANNEL_DATA = 1;
        const int AUTO_RETRY = 2;
        const int CALIBRATION_FILENAME = 3;
        const int CAPILLARY_DIAMETER = 4;
        const int CHANNEL_DATA = 5;
        const int COMMENT = 6;
        const int CYCLES = 7;
        const int COUNT_RETRIES = 8;
        const int COUNTS_ABOVE_CALIB = 9;
        const int COUNTS_BELOW_CALIB = 10;
        const int COUNTS_BELOW_MEAS = 11;
        const int COUNTS_PREVIOUS_MEAS = 12;
        const int CURSOR_LEFT = 13;
        const int CURSOR_RIGHT = 14;
        const int DEVIATION_CONTROL = 15;
        const int DILUTION = 16;
        const int FILENAME = 17;
        const int FROM_DIAMETER = 18;
        const int MAXIMUM_DEVIATION = 19;
        const int NORM_CURSOR_LEFT = 20;
        const int NORM_CURSOR_RIGHT = 21;
        const int SMOOTHING_ON = 22;
        const int SMOOTH_WIDTH = 23;
        const int TIME = 24;
        const int TO_DIAMETER = 25;
        const int VOLUME = 26;
        const int Y_SCALE_MANUAL = 27;
        const int Y_SCALE_MANUAL_MAXIMUM = 28;
        const int Y_SCALE_PAR = 29;
        const int TOTAL_COUNTS = 30;
        const int LINK_CURSORS = 31;
        const int LINK_NORM_CURSORS = 32;
        const int ENABLE_PERCENT_CALCULATION = 33;
        const int PERCENT_CALCULATION_TYPE = 34;
        const int ENABLE_AGGREGATION_CORRECTION = 35;
        const int AGGREGATION_CORRECTION_TYPE = 36;
        const int AGGREGATION_CORRECTION_PEAK_VOLUME = 37;
        const int AUTO_EDIT_COMMENT_ON = 38;
        const int AUTO_PRINT_ON = 39;
        const int AUTO_SAVE_MEAS_ON = 40;
        const int AUTO_SAVE_EXPORT_ON = 41;
        const int AUTO_SAVE_PATH = 42;
        const int AUTO_SAVE_BASE_NAME = 43;
        const int SERIAL_NUMBER = 44;
        const int OPTION_FLAGS = 45;
        const int CONCENTRATION = 46;
        const int DESCRIPTION = 47;
        const int VOLUME_CORR_FACTOR = 48;
        const int VOLUME_CORR_FACTOR_200 = 49;
        const int VOLUME_CORR_FACTOR_400 = 50;
        const int IMPORTED_RESULT = 51;
        
        const int SETUP_FILENAME = 56;
        const int HIGH_SECURITY_RESULT = 57;
        const int HIGH_SECURITY_SETUP = 58;
        const int LAST_PARAMETER_ID = 58;
        */
        }


        private static void GetFieldById(uint id, out string propertyName)
        {
            switch (id)
            {
                //AUTO_RETRY
                case 2:
                    propertyName = "AUTO_RETRY";
                    break;
                //CALIBRATION_FILENAME
                case 3:
                    propertyName = "CALIBRATION_FILENAME";
                    break;
                //CAPILLARY_DIAMETER
                case 4:
                    propertyName = "CAPILLARY_DIAMETER";
                    break;
                //CHANNEL_DATA
                case 5:
                    propertyName = "CHANNEL_DATA";
                    break;
                //COMMENT
                case 6:
                    propertyName = "COMMENT";
                    break;
                //CYCLES
                case 7:
                    propertyName = "CYCLES";
                    break;
                //COUNT_RETRIES
                case 8:
                    propertyName = "COUNT_RETRIES";
                    break;
                //COUNTS_ABOVE_CALIB
                case 9:
                    propertyName = "COUNTS_ABOVE_CALIB";
                    break;
                //COUNTS_BELOW_CALIB
                case 10:
                    propertyName = "COUNTS_BELOW_CALIB";
                    break;
                //COUNTS_BELOW_MEAS
                case 11:
                    propertyName = "COUNTS_BELOW_MEAS";
                    break;
                //COUNTS_PREVIOUS_MEAS
                case 12:
                    propertyName = "COUNTS_PREVIOUS_MEAS";
                    break;
                //CURSOR_LEFT
                case 13:
                    propertyName = "CURSOR_LEFT";
                    break;
                //NORM_CURSOR_LEFT
                case 20:
                    propertyName = "NORM_CURSOR_LEFT";
                    break;
                //CURSOR_RIGHT
                case 14:
                    propertyName = "CURSOR_RIGHT";
                    break;
                //NORM_CURSOR_RIGHT
                case 21:
                    propertyName = "NORM_CURSOR_RIGHT";
                    break;
                //DEVIATION_CONTROL
                case 15:
                    propertyName = "DEVIATION_CONTROL";
                    break;
                //DILUTION
                case 16:
                    propertyName = "DILUTION";
                    break;
                //FILENAME
                case 17:
                    propertyName = "FILENAME";
                    break;
                //FROM_DIAMETER
                case 18:
                    propertyName = "FROM_DIAMETER";
                    break;
                //MAXIMUM_DEVIATION
                case 19:
                    propertyName = "MAXIMUM_DEVIATION";
                    break;
                //SMOOTHING_ON
                case 22:
                    propertyName = "SMOOTHING_ON";
                    break;
                //SMOOTH_WIDTH
                case 23:
                    propertyName = "SMOOTH_WIDTH";
                    break;
                //TIME
                case 24:
                    propertyName = "TIME";
                    break;
                //TO_DIAMETER
                case 25:
                    propertyName = "TO_DIAMETER";
                    break;
                //VOLUME
                case 26:
                    propertyName = "VOLUME";
                    break;
                //Y_SCALE_MANUAL
                case 27:
                    propertyName = "Y_SCALE_MANUAL";
                    break;
                //Y_SCALE_MANUAL_MAXIMUM
                case 28:
                    propertyName = "Y_SCALE_MANUAL_MAXIMUM";
                    break;
                //Y_SCALE_PAR
                case 29:
                    propertyName = "Y_SCALE_PAR";
                    break;
                // Total couts
                case 30:
                    propertyName = "Total couts";
                    break;
                //LINK_CURSORS
                case 31:
                    propertyName = "LINK_CURSORS";
                    break;
                //LINK_NORM_CURSORS
                case 32:
                    propertyName = "LINK_NORM_CURSORS";
                    break;
                //ENABLE_PERCENT_CALCULATION
                case 33:
                    propertyName = "ENABLE_PERCENT_CALCULATION";
                    break;
                //PERCENT_CALCULATION_TYPE
                case 34:
                    propertyName = "PERCENT_CALCULATION_TYPE";
                    break;
                //ENABLE_AGGREGATION_CORRECTION
                case 35:
                    propertyName = "ENABLE_AGGREGATION_CORRECTION";
                    break;
                //AGGREGATION_CORRECTION_TYPE
                case 36:
                    propertyName = "AGGREGATION_CORRECTION_TYPE";
                    break;
                //AGGREGATION_CORRECTION_PEAK_VOLUME
                case 37:
                    propertyName = "AGGREGATION_CORRECTION_PEAK_VOLUME";
                    break;
                //AUTO_EDIT_COMMENT_ON
                case 38:
                    propertyName = "AUTO_EDIT_COMMENT_ON";
                    break;
                //AUTO_PRINT_ON
                case 39:
                    propertyName = "AUTO_PRINT_ON";
                    break;
                //AUTO_SAVE_MEAS_ON
                case 40:
                    propertyName = "AUTO_SAVE_MEAS_ON";
                    break;
                //AUTO_SAVE_EXPORT_ON
                case 41:
                    propertyName = "AUTO_SAVE_EXPORT_ON";
                    break;
                //AUTO_SAVE_PATH
                case 42:
                    propertyName = "AUTO_SAVE_PATH";
                    break;
                //AUTO_SAVE_BASE_NAME
                case 43:
                    propertyName = "AUTO_SAVE_BASE_NAME";
                    break;
                //Serial Number
                case 44:
                    propertyName = "Serial Number";
                    break;
                //OPTION_FLAGS
                case 45:
                    propertyName = "OPTION_FLAGS";
                    break;
                //CONCENTRATION
                case 46:
                    propertyName = "CONCENTRATION";
                    break;
                //DESCRIPTION
                case 47:
                    propertyName = "DESCRIPTION";
                    break;
                //VOLUME_CORR_FACTOR
                case 48:
                    propertyName = "VOLUME_CORR_FACTOR";
                    break;
                //VOLUME_CORR_FACTOR_200
                case 49:
                    propertyName = "VOLUME_CORR_FACTOR_200";
                    break;
                //VOLUME_CORR_FACTOR_400
                case 50:
                    propertyName = "VOLUME_CORR_FACTOR_400";
                    break;
                //IMPORTED_RESULT
                case 51:
                    propertyName = "IMPORTED_RESULT";
                    break;
                //SETUP_FILENAME
                case 56:
                    propertyName = "SETUP_FILENAME";
                    break;
                //HIGH_SECURITY_RESULT
                case 57:
                    propertyName = "HIGH_SECURITY_RESULT";
                    break;
                //HIGH_SECURITY_SETUP
                case 58:
                    propertyName = "HIGH_SECURITY_SETUP";
                    break;
                default:
                    propertyName = "Unknown";
                    break;
            }
        }
    }
}
