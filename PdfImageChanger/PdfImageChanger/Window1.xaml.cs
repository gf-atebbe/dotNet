using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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

        #region Properties

        private Config _configObj;
        public Config configObj
        {
            get { return _configObj; }
            set { _configObj = value; }
        }

        #endregion

        #region Constructors

        public Window1()
        {
            InitializeComponent();
        }

        public Window1(Config appConfig)
        {
            InitializeComponent();
            configObj = appConfig;
            dataGrid1.ItemsSource = _configObj.imageLinks;
            dataGrid2.ItemsSource = _configObj.distributors;

            ArrayList knownColors = new ArrayList();
            foreach (KnownColor col in Enum.GetValues(typeof(KnownColor)))
            {
                System.Drawing.Color color = System.Drawing.Color.FromKnownColor(col);
                if (!color.IsSystemColor)
                {
                    knownColors.Add(col.ToString());
                }
            }

            ColorPicker.ItemsSource = knownColors;
            ColorPicker.SelectedIndex = knownColors.IndexOf(appConfig.contactColor);
        }

        #endregion

        #region Button Events

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.configObj.contactColor = ColorPicker.SelectedValue.ToString();
            this.Close();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        #endregion

    }
}
