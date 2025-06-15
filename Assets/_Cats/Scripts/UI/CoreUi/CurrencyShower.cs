using System;
using _cats.Core;
using _cats.Scripts.Core;
using TMPro;
using UnityEngine;

namespace _Cats.UI.CoreUi
{
    public class CurrencyShower : CATSMonoBehaviour
    {
        public TextMeshProUGUI currencyText;
        public CurrencyType currencyType;

        [ContextMenu("try to add Currency")]
        public void Test()
        {
            _manager.currencyManager.TryAddCurrency(currencyType, 5000);
        }
        public void Start()
        {
            AddListener(CATSEventNames.OnCurrencyChanged,(OnGameStart));
            InitSlot();
        }

        private void InitSlot()
        {
            var t = _manager.currencyManager.GetCurrency(currencyType);
            currencyText.text = t != null ? t.Amount.ToString() : "0";
        }

        private void OnGameStart(object obj)
        {
            var currencySlot = (CurrencySlot) obj;
            if (currencySlot.Currency == currencyType)
            {
                currencyText.text = currencySlot.Amount.ToString();
            }
        }
    }
}