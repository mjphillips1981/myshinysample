using System.Diagnostics;

namespace Helpers
{
	public class TimerHelper
	{
		Timer _timer;
		public Action Callback { get; set; }
		public int Invertval { get; set; }
		string _nameOfCallback;
		bool _enabled;

		public TimerHelper()
		{
		}

		public void Start(Action action, string name, int waitTime, int interval)
		{
			if (_enabled)
			{
				return;
			}

			Debug.WriteLine("timer started");

			if (action != null)
			{
				Callback = action;
				_nameOfCallback = name;
			}

			_timer = new Timer(OnTimerElapsed, null, waitTime, interval);
			_enabled = true;
		}

		public void Stop(bool clearCallback)
		{
			if (!_enabled)
			{
				return;
			}

			Debug.WriteLine("timer stopped");

			_timer.Dispose();
			_timer = null;
			_enabled = false;

			if (clearCallback)
			{
				Debug.WriteLine("Callback Cleared");
				Callback = null;
				_nameOfCallback = String.Empty;
			}
			else
			{
				Debug.WriteLine("Callback Saved");
			}
		}

		//public void Restart()
		//{
		//	Stop(false);
		//	Start(null, String.Empty);
		//}

		private void OnTimerElapsed(object state)
		{
			Debug.WriteLine("Callback: " + _nameOfCallback);
			Callback?.Invoke();
		}
	}
}
