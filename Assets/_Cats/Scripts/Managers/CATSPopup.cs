using System;
using UnityEngine;

namespace _cats.Scripts.Core
{
    public abstract class CATSPopup : MonoBehaviour
    {
        protected Action onCloseAction;
        
        public virtual void Show(object data, Action onClose)
        {
            onCloseAction = onClose;
            OnShow(data);
        }

        public virtual void Close()
        {
            OnClose();
            gameObject.SetActive(false);
            onCloseAction?.Invoke();
        }

        protected abstract void OnShow(object data);
        protected abstract void OnClose();
    }
}