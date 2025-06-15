using System;
using System.Collections.Generic;
using System.Linq;
using _cats.Scripts.Core;
using UnityEngine;


namespace _cats.Core
{
    [Serializable]
    public class CATSCurrencyManager
    {
        public HashSet<CurrencySlot> currencySlots = new HashSet<CurrencySlot>();

        public CATSCurrencyManager()
        {
            Init((() =>
            {
               
            }));
        }

        private void Init(Action onInitComplete )
        {
            var enumNames = Enum.GetNames(typeof(CurrencyType)).ToList();
            foreach (var enumName in enumNames)
            {
                CurrencySlot currencySlot = new CurrencySlot();
                currencySlot.Currency = (Enum.Parse<CurrencyType>(enumName));
                currencySlot.Amount = 0;
                currencySlots.Add(currencySlot);
            }
            onInitComplete?.Invoke();
        }

        public CurrencySlot GetCurrency(CurrencyType currencyType) => currencySlots.FirstOrDefault(x => x.Currency == currencyType);

        public void TryAddCurrency(CurrencyType currencyType, int amount)
        {
            CurrencySlot currencySlot = GetCurrency(currencyType);
            currencySlot.Amount += amount;
            CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnCurrencyChanged, currencySlot);
        }

        public bool TryToBuy(CurrencyType currencyType, int amount , Action onBuyComplete)
        {
            CurrencySlot currencySlot = GetCurrency(currencyType);
            if (currencySlot.Amount < amount)
            {
                return false;
            }
            currencySlot.Amount -= amount;
            onBuyComplete?.Invoke();
            CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnCurrencyChanged, currencySlot);
            return true;
        }
    }

    public enum CurrencyType
    {
        Coins,Gems,Usd,Btc
    }

    [Serializable]
    public class CurrencySlot
    {
        public CurrencyType Currency;
        public int Amount;
    } 
    
    public static class EnumUtil {
        public static IEnumerable<T> GetValues<T>() {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}