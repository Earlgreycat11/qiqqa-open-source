﻿#if !HAS_NO_PROTOBUF

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;


namespace QiqqaLegacyFileFormats          // namespace Utilities.Language.TextIndexing
{
    public class WordIndex
    {
        private static readonly string INDEX_VERSION = "2.0";
        private static readonly int GANG_SIZE = 100;
        private string LIBRARY_INDEX_BASE_PATH;
        private bool use_make_reasonable_word;
        private object locker = new object();
        private Dictionary<string, int> fingerprint_to_document_ids = new Dictionary<string, int>();
        private Dictionary<int, string> document_id_to_fingerprints = new Dictionary<int, string>();
        private List<WordInWordIndex> word_in_word_indexes = new List<WordInWordIndex>();
        private Dictionary<string, WordInWordIndex> word_in_word_index_lookups = new Dictionary<string, WordInWordIndex>();


#if SAMPLE_LOAD_CODE

        public WordIndex(string LIBRARY_INDEX_BASE_PATH, bool use_make_reasonable_word)
        {
            this.LIBRARY_INDEX_BASE_PATH = LIBRARY_INDEX_BASE_PATH;
            this.use_make_reasonable_word = use_make_reasonable_word;

            CheckIndexVersion();

            ReadMasterList();
        }

        private string VersionFilename => Path.GetFullPath(Path.Combine(LIBRARY_INDEX_BASE_PATH, @"index_version.txt"));

        private void CheckIndexVersion()
        {
            string version = null;

            try
            {
                if (File.Exists(VersionFilename))
                {
                    string[] index_version_lines = File.ReadAllLines(VersionFilename);
                    version = index_version_lines[0];
                }
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem while trying to check the index version");
            }

            if (0 != String.Compare(version, INDEX_VERSION))
            {
                Logging.Warn("This index is out of date (it's version is {0}), so deleting the index.", version);

                DeleteIndex();
            }
        }

#region --- Working internals  ---------------------------------------------------------------------------------------------------------

        internal WordInWordIndex GetWordInWordIndex_LOCKER(string word, bool for_modification)
        {
            WordInWordIndex wiwi;

            // We have never seen this word before, so create an index entry from scratch
            if (!word_in_word_index_lookups.TryGetValue(word, out wiwi))
            {
                // If this is just a query request, dont create a new one
                if (!for_modification)
                {
                    return null;
                }


                // Make sure the last page of words is loaded
                if (0 < word_in_word_indexes.Count)
                {
                    LoadWord_LOCK(word_in_word_indexes[word_in_word_indexes.Count - 1]);
                }

                // Create the new wiwi
                wiwi = new WordInWordIndex(word, word_in_word_indexes.Count);

                // Add to our lookups
                word_in_word_indexes.Add(wiwi);
                word_in_word_index_lookups[wiwi.Word] = wiwi;
            }
            else // We have seen this word before
            {
                LoadWord_LOCK(wiwi);
            }

            // Set some access properties
            wiwi.last_accessed = DateTime.UtcNow;
            wiwi.last_flushed = DateTime.MinValue;
            wiwi.needs_flushing = wiwi.needs_flushing || for_modification;

            return wiwi;
        }

        internal int IndexSize
        {
            get
            {
                lock (locker)
                {
                    return word_in_word_index_lookups.Count;
                }
            }
        }

