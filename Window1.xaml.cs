using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PdfImageChanger
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private Config _configObj;
        public Config configObj
        {
            get { return _configObj; }
            set { _configObj = value; }
        }

        public Window1()
        {
            InitializeComponent();
        }

        public Window1(Config appConfig)
        {
            InitializeComponent();
            configObj = appConfig;

            if (configObj == null)
            {
                _configObj = new Config();
                _configObj.imageLinks = new List<ImageLink>();
                _configObj.distributors = new List<Distributor>();
            }
            
            if (_configObj.imageLinks == null)
            {
                _configObj.imageLinks = new List<ImageLink>();
            }
            
            if (_configObj.distributors == null)
            {
                _configObj.distributors = new List<Distributor>();
            }

            //ImageLink url = new ImageLink();
            //url.url = "http://www.facebook.com";
            //url.prettyName = "facebook";
            //_configObj.imageLinks.Add(url);

            dataGrid1.ItemsSource = _configObj.imageLinks;
        }
    }
}
