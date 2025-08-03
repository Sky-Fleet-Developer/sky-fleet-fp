using System;
using System.Collections;
using System.Globalization;
using Core.Character;
using Core.Character.Interaction;
using Core.Character.Interface;
using Core.Localization;
using Core.Patterns.State;
using Core.UiStructure;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Runtime.Trading.Ui
{
    public class TradeInterface : Service, IFirstPersonInterface
    {
        [SerializeField] private TradeItemsListView sellerItemsView;
        private ITradeHandler _handler;
        private FirstPersonController.TradeState _targetState;
        private FirstPersonInterfaceInstaller _master;

        public void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
            _targetState = ((FirstPersonController.TradeState)_master.TargetState);
            _handler = _targetState.Handler;
        }

        public bool IsMatch(IState state)
        {
            return state is FirstPersonController.TradeState;
        }

        public void Redraw()
        {
            
        }

        public void Show()
        {
            Window.Open();
            sellerItemsView.SetItems(_handler.GetItems());
        }

        public void Hide()
        {
            Window.Close();
        }

        public override IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            _targetState.LeaveState();
            return base.Hide(settings);
        }
    }
}