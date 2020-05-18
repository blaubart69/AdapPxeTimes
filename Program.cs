using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PXELogParser
{
    class Program
    {
        /*
          
Start frame: -> Startzeit & MAC Address
2020-05-14 09:52:40,458 - INFO - Sent message to server for pxe request : 98:FA:9B:C2:52:F0:8DDE084C-2E3F-11B2-A85C-F4EB68FB2E6F - PXEServerHelper - TID=67, PXEServerProtocol

Ende1: Endzeit MAC + IP (Wenn PXE Boot gewollt)
2020-05-14 09:52:45,552 - INFO - Sent PXEResponse for : MAC: 98:FA:9B:C2:52:F0, SMBIOS: null, IPAddress: /10.15.133.145 - PXEServerProtocol - TID=342920, P2PDiscoveryExecutionThread

Ende2: Endzeit MAC (Wenn kein PXE boot)
2020-05-15 10:45:35,350 - INFO - Set sentReqToServer to false for key 98:FA:9B:C2:52:F0:8DDE084C-2E3F-11B2-A85C-F4EB68FB2E6F - PXEServerHelper - TID=60572, ConsumerTask: Sender Id = [0], Retry Level : 0

         */

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
            
                                             //2020-05-15 09:22:10,764
            Regex startPattern = new Regex(@"^(....-..-.. ..:..:..,...) - INFO - Sent message to server for pxe request : (..:..:..:..:..:..):", RegexOptions.Compiled);
            /*
                                                     //Sent PXEResponse for : MAC: 94:C6:91:B8:BE:48, SMBIOS: null, IPAddress: /10.64.100.97 - PXEServerProtocol
            Regex endPXE1 = new Regex(@"^(....-..-.. ..:..:..,...) - INFO - Sent PXEResponse for : MAC: (..:..:..:..:..:..), SMBIOS:.+?, IPAddress: /([0-9\.]+)", RegexOptions.Compiled);

                                                     // Set sentReqToServer to false for key 94:C6:91:B7:0A:FA:88A80B00-DCAC-11E8-A426-68FF72D13600 - PXEServerHelper - TID=56511, ConsumerTask: Sender Id = [0], Retry Level : 0
            Regex endPXE0 = new Regex(@"^(....-..-.. ..:..:..,...) - INFO - Set sentReqToServer to false for key (..:..:..:..:..:..):", RegexOptions.Compiled);
            */

            //2020 - 05 - 12 09:37:14,864 - INFO - SmsBootPackageIdReply recvd from server: Name of the message: SmsBootPackageIdReply, Sender ID: 0, Receiver ID: 85248, Queue ID: 1, CORRELATION ID: 1588907218697, ORIGINAL CORRELATION ID: 1589011600559, ORIGINAL RECEIVER ID: 0, REPLY TO: 1, REPLY TO IP: / 10.60.116.6, IS REPLY: true, TRANSPORT: 0, Attribute count: 8Attribute names and their values: Name: BootPackageId ,Value: UC10092F; Name: IsMandatory ,Value: true; Name: IsValid ,Value: true; Name: MAC ,Value: 98:FA: 9B: C2: 52:F0; Name: PxeAdvertId ,Value: UC120B72; Name: RemoteRvp ,Value: 3655; Name: RemoteRvpIP ,Value: 10.15.133.51; Name: SMBIOS ,Value: 8DDE084C - 2E3F - 11B2 - A85C - F4EB68FB2E6F; -PXEServerHelper - TID = 215742, ConsumerTask: Sender Id = [0], Retry Level: 0
            //2020 - 05 - 15 10:45:35,350 - INFO - SmsBootPackageIdReply recvd from server: Name of the message: SmsBootPackageIdReply, Sender ID: 0, Receiver ID: 85248, Queue ID: 1, CORRELATION ID: 1588994658990, ORIGINAL CORRELATION ID: 1589011874474, ORIGINAL RECEIVER ID: 0, REPLY TO: 1, REPLY TO IP: / 10.60.116.6, IS REPLY: true, TRANSPORT: 0, Attribute count: 7Attribute names and their values: Name: IsAbort ,Value: true; Name: IsValid ,Value: true; Name: MAC ,Value: 98:FA: 9B: C2: 52:F0; Name: Reason ,Value: Advertisement has already been executed for this machine :UC120B72; Name: RemoteRvp, Value: 3655; Name: RemoteRvpIP ,Value: 10.15.133.51; Name: SMBIOS ,Value: 8DDE084C - 2E3F - 11B2 - A85C - F4EB68FB2E6F; -PXEServerHelper - TID = 60572, ConsumerTask: Sender Id = [0], Retry Level: 0


            //2020-05-15 09:50:27,870 - INFO - SmsBootPackageIdReply recvd from server: Name of the message: SmsBootPackageIdReply, xxxx Name: BootPackageId ,Value: PE40X86; Name: IsMandatory ,Value: true; Name: IsValid ,Value: true; Name: MAC ,Value: 00:50:56:A5:47:B9; Name: PxeAdvertId ,Value: ; Name: RemoteRvp ,Value: 56741; Name: RemoteRvpIP ,Value: 10.58.129.103; Name: SMBIOS ,Value: 0D9D2542-F059-832E-A814-BA73685E6E52; - PXEServerHelper - TID=57960, ConsumerTask: Sender Id = [0], Retry Level : 0
            Regex endPXE1 = new Regex(@"^(....-..-.. ..:..:..,...) - INFO - SmsBootPackageIdReply recvd from server: .+?(Name: BootPackageId ,Value: ([^;]+);).+?(Name: MAC ,Value: (..:..:..:..:..:..);).+?(Name: RemoteRvpIP ,Value: ([0-9\.]+);)", RegexOptions.Compiled);

            Regex endPXE0 = new Regex(@"^(....-..-.. ..:..:..,...) - INFO - SmsBootPackageIdReply recvd from server: .+?(Name: IsAbort ,Value: true;).+?(Name: MAC ,Value: (..:..:..:..:..:..);).+?(Name: RemoteRvpIP ,Value: ([0-9\.]+);)", RegexOptions.Compiled);


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
                    rdr = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                }


                using (rdr)
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        Match m = startPattern.Match(line);
                        if (m.Success)
                        {

                            DateTime startTime = ParseLogTime(m.Groups[1].Value);
                            string mac = m.Groups[2].Value;

                            if (opts.verbose)
                            {
                                Console.Out.WriteLine($"START: {startTime}\t{mac}");
                            }

                            HandleStart(ref sessions, startTime, mac, OnErrorHandler);
                            continue;
                        }

                        m = endPXE1.Match(line);
                        if (m.Success)
                        {
                            DateTime endTime = ParseLogTime(m.Groups[1].Value);
                            string bootImage = m.Groups[3].Value;
                            string mac = m.Groups[5].Value;
                            string IP = m.Groups[7].Value;
                            

                            if (opts.verbose)
                            {
                                Console.WriteLine($"END1: {endTime}\t{mac}\t{IP}\t{bootImage}");
                            }

                            HandleEnd(in sessions, endTime, mac, IP, bootImage, OnErrorHandler);
                            sessions.Remove(mac);
                            continue;
                        }

                        m = endPXE0.Match(line);
                        if (m.Success)
                        {
                            DateTime endTime = ParseLogTime(m.Groups[1].Value);
                            string mac = m.Groups[4].Value;
                            string IP = m.Groups[6].Value;

                            if (opts.verbose)
                            {
                                Console.WriteLine($"END0: {endTime}\t{mac}");
                            }

                            HandleEnd(in sessions, endTime, mac, IP, "abort", OnErrorHandler);
                            sessions.Remove(mac);
                        }
                    }
                }
            }
            return 0;
        }

        private static void PrintEnd(string mac, DateTime start, DateTime end, string IP, string bootImage)
        {
            TimeSpan duration = end - start;
            //string niceDura = NiceDuration2(duration);
            string duraString = MikeDuration(duration);



            //Console.WriteLine($"{mac}\t{niceDura,12}\t{IP}\t{bootImage,-12}\t{start}");
            Console.WriteLine($"{mac}\t{duraString,12}\t{IP}\t{bootImage,-12}\t{start}");
        }

        private static void HandleEnd(in Dictionary<string, DateTime> sessions, DateTime endTime, string mac, string IP, string bootimage, Action<string> OnError)
        {
            if (sessions.TryGetValue(mac, out DateTime startTime))
            {
                PrintEnd(mac, startTime, endTime, IP, bootimage);
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
        public static string NiceDuration2(in TimeSpan duration)
        {
            string nice = duration.Milliseconds + "ms"; ;

            if (duration.Ticks >= TimeSpan.TicksPerSecond)
            {
                nice = duration.Seconds + "s " + nice;

                if (duration.Ticks >= TimeSpan.TicksPerMinute)
                {
                    nice = duration.Minutes + "m " + nice;

                    if (duration.Ticks >= TimeSpan.TicksPerHour)
                    {
                        nice = duration.Hours + "h " + nice;

                        if (duration.Ticks >= TimeSpan.TicksPerDay)
                        {
                            nice += duration.Days + "d ";
                        }
                    }
                }
            }

            return nice;
        }
        public static string MikeDuration(in TimeSpan duration)
        {
            //return duration.TotalSeconds.ToString("ss.fff");
            return duration.TotalSeconds.ToString("0.000");
            //return duration.ToString(@"s\.fff");
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
