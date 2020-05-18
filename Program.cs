using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace PXELogParser
{
    class Program
    {
        static int Main(string[] args)
        {
            Opts opts;
            if ( ! Opts.ParseOpts(args, out opts) )
            {
                return 99;
            }

            if ( opts.Filenames.Count == 0 )
            {
                opts.Filenames = new string[] { "-" };
            }
            
            Regex one = new Regex(
                 @"^(....-..-.. ..:..:..,...) - INFO - "
                +@"(Sent message to server for pxe request : (..:..:..:..:..:..)"
                +@"|SmsBootPackageIdReply recvd from server:.+?Attribute names and their values:  Name: (IsAbort|BootPackageId) ,Value: (.+?);"
                     +@".+?Name: MAC ,Value: (..:..:..:..:..:..);.+?Name: RemoteRvpIP ,Value: ([0-9\.]+);.+"
                 +")" , RegexOptions.Compiled);

            Dictionary<string, DateTime> sessions = new Dictionary<string, DateTime>(comparer: StringComparer.OrdinalIgnoreCase);

            Action<string> OnErrorHandler = null;
            if (opts.printErrors)
            {
                OnErrorHandler = Console.Error.WriteLine;
            }

            foreach (string filename in opts.Filenames)
            {
                TextReader rdr;
                if ( "-".Equals(filename) )
                {
                    rdr = Console.In;
                    if (opts.verbose)
                    {
                        Console.Error.WriteLine("reading from stdin");
                    }
                }
                else
                {
                    rdr = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan));
                }

                using (rdr)
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        Match m = one.Match(line);
                        if ( m.Success )
                        {
                            DateTime logTimestamp = ParseLogTime(m.Groups[1].Value);

                            if ( m.Groups[2].Value.StartsWith("Sent") )
                            {
                                HandleStart(ref sessions,
                                    startTime: logTimestamp,
                                    mac: m.Groups[3].Value,
                                    OnError: OnErrorHandler);
                            }
                            else
                            {
                                HandleEnd(
                                    ref sessions,
                                    endTime: logTimestamp,
                                    mac: m.Groups[6].Value,
                                    IP: m.Groups[7].Value,
                                    bootimage: "IsAbort".Equals(m.Groups[4].Value) ? "abort" : m.Groups[5].Value,
                                    OnError: OnErrorHandler);
                            }
                        }
                    }
                }
            }
            return 0;
        }
        private static void PrintEnd(string mac, DateTime start, DateTime end, string IP, string bootImage)
        {
            TimeSpan duration = end - start;
            string duraString = duration.TotalSeconds.ToString("0.000");

            Console.WriteLine($"{mac}\t{duraString,12}\t{IP}\t{bootImage,-12}\t{start}");
        }
        private static void HandleEnd(ref Dictionary<string, DateTime> sessions, DateTime endTime, string mac, string IP, string bootimage, Action<string> OnError)
        {
            if (sessions.TryGetValue(mac, out DateTime startTime))
            {
                PrintEnd(mac, startTime, endTime, IP, bootimage);
                sessions.Remove(mac);
            }
            else
            {
                OnError?.Invoke($"E: no START for {mac}");
            }
        }
        private static void HandleStart(ref Dictionary<string, DateTime> sessions, DateTime startTime, string mac, Action<string> OnError)
        {
            if ( sessions.ContainsKey(mac) )
            {
                OnError?.Invoke($"E: already a START for mac {mac}");
            }
            else
            {
                sessions.Add(mac, startTime);
            }
        }
        private static DateTime ParseLogTime(string logfiletime)
        {
            try
            {
                //  2020-05-14 09:52:40,458
                return DateTime.ParseExact(logfiletime, "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"X-Logtime: could not parse: [{logfiletime}]");
                throw ex;
            }
        }

    }
}
