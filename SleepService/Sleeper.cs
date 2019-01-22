using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using Microsoft.Win32.SafeHandles;

namespace SleepService
{
	public class Sleeper
	{
		private readonly string ParamsFilePath = "C:\\Program Files\\SleepService\\SleepServiceParams.txt";
		private readonly Timer _timer;
		private int startHour, endHour, startMin, endMin;
		private readonly DateTime LastUpdateToParams = File.GetLastWriteTime(@"C:\Program Files\SleepService\SleepServiceParams.txt");

		public Sleeper()
		{
			_timer = new Timer(10000) { AutoReset = true };
			_timer.Elapsed += Timer_Elapsed;
			#region DEBUG
#if DEBUG
			_timer.Interval = 5000;
			_timer.AutoReset = false;
#endif
			#endregion
			// Start Time Hours, Minutes
			// xx, xx
			// End Time Hours, Minutes
			// xx, xx
			string[] values = File.ReadAllLines(ParamsFilePath);
			// Start Time Hours, Minutes
			string[] _startTime = values[1].Split(',');
			// End Time Hours, Minutes
			string[] _endTime = values[3].Split(',');
			startHour = int.Parse(_startTime[0].Replace(" ", ""));
			startMin = int.Parse(_startTime[1].Replace(" ", ""));
			endHour = int.Parse(_endTime[0].Replace(" ", ""));
			endMin = int.Parse(_endTime[1].Replace(" ", ""));
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			// Get new parameters if settings file updated while service is running
			if (LastUpdateToParams < File.GetLastWriteTime(ParamsFilePath))
			{
				string[] values = File.ReadAllText(ParamsFilePath).Split(',');
				// values [ startHour, startMin, endHour, endMin ]
				startHour = int.Parse(values[0]);
				startMin = int.Parse(values[1]);
				endHour = int.Parse(values[2]);
				endMin = int.Parse(values[3]);
			}
			TimeSpan idleTime = GetIdleTime(); // Time user hasn't touched PC

			DateTime startTime = DateTime.Parse(startHour.ToString() + ":" + startMin.ToString());
			DateTime endTime = DateTime.Parse(endHour.ToString() + ":" + endMin.ToString());
			endTime = (endTime.CompareTo(startTime) == -1) ? endTime.AddDays(1) : endTime;
			DateTime currentTime = DateTime.Now;
			Console.WriteLine("Current Time : {0}, Start Time : {1}, End Time {2}", 
				currentTime.ToLongTimeString(), startTime.ToLongTimeString(), endTime.ToLongTimeString());

			if (currentTime.CompareTo(startTime) >= 0 && currentTime.CompareTo(endTime) <= 0)
			{
				#region DEBUG
#if DEBUG
				// During Debug, Stop timer and WriteLine
				Stop();
				Console.WriteLine("Put Computer to Sleep Reached");
				_timer.Start();
#endif
				#endregion
				while (true)
				{
					if (GetIdleTime().Seconds > 15)
					{
						WakeUP wup = new WakeUP();
						wup.Woken += WakeUP_Woken;
						wup.SetWakeUpTime(DateTime.Parse(endHour.ToString() + ':' + endMin.ToString()));
						SetSuspendState(false, false, false);
						return;
					}
				}
			}

			// If currentTime is after midnight, The startTime & endTime will be for the current day.
			// This will check to see if the currentTime fits between both value for the previous day.
			// Example: startTime = 1/1 10:00 PM, endTime = 1/2 8:00 AM. When currentTime clicks midnight on 1/2, both times will add 1 day.
			// Now remove that day for the new check and see if currentTime fits in that range.
			startTime = startTime.Subtract(TimeSpan.FromDays(1));
			endTime = endTime.Subtract(TimeSpan.FromDays(1));
			if (currentTime.CompareTo(startTime) >= 0 && currentTime.CompareTo(endTime) <= 0)
			{
				while (true)
				{
					if (GetIdleTime().Seconds > 15)
					{
						WakeUP wup = new WakeUP();
						wup.Woken += WakeUP_Woken;
						wup.SetWakeUpTime(DateTime.Parse(endHour.ToString() + ':' + endMin.ToString()));
						SetSuspendState(false, false, false);
						return;
					}
				}
			}

			#if DEBUG
			_timer.Start();
			#endif
		}

		private TimeSpan GetIdleTime()
		{
			DateTime bootTime = DateTime.UtcNow.AddMilliseconds(-Environment.TickCount);
			LASTINPUTINFO lii = new LASTINPUTINFO
			{
				cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO))
			};
			GetLastInputInfo(ref lii);

			DateTime lastInputTime = bootTime.AddMilliseconds(lii.dwTime);
			return DateTime.UtcNow.Subtract(lastInputTime);
		}

		public void Start()
		{
			_timer.Start();
			#if DEBUG
			Console.WriteLine("Sleep Timer Start");
			#endif
		}

		public void Stop()
		{
			_timer.Stop();
			#if DEBUG
			Console.WriteLine("Sleep Timer Stop");
			#endif
		}

		[DllImport("user32.dll")]
		static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

		[StructLayout(LayoutKind.Sequential)]
		struct LASTINPUTINFO
		{
			public uint cbSize;
			public int dwTime;
		}

		[DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

		[DllImport("kernel32.dll")]
		public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWaitableTimer(SafeWaitHandle hTimer, [In] ref long pDueTime, 
			int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, bool fResume);

		private void WakeUP_Woken(object sender, EventArgs e)
		{
			// Do something 
		}
	}
}
