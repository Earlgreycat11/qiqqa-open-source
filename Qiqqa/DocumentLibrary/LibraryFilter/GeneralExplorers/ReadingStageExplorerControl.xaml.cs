﻿using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using Qiqqa.DocumentLibrary.TagExplorerStuff;
using Qiqqa.DocumentLibrary.WebLibraryStuff;
using Qiqqa.Documents.PDF;
using Utilities;
using Utilities.Collections;
using Utilities.GUI;

namespace Qiqqa.DocumentLibrary.LibraryFilter.GeneralExplorers
{
    /// <summary>
    /// Interaction logic for TagExplorerControl.xaml
    /// </summary>
    public partial class ReadingStageExplorerControl : UserControl
    {
        private WebLibraryDetail web_library_detail;

        public delegate void OnTagSelectionChangedDelegate(HashSet<string> fingerprints, Span descriptive_span);
        public event OnTagSelectionChangedDelegate OnTagSelectionChanged;

        public ReadingStageExplorerControl()
        {
            InitializeComponent();

            ToolTip = "Here are the Reading Stages of your documents.  " + GenericLibraryExplorerControl.YOU_CAN_FILTER_TOOLTIP;

            TagExplorerTree.DescriptionTitle = "Reading Stage";

            TagExplorerTree.GetNodeItems = GetNodeItems;

            TagExplorerTree.OnTagSelectionChanged += TagExplorerTree_OnTagSelectionChanged;
        }

        // -----------------------------

        public WebLibraryDetail LibraryRef
        {
            get => web_library_detail;
            set
            {
                web_library_detail = value;
                TagExplorerTree.LibraryRef = value;
            }
        }

        public void Reset()
        {
            TagExplorerTree.Reset();
        }

        // -----------------------------

        internal static MultiMapSet<string, string> GetNodeItems(WebLibraryDetail web_library_detail, HashSet<string> parent_fingerprints)
        {
            WPFDoEvents.AssertThisCodeIs_NOT_RunningInTheUIThread();

            List<PDFDocument> pdf_documents = null;
            if (null == parent_fingerprints)
            {
                pdf_documents = web_library_detail.Xlibrary.PDFDocuments;
            }
            else
            {
                pdf_documents = web_library_detail.Xlibrary.GetDocumentByFingerprints(parent_fingerprints);
            }
            Logging.Debug特("ReadingStageExplorerControl: processing {0} documents from library {1}", pdf_documents.Count, web_library_detail.Title);

            MultiMapSet<string, string> tags_with_fingerprints = new MultiMapSet<string, string>();
            foreach (PDFDocument pdf_document in pdf_documents)
            {
                tags_with_fingerprints.Add(pdf_document.ReadingStage ?? "(none)", pdf_document.Fingerprint);
            }

            return tags_with_fingerprints;
        }

        private void TagExplorerTree_OnTagSelectionChanged(HashSet<string> fingerprints, Span descriptive_span)
        {
            WPFDoEvents.SafeExec(() =>
            {
                OnTagSelectionChanged?.Invoke(fingerprints, descriptive_span);
            });
        }

        #region --- Test ------------------------------------------------------------------------

#if TEST
        public static void TestHarness()
        {
            Library library = Library.GuestInstance;
            TagExplorerControl tec = new TagExplorerControl();
            tec.LibraryRef = library;

            ControlHostingWindow w = new ControlHostingWindow("Tags", tec);
            w.Show();
        }
#endif

        #endregion
    }
}
