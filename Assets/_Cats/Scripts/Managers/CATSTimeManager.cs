using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _cats.Scripts.Core
{
    public class CATSTimeManager
    {
        private bool isLooping = false;

        private Dictionary<int, List<CATSTimerData>> timerActions = new();
        private List<CATSAlarmData> activeAlarms = new();

        private CATSOfflineTime _catsOfflineTime;

        private int counter;
        private int alarmCounter;
        private int offlineSeconds;

        public CATSTimeManager()
        {
            TimerLoop();


            CATSManager.Instance.EventsManager.AddListener(CATSEventNames.OnPause, OnPause);
        }

        private void OnPause(object pauseStatus)
        {
            if (!(bool) pauseStatus)
            {
                CheckOfflineTime();
            }
        }

        ~CATSTimeManager()
        {
            isLooping = false;
            CATSManager.Instance.EventsManager.RemoveListener(CATSEventNames.OnPause, OnPause);
        }

        private void CheckOfflineTime()
        {
            var timePassed = DateTime.Now - _catsOfflineTime.LastCheck;
            offlineSeconds = (int) timePassed.TotalSeconds;
            _catsOfflineTime.LastCheck = DateTime.Now;


            CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OfflineTimeRefreshed, offlineSeconds);
        }

        public int GetLastOfflineTimeSeconds()
        {
            return offlineSeconds;
        }

        private async Task TimerLoop()
        {
            isLooping = true;

            while (isLooping)
            {
                await Task.Delay(1000);
                InvokeTime();
            }

            isLooping = false;
        }

        private void InvokeTime()
        {
            counter++;

            foreach (var timers in timerActions)
            {
                foreach (var timer in timers.Value)
                {
                    var offsetCounter = counter - timer.StartCounter;

                    if (offsetCounter % timers.Key == 0)
                    {
                        timer.TimerAction.Invoke();
                    }
                }
            }

            for (var index = 0; index < activeAlarms.Count; index++)
            {
                var alarmData = activeAlarms[index];

                if (DateTime.Compare(alarmData.AlarmTime, DateTime.Now) < 0)
                {
                    alarmData.AlarmAction.Invoke();
                    activeAlarms.Remove(alarmData);
                }
            }
        }

        public void SubscribeTimer(int intervalSeconds, Action onTickAction)
        {
            if (!timerActions.ContainsKey(intervalSeconds))
            {
                timerActions.Add(intervalSeconds, new List<CATSTimerData>());
            }

            timerActions[intervalSeconds].Add(new CATSTimerData(counter, onTickAction));
        }

        public void UnSubscribeTimer(int intervalSeconds, Action onTickAction)
        {
            timerActions[intervalSeconds].RemoveAll(x => x.TimerAction == onTickAction);
        }

        public int SetAlarm(int seconds, Action onAlarmAction)
        {
            alarmCounter++;

            var alarmData = new CATSAlarmData
            {
                ID = alarmCounter,
                AlarmTime = DateTime.Now.AddSeconds(seconds),
                AlarmAction = onAlarmAction
            };

            activeAlarms.Add(alarmData);
            return alarmCounter;
        }

        public void DisableAlarm(int alarmID)
        {
            activeAlarms.RemoveAll(x => x.ID == alarmID);
        }

        public int GetLeftOverTime(OfflineTimes timeType)
        {
            if (!_catsOfflineTime.LeftOverTimes.ContainsKey(timeType))
            {
                return 0;
            }

            return _catsOfflineTime.LeftOverTimes[timeType];
        }

        public void SetLeftOverTime(OfflineTimes timeType, int timeAmount)
        {
            _catsOfflineTime.LeftOverTimes[timeType] = timeAmount;
        }
    }

    public class CATSTimerData
    {
        public Action TimerAction;
        public int StartCounter;

        public CATSTimerData(int counter, Action onTickAction)
        {
            TimerAction = onTickAction;
            StartCounter = counter;
        }
    }

    public class CATSAlarmData
    {
        public int ID;
        public DateTime AlarmTime;
        public Action AlarmAction;
    }

    [Serializable]
    public class CATSOfflineTime : ICATSSaveData
    {
        public DateTime LastCheck;
        public Dictionary<OfflineTimes, int> LeftOverTimes = new();
    }

    public enum OfflineTimes
    {
        DailyBonus,
        ExtraBonus
    }


    public interface ICATSSaveData
    {
    }
}