        private void ReadMasterList()
        {
            Logging.Info("+ReadMasterList");

            lock (locker)
            {
                try
                {
                    using (FileStream fs = File.OpenRead(GetFilename_MasterList()))
                    {
                        Headers headers = Serializer.Deserialize<Headers>(fs);

                        // First the documents
                        {
                            foreach (var header in headers.documents)
                            {
                                fingerprint_to_document_ids[header.Fingerprint] = header.DocumentId;
                                document_id_to_fingerprints[header.DocumentId] = header.Fingerprint;
                            }
                        }

                        // Then the words
                        {
                            foreach (var header in headers.words)
                            {
                                WordInWordIndex wiwi = new WordInWordIndex(header.Word, header.WordId, header.DocCount);

                                // Sanity check that they are in the right order
                                if (wiwi.WordId != word_in_word_indexes.Count)
                                {
                                    throw new Exception("The ordering of the word index is corrupt");
                                }

                                // Add to our maps
                                word_in_word_indexes.Add(wiwi);
                                word_in_word_index_lookups[wiwi.Word] = wiwi;
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    Logging.Warn(ex, "Unable to load index master list, so starting from scratch");
                }
            }

            Logging.Info("-ReadMasterList");
        }

        public void WriteMasterList()
        {
            Logging.Info("+WriteMasterList");

            string filename_temp = Path.GetTempFileName();

            lock (locker)
            {
                FlushAllWords_LOCK();
                PurgeAllWords_LOCK();

                using (FileStream fs = File.Open(filename_temp, FileMode.Create))
                {
                    Headers headers = new Headers();

                    // First the documents
                    {
                        headers.documents = new List<DocumentMapHeader>();
                        foreach (var pair in fingerprint_to_document_ids)
                        {
                            DocumentMapHeader header = new DocumentMapHeader
                            {
                                Fingerprint = pair.Key,
                                DocumentId = pair.Value
                            };
                            headers.documents.Add(header);
                        }
                    }

                    // Then the words
                    {
                        headers.words = new List<WordMapHeader>();
                        foreach (WordInWordIndex word_in_word_index in word_in_word_indexes)
                        {
                            WordMapHeader header = new WordMapHeader
                            {
                                Word = word_in_word_index.Word,
                                WordId = word_in_word_index.WordId,
                                DocCount = word_in_word_index.DocumentCount
                            };
                            headers.words.Add(header);
                        }
                    }

                    Serializer.Serialize<Headers>(fs, headers);
                }
            }

            Logging.Info("-WriteMasterList");

            // Move the temp file over the library filename
            Directory.CreateDirectory(Path.GetDirectoryName(GetFilename_MasterList()));
            FileTools.MoveSafelyWithOverwriting(filename_temp, GetFilename_MasterList());

            // Write the version of the index
            File.WriteAllText(VersionFilename, INDEX_VERSION);
        }

        /// <summary>
        /// Purges words from memory if there are too many of them for if they have been untouched/idle for too long.
        /// </summary>
        private void PurgeAllWords_LOCK()
        {
            Logging.Info("+PurgeAllWords_LOCK");

            var ordered_results =
                from wiwi in word_in_word_indexes
                where wiwi.IsLoaded
                group wiwi by GangStart(wiwi.WordId) into g
                let last_accessed = g.Max(wiwi => wiwi.last_accessed)
                orderby last_accessed ascending
                select new { Gang = g.Min(wiwi => wiwi.WordId), LastAccessed = last_accessed };

            int total_gangs = ordered_results.Count();
            Logging.Info("We have {0} loaded gangs", total_gangs);


            int total_purged = 0;
            foreach (var result in ordered_results)
            {
                bool bad_gang = false;

                int MAXIMUM_GANGS_IN_MEMORY = 10;
                int MAXIMUM_GANGS_IDLE_TIME_IN_MEMORY_IN_SECONDS = 30;

                // Is it a bad gang?
                if (total_gangs - total_purged > MAXIMUM_GANGS_IN_MEMORY)
                {
                    bad_gang = true;
                }

                if (DateTime.Now.Subtract(result.LastAccessed).TotalSeconds > MAXIMUM_GANGS_IDLE_TIME_IN_MEMORY_IN_SECONDS)
                {
                    bad_gang = true;
                }

                if (bad_gang)
                {
                    for (int i = 0; i < GANG_SIZE; ++i)
                    {
                        if (word_in_word_indexes.Count <= result.Gang + i)
                        {
                            break;
                        }

                        word_in_word_indexes[result.Gang + i].Purge();
                    }

                    ++total_purged;
                }
            }

            Logging.Info("Purged {0} out of {1} gangs", total_purged, total_gangs);

            Logging.Info("-PurgeAllWords_LOCK");
        }

        private void FlushAllWords()
        {
            lock (locker)
            {
                FlushAllWords_LOCK();
            }
        }

        private void FlushAllWords_LOCK()
        {
            Logging.Info("Starting to flush out word document indices");

            int total_flushed = 0;

            foreach (WordInWordIndex wiwi in word_in_word_index_lookups.Values)
            {
                if (wiwi.needs_flushing)
                {
                    FlushKeyword_LOCK(wiwi);
                    ++total_flushed;
                }
            }

            Logging.Info("Flushed out {0} word document indices", total_flushed);
        }

        /// <summary>
        /// Flushes the record for the keyword (and its entire gang) out to disk.  You must call this from inside a lock.
        /// </summary>
        /// <param name="wiwi"></param>
        /// <param name="create_directory_first"></param>
        private void FlushKeyword_LOCK(WordInWordIndex wiwi)
        {
            FlushKeyword_LOCK(wiwi, false);
        }

        /// <summary>
        /// Flushes the record for the keyword (and its entire gang) out to disk.  You must call this from inside a lock.
        /// </summary>
        /// <param name="wiwi"></param>
        /// <param name="create_directory_first"></param>
        private void FlushKeyword_LOCK(WordInWordIndex wiwi, bool create_directory_first)
        {
            // If this is not loaded, there is nothing to do
            if (!wiwi.IsLoaded)
            {
                return;
            }

            // If this one doesnt need flushing, don't do it
            if (!wiwi.needs_flushing)
            {
                return;
            }

            try
            {
                // Build up the gangs
                int gang_start = GangStart(wiwi.WordId);
                List<WordEntry> word_entrys = new List<WordEntry>();

                string filename_temp = Path.GetTempFileName();
                using (FileStream fs = File.Open(filename_temp, FileMode.Create))
                {
                    {
                        for (int i = 0; i < GANG_SIZE; ++i)
                        {
                            // If this is the last gang, there may be too few words
                            if (word_in_word_indexes.Count <= gang_start + i)
                            {
                                break;
                            }

                            if (null == word_in_word_indexes[gang_start + i].DocumentIds)
                            {
                                throw new Exception("Document ids should not be null");
                            }

                            WordEntry word_entry = new WordEntry
                            {
                                Word = word_in_word_indexes[gang_start + i].Word,
                                DocumentIds = word_in_word_indexes[gang_start + i].DocumentIds,
                                DocumentIdsCount = word_in_word_indexes[gang_start + i].DocumentIdsCount
                            };

                            word_entrys.Add(word_entry);
                        }

                        Serializer.Serialize<List<WordEntry>>(fs, word_entrys);
                    }
                }

                // Move the temp file into place
                string filename = Filename_GangList(wiwi.WordId);

                // Create the directory for the file
                if (create_directory_first)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                }

                FileTools.MoveSafelyWithOverwriting(filename_temp, filename);

                // Mark the gang as flushed
                for (int i = 0; i < GANG_SIZE; ++i)
                {
                    // If this is the last gang, there may be too few words
                    if (word_in_word_indexes.Count <= gang_start + i)
                    {
                        break;
                    }

                    word_in_word_indexes[gang_start + i].last_flushed = DateTime.UtcNow;
                    word_in_word_indexes[gang_start + i].needs_flushing = false;
                }
            }
            catch (Exception ex)
            {
                //  If we have an exception, it is probably because we have not created the directory, so try that
                if (!create_directory_first)
                {
                    FlushKeyword_LOCK(wiwi, true);
                }
                else
                {
                    // If we have created the directory before, then there must be some other problem
                    throw;
                }
            }
        }

        private void LoadWord_LOCK(WordInWordIndex wiwi)
        {
            // If the word is already loaded, nothing to do...
            if (wiwi.IsLoaded)
            {
                return;
            }

            try
            {
                string filename = Filename_GangList(wiwi.WordId);
                using (FileStream fs = File.OpenRead(filename))
                {
                    {
                        bool gang_has_corrupted_word_counts = false;

                        List<WordEntry> word_entrys = Serializer.Deserialize<List<WordEntry>>(fs);

                        int gang_start = GangStart(wiwi.WordId);
                        for (int i = 0; i < word_entrys.Count; ++i)
                        {
                            if (0 != String.Compare(word_in_word_indexes[gang_start + i].Word, word_entrys[i].Word))
                            {
                                throw new Exception("The ordering of the word index is corrupt: words don't match");
                            }

                            if (null != word_in_word_indexes[gang_start + i].DocumentIds)
                            {
                                Logging.Warn("The ordering of the word index is corrupt: document_ids should be null");
                            }

                            WordInWordIndex wiwi_just_loaded = word_in_word_indexes[gang_start + i];
                            bool corruption_detected = wiwi_just_loaded.SetDocumentIds(word_entrys[i].DocumentIds, word_entrys[i].DocumentIdsCount);
                            if (corruption_detected)
                            {
                                gang_has_corrupted_word_counts = true;
                            }
                        }

                        if (gang_has_corrupted_word_counts)
                        {
                            Logging.Warn("The ordering of a word index in the gang is corrupt: doc counts don't match (the user probably exited before the gang was saved...)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error(ex, "There was a problem loading the word document list for word {0}:{1}.  Assuming it was empty.", wiwi.WordId, wiwi.Word);
                bool set_result = wiwi.SetDocumentIds(new List<int>(), new List<int>());
            }
        }

        private string GetFilename_MasterList()
        {
            return Path.GetFullPath(Path.Combine(LIBRARY_INDEX_BASE_PATH, @"MasterList.dat"));
        }

        private int GangStart(int word_id)
        {
            return (word_id / GANG_SIZE) * GANG_SIZE;
        }

        private string Filename_GangList(int word_id)
        {
            string filename = String.Format(
                @"{2}/{1}/{0}.dat",
                word_id / GANG_SIZE,
                word_id / GANG_SIZE / 100,
                word_id / GANG_SIZE / 1000
             );

            return Path.GetFullPath(Path.Combine(LIBRARY_INDEX_BASE_PATH, filename));
        }

#endregion

#endif

#region --- Serialisation records -----------------------------------------------------------------

        [ProtoContract]
        public class Headers
        {
            [ProtoMember(1)]
            public List<DocumentMapHeader> documents;

            [ProtoMember(2)]
            public List<WordMapHeader> words;
        }

        [ProtoContract]
        public class DocumentMapHeader
        {
            [ProtoMember(1)]
            public string Fingerprint { get; set; }
            [ProtoMember(2)]
            public int DocumentId { get; set; }
        }

        [ProtoContract]
        public class WordMapHeader
        {
            [ProtoMember(1)]
            public string Word { get; set; }
            [ProtoMember(2)]
            public int WordId { get; set; }
            [ProtoMember(3)]
            public int DocCount { get; set; }
        }

        [ProtoContract]
        public class WordEntry
        {
            [ProtoMember(1)]
            public string Word { get; set; }
            [ProtoMember(2)]
            public List<int> DocumentIds { get; set; }
            [ProtoMember(3)]
            public List<int> DocumentIdsCount { get; set; }

            public override string ToString()
            {
                return String.Format("{0} {1}", Word, DocumentIds.Count);
            }
        }

#endregion
    }
}

#endif
