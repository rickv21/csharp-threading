using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public static class FileUtil
    {
        public static string ConvertBytesToHumanReadable(long bytes)
        {
            // const int scale = 1024;
            // string[] orders = ["TB", "GB", "MB", "KB", "B"];
            // long max = (long)Math.Pow(scale, orders.Length - 1);
            //
            // foreach (string order in orders)
            // {
            //     if (bytes > max)
            //     {
            //         return string.Format("{0:##.##} {1}", decimal.Divide(bytes, (decimal)max), order);
            //     }
            //
            //     if (max == 1)
            //         return string.Format("{0} {1}", bytes, order);
            //
            //     max /= scale;
            // }

            return "0 B";
        }
    }
}
