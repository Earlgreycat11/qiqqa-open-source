﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;


namespace QiqqaLegacyFileFormats          // namespace Utilities.OCR
{
    [Serializable]
    public class WordList : List<Word>
    {
        private static readonly string FILE_HEADER_QIQQA_OCR = "QiqqaOCR.";
        private static readonly string FILE_HEADER = "# Generated by: " + FILE_HEADER_QIQQA_OCR;
        private static readonly string FILE_VERSION = "# Version: 3";
        private static readonly string LIST_SOURCE_TYPE = "# List source: ";
        private static readonly string SYSTEM_CULTURE = "# System culture: ";
        private static readonly string PAGE_PREAMBLE = "@PAGE: ";

        public static void WriteToFile(string filename, Dictionary<int, WordList> word_lists, string list_source)
        {
            List<string> lines = new List<string>();
            lines.Add(FILE_HEADER);
            lines.Add(FILE_VERSION);
            lines.Add(LIST_SOURCE_TYPE + list_source);
            lines.Add(SYSTEM_CULTURE + CultureInfo.CurrentCulture);

            // Process each of the pages
            foreach (var pair in word_lists)
            {
                lines.Add(PAGE_PREAMBLE + String.Format("{0}\n", pair.Key));

                // Process each of the words on the page
                foreach (Word word in pair.Value)
                {
                    lines.Add(String.Format("{0:0.00000},{1:0.00000},{2:0.00000},{3:0.00000}:{4}\n", word.Left, word.Top, word.Width, word.Height, word.Text));
                }
            }

            File.WriteAllLines(filename, lines.ToArray());
        }

        public static Dictionary<int, WordList> ReadFromFile(string filename, int default_page = 0)
        {
            Dictionary<int, WordList> word_lists = new Dictionary<int, WordList>();
            WordList current_word_list = null;
            int current_page = default_page;

            string[] lines = File.ReadAllLines(filename);

            // Get all the headers out of the file
            Dictionary<string, string> headers = new Dictionary<string, string>();
            for (int i = 0; i < 10 && i < lines.Length; ++i)
            {
                string line = lines[i];
                if (line.StartsWith("#"))
                {
                    string[] pair = line.Split(':');
                    if (pair.Length > 1)
                    {
                        headers[pair[0]] = pair[1].Trim();
                    }
                }
            }

            // Check the filetype
            if (!headers.ContainsKey("# Generated by") || !headers["# Generated by"].Contains(FILE_HEADER_QIQQA_OCR))
            {
                throw new Exception("Unrecognised file format");
            }
            // Check the version
            if (!headers.ContainsKey("# Version"))
            {
                throw new Exception("OCR file too old - it is missing a version number");
            }
            int version = Convert.ToInt32(headers["# Version"]);
            if (version < 2)
            {
                throw new Exception("OCR file too old (pre v2)");
            }

            try
            {
                // Process each line
                foreach (string line in lines)
                {
                    // Ignore comments
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    // Skip blanks
                    if (String.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    // New page?                    
                    if (line.StartsWith(PAGE_PREAMBLE))
                    {
                        string page_string = line.Substring(PAGE_PREAMBLE.Length);
                        int page = Convert.ToInt32(page_string);

                        current_page = page;
                        current_word_list = new WordList();
                        word_lists[page] = current_word_list;

                        continue;
                    }

                    // Parse the line
                    int colon_pos = line.IndexOf(':');
                    string locations_string = line.Substring(0, colon_pos);
                    string[] locations = locations_string.Split(',');

                    Word word = new Word();
                    word.Left = Convert.ToDouble(locations[0]);
                    word.Top = Convert.ToDouble(locations[1]);
                    word.Width = Convert.ToDouble(locations[2]);
                    word.Height = Convert.ToDouble(locations[3]);
                    word.Text = line.Substring(colon_pos + 1);
                    if (word.Width <= 0.0 || word.Height <= 0.0)
                    {
                        throw new Exception(String.Format("OCR file '{0}': format error: zero word width/height @PAGE {1}", filename, current_page));
                    }

                    // If we get this far and we don't yet have a page, assume the default page
                    if (null == current_word_list)
                    {
                        if (default_page < 1)
                        {
                            throw new Exception(String.Format("OCR file '{0}': format error: words without leading @PAGE", filename));
                        }
                        current_word_list = new WordList();
                        word_lists[default_page] = current_word_list;
                    }

                    current_word_list.Add(word);

                    continue;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid line format", ex);
            }

            return word_lists;
        }

        private string text_string = null;
        public string TextString
        {
            get
            {
                if (null != text_string)
                {
                    return text_string;
                }

                StringBuilder sb = new StringBuilder();
                foreach (Word word in this)
                {
                    sb.Append(word.Text);
                    sb.Append(' ');
                }

                text_string = sb.ToString();
                return text_string;
            }
        }

        private string text_string_lower = null;
        public string TextStringLower
        {
            get
            {
                if (null != text_string_lower)
                {
                    return text_string_lower;
                }

                text_string_lower = TextString.ToLower();
                return text_string_lower;
            }
        }
    }
}
