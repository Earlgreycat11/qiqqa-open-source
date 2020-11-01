﻿using System;
using System.Collections.Generic;
using Qiqqa.DocumentLibrary.WebLibraryStuff;
using Qiqqa.Documents.PDF;
using Utilities;
using Utilities.Collections;
using Utilities.Language;

namespace Qiqqa.DocumentLibrary.SimilarAuthorsStuff
{
    public static class SimilarAuthors
    {
        private static readonly List<NameTools.Name> EMPTY_NAMES = new List<NameTools.Name>();

        public static List<NameTools.Name> GetAuthorsForPDFDocument(PDFDocument pdf_document)
        {
            string authors = pdf_document.AuthorsCombined;
            if (String.IsNullOrEmpty(authors) || Constants.UNKNOWN_AUTHORS == authors)
            {
                return EMPTY_NAMES;
            }

            List<NameTools.Name> names = NameTools.SplitAuthors(authors);
            return names;
        }

        public static MultiMap<string, PDFDocument> GetDocumentsBySameAuthors(WebLibraryDetail web_library_detail, PDFDocument pdf_document_original, List<NameTools.Name> authors)
        {
            // Build a quick lookup of the authors
            HashSet<string> last_names = new HashSet<string>();
            foreach (var author in authors)
            {
                last_names.Add(author.last_name);
            }

            return GetDocumentsBySameAuthorsSurname(web_library_detail, pdf_document_original, last_names);
        }

        public static MultiMap<string, PDFDocument> GetDocumentsBySameAuthorsSurname(WebLibraryDetail web_library_detail, string last_name)
        {
            return GetDocumentsBySameAuthorsSurname(web_library_detail, null, new HashSet<string>(new string[] { last_name }));
        }

        public static MultiMap<string, PDFDocument> GetDocumentsBySameAuthorsSurname(WebLibraryDetail web_library_detail, PDFDocument pdf_document_original, HashSet<string> last_names)
        {
            MultiMap<string, PDFDocument> authors_documents = new MultiMap<string, PDFDocument>(true);
            foreach (PDFDocument pdf_document in web_library_detail.Xlibrary.PDFDocuments)
            {
                // Don't include the original document
                if (pdf_document == pdf_document_original)
                {
                    continue;
                }

                List<NameTools.Name> authors_search = GetAuthorsForPDFDocument(pdf_document);
                foreach (var author_search in authors_search)
                {
                    if (last_names.Contains(author_search.last_name))
                    {
                        authors_documents.Add(author_search.last_name, pdf_document);
                    }
                }
            }

            return authors_documents;
        }

        internal static List<PDFDocument> GetDocumentsBySameAuthorsSurnameAndInitial(WebLibraryDetail web_library_detail, string surname, string initial)
        {
            List<PDFDocument> pdf_documents = new List<PDFDocument>();
            foreach (PDFDocument pdf_document in web_library_detail.Xlibrary.PDFDocuments)
            {
                List<NameTools.Name> authors_search = GetAuthorsForPDFDocument(pdf_document);
                foreach (var author_search in authors_search)
                {
                    bool surnames_match = surname == author_search.last_name;
                    bool initials_match = String.IsNullOrEmpty(initial) || String.IsNullOrEmpty(author_search.first_names) || initial[0] == author_search.first_names[0];
                    if (surnames_match && initials_match)
                    {
                        pdf_documents.Add(pdf_document);
                        break;
                    }
                }
            }

            return pdf_documents;
        }

        #region --- Test ------------------------------------------------------------------------

#if TEST
        public static void Test()
        {
            Library library = Library.GuestInstance;
            List<PDFDocument> pdf_documents = library.PDFDocuments;

            for (int i = 0; i < pdf_documents.Count; ++i)
            {
                PDFDocument pdf_document = pdf_documents[i];
                List<NameTools.Name> authors = GetAuthorsForPDFDocument(pdf_document);
                MultiMap<string, PDFDocument> similar_documents = GetDocumentsBySameAuthors(library, pdf_document, authors);

                if (similar_documents.Keys.Count > 0)
                {

                }
            }
        }
#endif

        #endregion
    }
}
