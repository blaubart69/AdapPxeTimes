using System;
using System.Collections;
using System.Collections.Generic;
using Spi;

namespace PXELogParser
{
    class Opts
    {
        public IList<string> Filenames;
        public bool verbose = false;
        public string errorFilename;

        private Opts()
        {

        }
        public static bool ParseOpts(string[] args, out Opts opts)
        {
            opts = null;
            bool showhelp = false;

            Opts tmpOpts = new Opts() { };
            var cmdOpts = new BeeOptsBuilder()
                .Add('e', "err",           OPTTYPE.VALUE, "filename for error messages", o => tmpOpts.errorFilename = o)
                .Add('v', "verbose",       OPTTYPE.BOOL,  "show some debug output", o => tmpOpts.verbose = true)
                .Add('h', "help",          OPTTYPE.BOOL,  "show help", o => showhelp = true)
                .GetOpts();

            tmpOpts.Filenames = BeeOpts.Parse(args, cmdOpts, (string unknownOpt) => Console.Error.WriteLine($"unknow option [{unknownOpt}]"));

            if (showhelp)
            {
                Console.WriteLine(
                      "\nusage: PXELogParser [OPTIONS] [Filenames...]"
                    + "\n\nOptions:");
                BeeOpts.PrintOptions(cmdOpts);
                return false;
            }

            opts = tmpOpts;
            return true;
        }
    }
}