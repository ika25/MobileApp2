using System;

namespace TestOneDriveSdk_001.Helpers
{
    public static class LongExtensions
    {
        public static string ConvertSize(this long? sizeInBytes)
        {
            if (sizeInBytes == null)
            {
                return "null";
            }

            if (sizeInBytes > 1024 * 1024 * 1024)
            {
                return $"{((sizeInBytes / 1024.0) / 1024.0 / 1024):N2} GB";
            }

            if (sizeInBytes > 1024 * 1024)
            {
                return $"{((sizeInBytes / 1024.0) / 1024.0):N2} MB";
            }

            if (sizeInBytes > 1024)
            {
                return $"{(sizeInBytes / 1024.0):N2} kB";
            }

            return $"{sizeInBytes} bytes";
        }

        public static string ConvertDuration(this long? milliseconds)
        {
            if (milliseconds == null)
            {
                return "null";
            }

            return TimeSpan.FromMilliseconds((double)milliseconds).ToString();
        }
    }
}
