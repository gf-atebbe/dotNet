using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfImageChanger
{
    public class ImageLink
    {
        public string prettyName { get; set; }
        public string url { get; set; }
        public double xStart { get; set; }
        public double yStart { get; set; }
        public double width { get; set; }
        public double height { get; set; }
    }
}
