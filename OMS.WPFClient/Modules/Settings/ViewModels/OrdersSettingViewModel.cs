﻿using Microsoft.Practices.Unity;
using Prism.Regions;
using Prism.Unity;
using System;
using System.Collections.Generic;
using OMS.WPFClient.Infrastructure.SettingsRepository;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using ReactiveUI;

namespace OMS.WPFClient.Modules.Settings.ViewModels
{
    public class OrdersSettingViewModel : ReactiveObject
    {
        #region Declaration
        IUserSettingsRepository userSettingsRepository;
        #endregion

        #region Constructor
        public OrdersSettingViewModel(IRegionManager regionManager, IUserSettingsRepository userSettingsRepository)
        {
            this.userSettingsRepository = userSettingsRepository;

            ChangeDefaultViewCommand = new DelegateCommand<string>(ChangeDefaultViewExecute);

            SetDefaultView();
        }

        #endregion

        #region Commands

        #region ChangeDefaultView
        public DelegateCommand<string> ChangeDefaultViewCommand { get; }

        private void ChangeDefaultViewExecute(string newDefaultView)
        {
            userSettingsRepository.WriteSetting("OrdersMainView", newDefaultView);
        }
        #endregion

        #endregion

        #region Properties

        public bool IsCreateViewDefaultView { set; get; }

        public bool IsJournalViewDefaultView { set; get; }

        #endregion

        #region Utilities 
        /// <summary>
        /// Setting default view when creating instance of current ViewModel.
        /// </summary>
        private void SetDefaultView()
        {
            string defaultView = (string)userSettingsRepository.ReadSetting("OrdersMainView");

            if (defaultView == "Create View") IsCreateViewDefaultView = true;
            else IsJournalViewDefaultView = true;
        }

        #endregion
    }
}
