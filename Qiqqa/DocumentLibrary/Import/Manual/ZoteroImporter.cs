﻿using System;
using System.Text.RegularExpressions;
using Qiqqa.DocumentLibrary.WebLibraryStuff;
using Qiqqa.UtilisationTracking;
using Utilities;
using Utilities.Strings;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;


namespace Qiqqa.DocumentLibrary.Import.Manual
{
    public class ZoteroImporter : BibTeXImporter
    {
        private readonly string _importBasePath;

        public ZoteroImporter(WebLibraryDetail web_library_detail, string filename)
            : base(web_library_detail, filename)
        {
            FeatureTrackingManager.Instance.UseFeature(Features.Library_ImportFromZotero);

            // For Zotero export, the files all live under \Files,
            // e.g. if export is c:\temp\bla.bib, a file might be c:\temp\files\20\foo.pdf
            _importBasePath = Path.GetDirectoryName(filename);
        }


        public override ParseFileResult GetResult()
        {

            //string matchFilenameRegex = "([^;:]*) :  ([^;:]*) :  ([^;:]*)";
            string matchFilenameRegex = "(.*?):(.*):(.*)";
            Regex rx = new Regex(matchFilenameRegex);

            foreach (var entry in Entries)
            {
                try
                {
                    if (entry.Item.ContainsField("file"))
                    {
                        #region Do some post processing on the file
                        /*
                         * Samples:
                         *
                         *  file = {sample.pdf:files/10/sample.pdf:application/pdf}
                         *  file = {Google Books Link:undefined:text/html}
                         *  file = {chris bishop - Google Scholar:files/21/scholar.html:text/html}
                         *
                         * *NOTE THE MULTIPLE ATTACHMENTS HERE
                         * file = {Analytics_www.qiqqa.com_20100531_(Qiqqa_utilisation).pdf:files/24/Analytics_www.qiqqa.com_20100531_(Qiqqa_utilisation).pdf:application/pdf;Namecheap.com - Checkout : Secure Payment:files/26/payment.html:text/html}
                         */

                        // Since there can be multiple attachments, we take the first one with mime type application/pdf, with a valid file.
                        entry.FileType = null; // Assume we've not found a workable attachment

                        string fileValue = entry.Item["file"];

                        foreach (string attachmentValue in fileValue.Split(';'))
                        {
                            Match match = rx.Match(attachmentValue);
                            if (match.Success)
                            {
                                string fileType = match.Groups[3].Value.ToLower();
                                if (String.CompareOrdinal(fileType, PDF_MIMETYPE) == 0)
                                {
                                    string fn = match.Groups[2].Value;
                                    fn = fn.Replace("\\_", "_"); // Zotero escapes underscores. I get the feeling from http://www.citeulike.org/groupforum/1245 that others might not.
                                    //fn = fn.Replace("/", "\\"); // Convert to windows slashes for directories.

                                    try
                                    {
                                        fn = Path.GetFullPath(Path.Combine(_importBasePath, fn));

                                        if (File.Exists(fn))
                                        {
                                            entry.FileType = "pdf"; // Normalized with other importers
                                            entry.Filename = fn;
                                            break; // We've found a suitable attachment
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logging.Warn(ex, "Ignoring problems with weird filenames like \"undefined\": {0}", fn);
                                    }
                                }
                            }
                            else
                            {
                                Logging.Warn("Non-conformant file key in Zotero import: {0}", fileValue);
                            }
                        }
                        #endregion
                    }


                    // Parse notes.
                    string notes = entry.Item["annote"];
                    if (!String.IsNullOrEmpty(notes))
                    {
                        //annote = {\textless}p{\textgreater}zotnotes1{\textless}/p{\textgreater},

                        // Turn back to pseudo html - we may want to support this format later.
                        notes = notes.Replace(@"{\textless}", "<").Replace(@"{\textgreater}", ">");

                        notes = notes.Replace("<p>", "\r");  //Some basic support

                        // Remove tags
                        notes = StringTools.StripHtmlTags(notes, " ");

                        // Change N to 1 repeated spaces due to missing tags
                        notes = Regex.Replace(notes, @"[\s]+", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                        entry.Notes = notes.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Logging.Warn(ex, "Exception while parsing Zotero import");
                }
            }

            return CreateFinalResult();
        }
    }
}
