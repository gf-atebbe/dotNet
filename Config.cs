using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfImageChanger
{
    public class Config
    {
        public List<ImageLink> imageLinks { get; set; }
        public List<Distributor> distributors { get; set; }
    }

    public class Distributor
    {
        public String logo;
        public String logoUrl;
        public String contactName;
        public String contactStreet;
    }
}
