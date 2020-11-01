﻿using System.Data.SQLite;
using Utilities;
using Utilities.Files;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;
using LibraryDB = Qiqqa.DocumentLibrary.LibraryDB;


namespace Qiqqa.UpgradePaths.V037To038
{
    internal class SQLiteUpgrade_LibraryDB
    {
        private string base_path;
        private string library_path;

        public SQLiteUpgrade_LibraryDB(string base_path)
        {
            this.base_path = base_path;
            library_path = LibraryDB.GetLibraryDBPath(base_path);

            // Copy a library into place...
            if (!File.Exists(library_path))
            {
                Logging.Warn("Library db does not exist so copying the template to {0}", library_path);
                string library_template_path = LibraryDB.GetLibraryDBTemplatePath();
                File.Copy(library_template_path, library_path);
            }
        }

        public SQLiteConnection GetConnection()
        {
            SQLiteConnection connection = new SQLiteConnection("Pooling=True;Max Pool Size=3;Data Source=" + library_path);

            // Turn on extended result codes
            connection.SetExtendedResultCodes(true);

            return connection;
        }

        internal void PutBlob(SQLiteConnection connection, SQLiteTransaction transaction, string fingerprint, string extension, byte[] data)
        {
            // Calculate the MD5 of this blobbiiiieeeeee
            string md5 = StreamMD5.FromBytes(data);
            using (var command = new SQLiteCommand("INSERT INTO LibraryItem(fingerprint, extension, md5, data) VALUES(@fingerprint, @extension, @md5, @data)", connection, transaction))
            {
                command.Parameters.AddWithValue("@fingerprint", fingerprint);
                command.Parameters.AddWithValue("@extension", extension);
                command.Parameters.AddWithValue("@md5", md5);
                command.Parameters.AddWithValue("@data", data);
                command.ExecuteNonQuery();
            }
        }
    }
}
