# Testing :: Nasty URLs for PDFs

- http://dspace.bracu.ac.bd/xmlui/bitstream/handle/10361/7620/12201001%20&%2013101230_CSE.pdf?sequence=1

  This link downloads/views a PDF fine in MS Edge / Chrome, but produces a HTML/404 crash page when using `curl` as in `curl -v -L http://dspace.bracu.ac.bd/xmlui/bitstream/handle/10361/7620/12201001%20&%2013101230_CSE.pdf?sequence=1 -o test.pdf`

- https://www.aaai.org/Papers/Workshops/2006/WS-06-06/WS06-06-006.pdf

  Downloads OK in the browser, but `curl -v -L` outputs this:

  ```
  < HTTP/1.1 406 Not Acceptable
  < Date: Thu, 01 Oct 2020 21:00:39 GMT
  < Server: Apache
  < Content-Length: 373
  < Content-Type: text/html; charset=iso-8859-1
  ```

  + and another one: https://www.aaai.org/Papers/Workshops/2006/WS-06-06/WS06-06-003.pdf
  + https://www.aaai.org/Papers/Workshops/2006/WS-06-09/WS06-09-006.pdf
  
- https://aaaipress.org/Papers/Workshops/1998/WS-98-07/WS98-07-015.pdf

  Connection Not Secure (not tested with `curl` though; this is a browser report)

- http://en.saif.sjtu.edu.cn/junpan/Peso_RFS.pdf

  Weird DNS shit happening. Bing and Google know this one, browser cannot reach.
  
- https://onlinelibrary.wiley.com/doi/pdf/10.1002/humu.22848

  Renders as PDF in browser, but 'Save As' produces the HTML. Only clicking on the content and then hitting right-click meenu -> Save As will prduce the PDF.
  
  https://onlinelibrary.wiley.com/doi/pdf/10.1002/acp.2995 : ditto
  
  https://onlinelibrary.wiley.com/doi/pdf/10.1002/ejsp.2331 : ditto. Hm, must be something smelly in that HTML...
  
- http://www.insightsociety.org/ojaseit/index.php/ijaseit/article/view/6566

  The small link to the PDF is not the PDF itself but delivers a page, which embeds a PDF in its HTML surroundings.
  
  
  
