﻿using System;
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
using OMS.WPFClient.Infrastructure.Events;
using OMS.WPFClient.Infrastructure.SettingsRepository;
using ReactiveUI;

namespace OMS.WPFClient.Modules.Settings.Views
{
    /// <summary>
    /// Interaction logic for GeneralSettingView.xaml
    /// </summary>
    public partial class GeneralSettingView : UserControl
    {
        public GeneralSettingView(IUserSettingsRepository userSettingsRepository)
        {
            InitializeComponent();

            string defualtPrimaryColor = (string)userSettingsRepository.ReadSetting("AppPrimaryColor");
            string defualtSecondaryColor = (string)userSettingsRepository.ReadSetting("AppSecondaryColor");

            foreach (Button button in PrimaryCollorCB.Items)
            {
                if ((string)button.Tag == defualtPrimaryColor)
                {
                    PrimaryCollorCB.SelectedItem = button;
                    break;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button currentButton = (Button)sender;

            PrimaryCollorCB.SelectedItem = currentButton;

            PrimaryCollorCB.IsDropDownOpen = false;
        }
    }
}
