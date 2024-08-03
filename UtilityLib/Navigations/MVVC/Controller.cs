﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UtilityLib.Navigations.MVVC
{
    public interface IController
    {
        string ID { get; }
        MVVMCViewModel GetCurrentViewModel();
        void Navigate(string action, object parameter);
        void NavigateToInitial();
    }

    public enum HistoryMode
    {
        SaveParameterInstance,
        DiscardParameterInstance,
        SaveViewModel,
    }

    public enum NavigationMode { Regular, HistoryBack, HistoryForward };

    public abstract class Controller : IController
    {

        protected class HistoryItem
        {
            public string PageName { get; set; }
            public object NavigationParameter { get; set; }
            public MVVMCViewModel ViewModel { get; set; }
            public Dictionary<string, object> ViewBag { get; set; }
            public HistoryMode HistoryModeAtTheTime { get; set; }
        }

        private Type _thisType;
        private MethodInfo[] _methods;

        private MVVMCNavigationService _navigationService { get; set; }
        public INavigationService NavigationService
        {
            get { return _navigationService; }
        }

        public INavigationExecutor NavigationExecutor { get; set; }

        public string ID { get; internal set; }

        public HistoryMode HistoryMode { get; set; } = HistoryMode.DiscardParameterInstance;

        protected List<HistoryItem> History { get; private set; } = new List<HistoryItem>();
        protected int HistoryCurrentItemIndex { get; set; } = -1;

        public Controller()
        {
            _thisType = this.GetType();
            _methods = _thisType.GetMethods();
        }

        internal void SetNavigationService(MVVMCNavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public void NavigateToInitial()
        {
            Navigate("Initial", null);
        }

        public void Navigate(string action, object parameter)
        {
            var method = _methods.FirstOrDefault(m => m.Name == action);
            if (method == null)
            {
                throw new Exception($"Navigate failed. Can't find method '{action}' in controller '{ID}'");
            }
            else
            {
                var prms = method.GetParameters();
                if (prms.Length == 0)
                    method.Invoke(this, new object[] { });
                else
                    method.Invoke(this, new object[] { parameter });
            }
        }

        public virtual void GoBack()
        {
            CheckGoBackPossible();
            var historyItem = History[HistoryCurrentItemIndex - 1];
            VerifyHistoryModeAtTheTimeWasSaveParameterOrSaveViewModel(historyItem.HistoryModeAtTheTime);

            ExecuteNavigationInternal(historyItem.PageName,
                historyItem.NavigationParameter,
                historyItem.ViewModel,
                NavigationMode.HistoryBack,
                historyItem.ViewBag == null ? null : new Dictionary<string, object>(historyItem.ViewBag));
        }

        public virtual void GoBack(object parameter, Dictionary<string, object> viewBag)
        {
            CheckGoBackPossible();
            var historyItem = History[HistoryCurrentItemIndex - 1];
            ExecuteNavigationInternal(historyItem.PageName, parameter, null, NavigationMode.HistoryBack, viewBag);
        }

        private void CheckGoBackPossible()
        {
            if (HistoryCurrentItemIndex <= 0)
            {
                throw new InvalidOperationException("Trying to GoBack with Controller, when it's the first navigation history item");
            }
        }

        public virtual void GoForward()
        {
            CheckGoForwardPossible();
            var historyItem = History[HistoryCurrentItemIndex + 1];
            VerifyHistoryModeAtTheTimeWasSaveParameterOrSaveViewModel(historyItem.HistoryModeAtTheTime);
            ExecuteNavigationInternal(historyItem.PageName,
                historyItem.NavigationParameter,
                historyItem.ViewModel,
                NavigationMode.HistoryForward,
                historyItem.ViewBag == null ? null : new Dictionary<string, object>(historyItem.ViewBag));
        }

        public virtual void GoForward(object parameter, Dictionary<string, object> viewBag)
        {
            CheckGoForwardPossible();
            var historyItem = History[HistoryCurrentItemIndex + 1];
            ExecuteNavigationInternal(historyItem.PageName, parameter, null, NavigationMode.HistoryForward, viewBag);
        }

        private void VerifyHistoryModeAtTheTimeWasSaveParameterOrSaveViewModel(HistoryMode historyItemHistoryModeAtTheTime)
        {
            if (historyItemHistoryModeAtTheTime != HistoryMode.SaveParameterInstance && historyItemHistoryModeAtTheTime != HistoryMode.SaveViewModel)
            {
                throw new InvalidOperationException("Can't GoBack / GoForward without parameter because at the navigation time, " +
                                                    "the history mode was 'DiscardParameterInstance'. You can use the other overload and specify parameter and viewbag, even if null");
            }
        }

        private void CheckGoForwardPossible()
        {
            if (HistoryCurrentItemIndex >= History.Count - 1)
            {
                throw new InvalidOperationException("Trying to GoForward with Controller, when it's the last navigation history item");
            }
        }

        public virtual bool CanGoBack
        {
            get { return HistoryCurrentItemIndex > 0; }
        }

        public virtual bool CanGoForward
        {
            get { return HistoryCurrentItemIndex < History.Count - 1; }
        }

        protected void ClearHistory()
        {
            History.Clear();
            HistoryCurrentItemIndex = -1;
            _navigationService.RunOnUIThread(() =>
            {
                _navigationService.ChangeCanGoBack(ID);
                _navigationService.ChangeCanGoForward(ID);
            });
        }

        protected void ExecuteNavigation([CallerMemberName] string pageName = null)
        {
            ExecuteNavigationInternal(pageName, null, null, NavigationMode.Regular, null);
        }

        protected void ExecuteNavigation(object parameter, Dictionary<string, object> viewBag, [CallerMemberName] string pageName = null)
        {
            ExecuteNavigationInternal(pageName, parameter, null, NavigationMode.Regular, viewBag);
        }

        private void ExecuteNavigationInternal(string pageName, object parameter, MVVMCViewModel viewModel, NavigationMode navigationMode, Dictionary<string, object> viewBag = null)
        {
            bool shouldCancel = CallOnLeavingNavigation(true);
            if (shouldCancel)
                return;
            var newViewModel = NavigationExecutor.ExecuteNavigation(ID, pageName, parameter, viewModel, navigationMode, viewBag);
            ModifyHistory(pageName, parameter, newViewModel, navigationMode, viewBag);
            _navigationService.RunOnUIThread(() =>
            {
                _navigationService.ChangeCanGoBack(ID);
                _navigationService.ChangeCanGoForward(ID);
            });
            var currentViewModel = GetCurrentViewModel();
            if (currentViewModel != null)
            {
                _navigationService.RunOnUIThreadAsync(() => currentViewModel.OnLoad());
            }
        }

        /// <summary>
        /// Return value indicates whether navigation should be cancelled
        /// </summary>
        internal bool CallOnLeavingNavigation(bool allowCancelNavigation)
        {
            var vm = GetCurrentViewModel();
            if (vm != null)
            {
                var ev = new LeavingPageEventArgs()
                {
                    CancellingNavigationAllowed = allowCancelNavigation
                };
                vm.OnLeave(ev);
                return ev.CancelNavigation;
            }
            return false;
        }



        private void ModifyHistory(string pageName, object parameter, MVVMCViewModel viewModel, NavigationMode navigationMode, Dictionary<string, object> viewBag)
        {
            switch (navigationMode)
            {
                case NavigationMode.Regular:
                    if (History.Count > HistoryCurrentItemIndex + 1) //Means current item is not last. We need to remove from history all items in front
                    {
                        History.RemoveRange(HistoryCurrentItemIndex + 1, History.Count - HistoryCurrentItemIndex - 1);
                    }

                    var newHistoryItem = new HistoryItem()
                    {
                        PageName = pageName,
                        NavigationParameter = HistoryMode == HistoryMode.SaveParameterInstance ? parameter : null,
                        ViewBag = HistoryMode == HistoryMode.SaveParameterInstance ? new Dictionary<string, object>(viewBag) : null,
                        ViewModel = HistoryMode == HistoryMode.SaveViewModel ? viewModel : null,
                        HistoryModeAtTheTime = this.HistoryMode
                    };
                    History.Add(newHistoryItem);
                    HistoryCurrentItemIndex++;

                    break;
                case NavigationMode.HistoryBack:

                    HistoryCurrentItemIndex--;
                    break;

                case NavigationMode.HistoryForward:
                    HistoryCurrentItemIndex++;
                    break;
            }

        }

        public MVVMCViewModel GetCurrentViewModel()
        {
            return _navigationService.GetCurrentViewModelByControllerID(ID);
        }

        public string GetCurrentPageName()
        {
            return _navigationService.GetCurrentPageNameByControllerID(ID);
        }

        public abstract void Initial();

    }
}
}
