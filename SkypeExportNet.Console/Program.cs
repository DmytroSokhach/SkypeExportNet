namespace SkypeExportNet.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Mono.Options;
    using Console = System.Console;

    class Program
    {
        const string forbiddenChars = "\\/:?\"<>|*";
        bool isForbiddenCharacter(char c)
        {
            return forbiddenChars.Contains(c.ToString());
        }

        static string makeSafeFilename(string input, char replacement)
        {
            string result = input;
            foreach (var c in forbiddenChars)
            {
                result = input.Replace(c, replacement);
            }

            return result;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Skype History Exporter .NET v1.0.0 Stable\n" +
          "\tWEBSITE: [ https://github.com/DmitriySokhach/SkypeExportNet ]\n"); // helps people find updated versions

            // prepare command line parameters
            var options = new OptionSet();            
            var shouldShowHelp = false;
            var dbPath = "./main.db";
            var outPath = "./ExportedHistory";
            var contacts = new List<string>();
            var timeFormat = 2;
            var timeReference = 0;

            options
                .Add("help|h", "show this help message", h => shouldShowHelp = h != null)
                .Add("db|i", "path to your Skype profile's main.db. Default: " + dbPath, s => { dbPath = s; })
                .Add("outpath|o",
                    "path where all html files will be written; will be created if missing. Default: " +
                    "\"./ExportedHistory\"",
                    s => { outPath = s; })
                .Add("contacts|c", "space-separated list of the SkypeIDs to output; defaults to blank which outputs all contacts",
                    s =>
                    {
                        contacts = s.Split(' ').ToList();
                    })
                .Add("timefmt|t", "format of timestamps in history output; set to \"12h\" for 12-hour clock (default), \"24h\" for a 24-hour clock, \"utc12h\" for UTC-based 12-hour clock, or \"utc24h\" for UTC-based 24-hour clock",
                    s =>
                    {
                        timeFormat = s.Contains("24") ? 2 : 1;
                        timeReference = s.Contains("utc") ? 0 : 1;
                    });


            List<string> extra;
            try
            {
                // parse the command line
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                // output some error message
                Console.Write("SkypeExporterNet.Console: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `SkypeExporterNet.Console --help' for more information.");
                return;
            }

            if (shouldShowHelp)
            {
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
            }

            // verify the provided database and output paths and turn them into boost filesystem objects, then create the output path if needed
            //fs::path dbPath(vm["db"].as< string > ());
            //fs::path outPath(vm["outpath"].as< string > ());
            //dbPath.make_preferred(); // formats all slashes according to operating system
            //outPath.make_preferred();
            try
            {
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine("\nError: Database " + dbPath + " does not exist at the provided path!\n\n");
                }
                if (new FileInfo(dbPath).Length == 0)
                {
                    Console.WriteLine("\nError: Database " + dbPath + " is empty!\n\n");
                }
                if (Directory.Exists(outPath) && !File.GetAttributes(outPath).HasFlag(FileAttributes.Directory))
                {
                    Console.WriteLine("\nError: Output path " + outPath + " already exists and is not a directory!\n\n");
                }
                else if (!Directory.Exists(outPath))
                {
                    // outPath either exists and is a directory, or doesn't exist.
                    // we must now create the path if missing. will throw an exception on errors such as lack of write permissions.
                    Directory.CreateDirectory(outPath); // creates any missing directories in the given path.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nError: " + ex.Message + "\n");
            }

            // if they've provided a space-separated list of contacts to output, we need to tokenize it and store the SkypeIDs
            //std::map<string, bool> outputContacts; // filled with all contacts to output, or blank to output all
            //std::map<string, bool>::iterator outputContacts_it;
            //if (vm.count("contacts"))
            //{
            //    boost::char_separator<char> sep(" ");
            //    boost::tokenizer<boost::char_separator<char>> tokens(vm["contacts"].as< string > (), sep );
            //    for (boost::tokenizer<boost::char_separator<char>>::iterator identities_it(tokens.begin()); identities_it != tokens.end(); ++identities_it)
            //    {
            //        outputContacts_it = outputContacts.find((*identities_it));
            //        if (outputContacts_it == outputContacts.end())
            //        { // makes sure we only add each unique skypeID once, even if the idiot user has provided it multiple times
            //            outputContacts.insert(std::pair<string, bool>((*identities_it), false)); // NOTE: we initialize the skypeID as false, and will set it to true if it's been output
            //        }
            //    }
            //}

            // alright, let's begin output...
            try
            {
                // open Skype history database
                var sp = new SkypeParser(dbPath);

                // display all options (input database, output path, and all names to output (if specified))
                Console.WriteLine("  DATABASE: [ " + dbPath + " ]\n" // note: no newline prefix (aligns it perfectly with version header)
                          + "   TIMEFMT: [ \"" + (timeFormat == 1 ? "12h" : "24h") + " " + (timeReference == 0 ? "UTC" : "Local Time") + "\" ]\n"
                          + "    OUTPUT: [ " + outPath + " ]\n");
                //if (outputContacts.size() > 0)
                //{
                //    Console.WriteLine("  CONTACTS: [ \"");
                //    for (std::map<string, bool>::const_iterator it(outputContacts.begin()); it != outputContacts.end(); ++it)
                //    {
                //        Console.WriteLine((*it).first;
                //        if (boost::next(it) != outputContacts.end()) { Console.WriteLine("\", \""); } // appended after every element except the last one
                //    }
                //    Console.WriteLine("\" ]\n\n");
                //}
                //else
                {
                    Console.WriteLine("  CONTACTS: [ \"*\" ]\n\n");
                }

                // grab a list of all contacts encountered in the database
                var users = sp.getSkypeUsers();

                // output statistics
                Console.WriteLine("Found " + users.Count + " contacts in the database...\n\n");

                // output contacts, skipping some in case the user provided a list of contacts to export
                Dictionary<string, bool> outputContacts_it = users.ToDictionary(x => x, y => false);
                foreach (var skypeID in users)
                {
                    // skip if we're told to filter contacts
                    //outputContacts_it = outputContacts.find((*it)); // store iterator here since we'll set it to true after outputting, if contact filters are enabled
                    //if (outputContacts.size() > 0 && (outputContacts_it == outputContacts.end())) { continue; } // if no filters, it's always false; if filters it's true if the contact is to be skipped

                    // construct the final path to the log file for this user
                    string safeFilename = makeSafeFilename(skypeID, '$'); // replace illegal characters with $ instead; some skype IDs are "live:username", and will become "live$username"
                    var logPath = outPath + safeFilename + ".skypelog.htm"; // appends the log filename and chooses the appropriate path separator

                    // output exporting header
                    Console.WriteLine(" * Exporting: " + skypeID + " (" + sp.getDisplayNameAtTime(skypeID, -1) + ")\n");
                    Console.WriteLine("   => " + logPath + "\n");
                    sp.exportUserHistory(skypeID, logPath, timeFormat, timeReference);
                    //if (outputContacts.size() > 0) { (*outputContacts_it).second = true; } // since filters are enabled and we've come here, we know we've output the person as requested, so mark them as such
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while processing Skype database: \"" + ex.Message + "\".\n");
            }

            // check for any missing IDs if filtered output was requested
            //for (std::map<string, bool>::const_iterator it(outputContacts.begin()); it != outputContacts.end(); ++it)
            //{
            //    if ((*it).second == false)
            //    { // a requested ID that was not found in the database
            //        Console.WriteLine(" * Not Found: " + (*it).first + "\n");
            //    }
            //}

            Console.WriteLine("\nExport finished.\n");

        }
    }
}
