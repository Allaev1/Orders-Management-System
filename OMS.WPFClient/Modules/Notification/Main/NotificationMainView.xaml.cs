﻿using MaterialDesignThemes.Wpf;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OMS.WPFClient.Modules.Notification.Main
{
    /// <summary>
    /// Interaction logic for NotificationMainView.xaml
    /// </summary>
    public partial class NotificationMainView : UserControl
    {
        public NotificationMainView()
        {
            InitializeComponent();
        }

        private async void TextBlock_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            if (string.IsNullOrWhiteSpace(textBlock.Text)) return;

            await Task.Delay(10000);

            Application.Current.Shutdown();
        }
    }
}
