﻿using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using icons;
using Qiqqa.Brainstorm.SceneManager;
using Qiqqa.DocumentLibrary.WebLibraryStuff;
using Qiqqa.Documents.PDF;
using Qiqqa.UtilisationTracking;

namespace Qiqqa.Brainstorm.Nodes
{
    /// <summary>
    /// Interaction logic for DocumentNodeContentControl.xaml
    /// </summary>
    public partial class PDFAutoTagNodeContentControl : UserControl, IKeyPressableNodeContentControl
    {
        private NodeControl node_control;
        private PDFAutoTagNodeContent pdf_auto_tag_node_content;

        public PDFAutoTagNodeContentControl(NodeControl node_control, PDFAutoTagNodeContent pdf_auto_tag_node_content)
        {
            this.node_control = node_control;
            this.pdf_auto_tag_node_content = pdf_auto_tag_node_content;
            DataContext = pdf_auto_tag_node_content;

            InitializeComponent();

            Focusable = true;

            ImageIcon.Source = Icons.GetAppIcon(Icons.BrainstormPDFAutoTag);

            ImageIcon.Width = NodeThemes.image_width;
            TextBorder.CornerRadius = NodeThemes.corner_radius;
            TextBorder.Background = NodeThemes.background_brush;
        }

        public void ProcessKeyPress(KeyEventArgs e)
        {
            if (Key.D == e.Key)
            {
                ExpandDocuments();
                e.Handled = true;
            }
        }

        public void ExpandDocuments()
        {
            FeatureTrackingManager.Instance.UseFeature(Features.Brainstorm_ExploreLibrary_AutoTag_Documents);

            WebLibraryDetail web_library_detail = WebLibraryManager.Instance.GetLibrary(pdf_auto_tag_node_content.LibraryId);

            HashSet<string> document_fingerprints = web_library_detail.Xlibrary.AITagManager.AITags.GetDocumentsWithTag(pdf_auto_tag_node_content.Tag);
            List<PDFDocument> pdf_documents = web_library_detail.Xlibrary.GetDocumentByFingerprints(document_fingerprints);

            foreach (PDFDocument pdf_document in pdf_documents)
            {
                PDFDocumentNodeContent content = new PDFDocumentNodeContent(pdf_document.Fingerprint, pdf_document.LibraryRef.Id);
                NodeControlAddingByKeyboard.AddChildToNodeControl(node_control, content, false);
            }
        }
    }
}
