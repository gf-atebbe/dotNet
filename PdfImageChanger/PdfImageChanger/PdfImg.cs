using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PdfSharp.Pdf;

namespace PdfImageChanger
{
    public class PdfImg
    {
        public int imageCount { get; set; }
        public bool replace { get; set; }
        public string filename { get; set; }
        public byte[] sourceImg { get; set; }
        public List<string> links { get; set; }
        public string link { get; set; }

    }
}
