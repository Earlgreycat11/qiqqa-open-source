﻿using System.Collections.Generic;

namespace Qiqqa.WebBrowsing
{
    internal class WebSearchers
    {
        public const string SCHOLAR_KEY = "GOOGLE_SCHOLAR";
        public const string PUBMED_KEY = "PUBMED";
        public const string PUBMEDXML_KEY = "PUBMEDXML";
        public const string ARXIV_KEY = "ARXIV";
        public const string JSTOR_KEY = "JSTOR";
        public const string IEEEXPLORE_KEY = "IEEEXPLORE";
        public const string SCIVERSE_KEY = "SCIVERSE";
        public const string MSACADEMIC_KEY = "MSACADEMIC";
        public const string WIKIPEDIA_KEY = "WIKIPEDIA";
        public const string GOOGLE_US_KEY = "GOOGLE_US";
        public const string GOOGLE_UK_KEY = "GOOGLE_UK";
        public const string DOI2BIB_KEY = "DOI2BIB";

        public static readonly List<WebSearcher> WEB_SEARCHERS = new List<WebSearcher>
            (
                new WebSearcher[]
                {
                    new WebSearcher(GOOGLE_US_KEY, "Google.com", "http://www.google.com/#q={0}", "http://www.google.com/", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(GOOGLE_UK_KEY, "Google.co.uk", "http://www.google.co.uk/#q={0}", "http://www.google.co.uk/", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(SCHOLAR_KEY, "Google Scholar", "http://scholar.google.com/scholar?q={0}", "http://scholar.google.com/scholar", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(PUBMED_KEY, "PubMed", "http://www.ncbi.nlm.nih.gov/pubmed?term={0}", "http://www.ncbi.nlm.nih.gov/pubmed", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(PUBMEDXML_KEY, "PubMedXML", "http://www.ncbi.nlm.nih.gov/pubmed?report=xml&term={0}", "http://www.ncbi.nlm.nih.gov/pubmed", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(ARXIV_KEY, "arXiv", "http://search.arxiv.org:8081/?query={0}", "http://search.arxiv.org:8081", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(MSACADEMIC_KEY, "Microsoft Academic", "http://academic.research.microsoft.com/Search?query={0}", "http://academic.research.microsoft.com/Search", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(JSTOR_KEY, "JSTOR", "http://www.jstor.org/action/doBasicSearch?Query={0}", "http://www.jstor.org/action/doBasicSearch", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(IEEEXPLORE_KEY, "IEEE Xplore", "http://ieeexplore.ieee.org/search/searchresult.jsp?queryText={0}", "http://ieeexplore.ieee.org/search/searchresult.jsp", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(WIKIPEDIA_KEY, "Wikipedia", "http://en.wikipedia.org/wiki/{0}", "http://en.wikipedia.org/wiki/", WebSearcher.PopulateUrlTemplateDelegate_WikiEncode),
                    new WebSearcher(SCIVERSE_KEY, "ScienceDirect", "https://www.elsevier.com/search-results?query={0}&labels=all", "https://www.elsevier.com/search-results", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                    new WebSearcher(DOI2BIB_KEY, "DOI2BIB", "https://www.doi2bib.org/bib/{0}", "https://www.doi2bib.org/", WebSearcher.PopulateUrlTemplateDelegate_UrlEncode),
                }
            );

        internal static WebSearcher FindWebSearcher(string requested_web_searcher)
        {
            foreach (var web_searcher in WEB_SEARCHERS)
            {
                if (0 == web_searcher.key.CompareTo(requested_web_searcher))
                {
                    return web_searcher;
                }
            }

            return null;
        }
    }
}