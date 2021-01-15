using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common.Helper
{
    public class MyLogger
    {
        public static void LogException(Exception ex)
        {
            string filePath = @"D:\Projects\Error.txt";

            Exception exception = ex;

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine();

                while (exception != null)
                {
                    writer.WriteLine(exception.GetType().FullName);
                    writer.WriteLine("Message : " + exception.Message);
                    writer.WriteLine("StackTrace : " + exception.StackTrace);

                    exception = exception.InnerException;
                }
            }
        }
    }
}
