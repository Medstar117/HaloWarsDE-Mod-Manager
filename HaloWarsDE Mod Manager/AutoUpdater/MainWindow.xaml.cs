using AutoUpdater.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutoUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Label    { get => TextLabel.Text;    set => TextLabel.Text = value; }
        public string SubLabel { get => SubTextLabel.Text; set => SubTextLabel.Text = value; }
        
        public MainWindow()
        {
            InitializeComponent();

            Title = $"{Constants.AppName} Auto Updater";
            Label = "Downloading latest release...";

            SubTextLabel.Visibility= Visibility.Visible;
            
            // Check if pBar needs to be indeterminate (constant scrolling)
            //ProgressBar.IsIndeterminate = true;
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
