using OLS.Casy.Core;
using OLS.Casy.Models;
using System;
using System.IO;

namespace OLS.Casy.Controller.Service
{
    public static class ResponseDataConverter
    {
        public static Statistic ReadStatisticsData(byte[] data)
        {
            var result = new Statistic();

            using (MemoryStream ms = new MemoryStream(data))
            {
                for(int i = 0; i < 4; i++)
                {
                    CapillaryStatistic capillaryStatistic = ReadCapillaryStatisticData(ms);
                    if (capillaryStatistic.Diameter != 0)
                    {
                        result.CapillaryStatistics.Add(capillaryStatistic);
                    }
                }

                for(int i = 0; i < 48; i++)
                {
                    ErrorStatistic errorStatistic = ReadErrorStatisticData(ms);
                    result.ErrorStatistics.Add(errorStatistic);
                }

                byte[] buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                var minute = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                var hour = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                var day = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                var month = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                var year = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                if (year == 0 && month == 0 && day == 0)
                {
                    result.LastPowerOn = null;
                }
                else
                {
                    result.LastPowerOn = new DateTime(year, month, day, hour, minute, 0);
                }

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                minute = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                hour = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                day = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                month = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                year = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                if (year == 0 && month == 0 && day == 0)
                {
                    result.LastUpdateWorkingCounter = null;
                }
                else
                {
                    result.LastUpdateWorkingCounter = new DateTime(year, month, day, hour, minute, 0);
                }

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                minute = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                hour = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                day = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                month = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                year = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                if (year == 0 && month == 0 && day == 0)
                {
                    result.LastResetStatistics = null;
                }
                else
                {
                    result.LastResetStatistics = new DateTime(year, month, day, hour, minute, 0);
                }

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                minute = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                hour = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                buffer = new byte[2];
                ms.Read(buffer, 0, 2);
                day = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

                result.PowerUpTime = new TimeSpan(day, hour, minute, 0);

                buffer = new byte[4];
                ms.Read(buffer, 0, 4);
                result.PowerOnCount = SwapHelper.SwapBytes(BitConverter.ToUInt32(buffer, 0));

                buffer = new byte[4];
                ms.Read(buffer, 0, 4);
                result.AveragePowerOnTime = SwapHelper.SwapBytes(BitConverter.ToUInt32(buffer, 0));

                buffer = new byte[4];
                ms.Read(buffer, 0, 4);
                result.AveragePowerOffTime = SwapHelper.SwapBytes(BitConverter.ToUInt32(buffer, 0));
            }

            return result;
        }

        private static CapillaryStatistic ReadCapillaryStatisticData(MemoryStream ms)
        {
            CapillaryStatistic result = new CapillaryStatistic();

            byte[] buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.Diameter = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.LastPosition200 = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.LastPosition400 = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[4];
            ms.Read(buffer, 0, 4);
            result.CleanCount = SwapHelper.SwapBytes(BitConverter.ToUInt32(buffer, 0));

            buffer = new byte[4];
            ms.Read(buffer, 0, 4);
            result.MeasureCount = SwapHelper.SwapBytes(BitConverter.ToUInt32(buffer, 0));

            for(int i = 0; i < 10; i++)
            {
                MeasureStatistic measureStatistic = ReadMeasureStatisticData(ms);
                result.Measure200Statistics.Add(measureStatistic);
            }

            for (int i = 0; i < 10; i++)
            {
                MeasureStatistic measureStatistic = ReadMeasureStatisticData(ms);
                result.Measure400Statistics.Add(measureStatistic);
            }
            return result;
        }

        private static MeasureStatistic ReadMeasureStatisticData(MemoryStream ms)
        {
            MeasureStatistic result = new MeasureStatistic();
            result.ErrorCode = new ushort[3];

            byte[] buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.ErrorCode[0] = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.ErrorCode[1] = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.ErrorCode[2] = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var minute = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var hour = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var day = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var month = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var year = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            if (year == 0 && month == 0 && day == 0)
            {
                result.Timestamp = null;
            }
            else
            {
                result.Timestamp = new DateTime(year, month, day, hour, minute, 0);
            }

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.Time2 = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.Time3 = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.BubbleTime = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            return result;
        }

        private static ErrorStatistic ReadErrorStatisticData(MemoryStream ms)
        {
            ErrorStatistic result = new ErrorStatistic();

            byte[] buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var minute = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var hour = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var day = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var month = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            var year = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            if (year == 0 && month == 0 && day == 0)
            {
                result.FirstOccured = null;
            }
            else
            {
                result.FirstOccured = new DateTime(year, month, day, hour, minute, 0);
            }

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            minute = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            hour = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            day = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            month = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            year = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            if (year == 0 && month == 0 && day == 0)
            {
                result.LastOccured = null;
            }
            else
            {
                result.LastOccured = new DateTime(year, month, day, hour, minute, 0);
            }

            buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            result.OccurenceCount = SwapHelper.SwapBytes(BitConverter.ToUInt16(buffer, 0));

            return result;
        }
    }
}
