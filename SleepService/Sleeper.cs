using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace SleepService
{
	public class Sleeper
	{
		private readonly string ParamsFilePath = "C:\\Program Files\\SleepService\\SleepServiceParams.txt";
		private Timer _timer;
		private int startHour, endHour, startMin, endMin;
		private readonly DateTime LastUpdateToParams = File.GetLastWriteTime(@"C:\Program Files\SleepService\SleepServiceParams.txt");
		private DateTime LastWake;
		private long SleeperCounter = 0;
		private int SleepWaitTime = 5000;
		private DateTime startTime, startTime2, endTime, endTime2;

		public Sleeper()
		{
			// _timer set for 10 seconds
			_timer = new Timer(SleepWaitTime){ AutoReset = true };
			_timer.Elapsed += Timer_Elapsed;
			#region DEBUG
#if DEBUG
			_timer.Interval = SleepWaitTime;
			_timer.AutoReset = false;
#endif
			#endregion
			GetNewStartEndTime();
			LastWake = DateTime.Now; // For initialization
			#if DEBUG
			Console.WriteLine("Current Time : {0}, Start Time : {1}, End Time {2}",
				DateTime.Now.ToLongTimeString(), startTime.ToLongTimeString(), endTime.ToLongTimeString());
			#endif
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			// Restart _timer if counter gets to 1000
			SleeperCounter++;
			if (SleeperCounter >= 1000) RestartTimer();

			// Get new parameters if settings file updated while service is running
			if (LastUpdateToParams < File.GetLastWriteTime(ParamsFilePath)) GetNewStartEndTime();

			DateTime currentTime = DateTime.Now;
			#if DEBUG
			Console.WriteLine("Current Time : {0:HH\\:mm\\:ss MM/dd}", currentTime);
			#endif

			// If current time is after start time and before end time
			if (currentTime.CompareTo(startTime) >= 0 && currentTime.CompareTo(endTime) <= 0)
			{
				Stop();
				#region DEBUG
#if DEBUG
				// During Debug, Stop timer and WriteLine
				//Stop();
				Console.WriteLine("Put Computer to Sleep Reached");
				//_timer.Start();
#endif
				#endregion
				// While current day hasn't increased (Before Midnight)
				while (DateTime.Now.Day < startTime.AddDays(1).Day)
				{
					#region DEBUG
#if DEBUG
					Console.WriteLine("Last Wake    : " + LastWake.ToShortTimeString());
#endif
					#endregion
					if (GetIdleTime().Minutes > 15 && DateTime.Compare(LastWake.AddMinutes(15), DateTime.Now) >= 0)
					{
						WakeUP wup = new WakeUP();
						wup.Woken += WakeUP_Woken;
						wup.SetWakeUpTime(DateTime.Parse(endHour.ToString() + ':' + endMin.ToString()));
						SetSuspendState(false, false, false);
						return;
					}
					System.Threading.Thread.Sleep(SleepWaitTime);
				}
				Start();
				return;
			}

			// If currentTime is after midnight, The startTime & endTime will be for the current day.
			// This will check to see if the currentTime fits between both value for the previous day.
			// Example: startTime = 1/1 10:00 PM, endTime = 1/2 8:00 AM. When currentTime clicks midnight on 1/2, both times will add 1 day.
			// Now remove that day for the new check and see if currentTime fits in that range.
			startTime2 = startTime.Subtract(TimeSpan.FromDays(1));
			endTime2 = endTime.Subtract(TimeSpan.FromDays(1));
			if (currentTime.CompareTo(startTime2) >= 0 && currentTime.CompareTo(endTime2) <= 0)
			{
				Stop();
				#region DEBUG
#if DEBUG
				// During Debug, Stop timer and WriteLine
				//Stop();
				Console.WriteLine("Put Computer to Sleep Reached");
				//_timer.Start();
#endif
				#endregion
				while (true)
				{
					#region DEBUG
#if DEBUG
					Console.WriteLine("Last Wake    : " + LastWake.ToShortTimeString());
#endif
					#endregion
					if (GetIdleTime().Minutes >= 15 && DateTime.Compare(LastWake.AddMinutes(15), DateTime.Now) >= 0)
					{
						WakeUP wup = new WakeUP();
						wup.Woken += WakeUP_Woken;
						wup.SetWakeUpTime(DateTime.Parse(endHour.ToString() + ':' + endMin.ToString()));
						SetSuspendState(false, false, false);
						return;
					}
					System.Threading.Thread.Sleep(SleepWaitTime);
				}
			}
			#region DEBUG
#if DEBUG
			_timer.Start();
#endif
			#endregion
		}

		/// <summary>
		/// Gets the start and end times from the Settings file
		/// </summary>
		private void GetNewStartEndTime()
		{
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
			startTime = DateTime.Parse(startHour.ToString() + ":" + startMin.ToString());
			endHour = int.Parse(_endTime[0].Replace(" ", ""));
			endMin = int.Parse(_endTime[1].Replace(" ", ""));
			endTime = DateTime.Parse(endHour.ToString() + ":" + endMin.ToString());
			endTime = (endTime.CompareTo(startTime) == -1) ? endTime.AddDays(1) : endTime;
			#region DEBUG
			#if DEBUG
			Console.WriteLine("Start Time   : {0:HH\\:mm\\:ss MM/dd}", startTime);
			Console.WriteLine("End Time     : {0:HH\\:mm\\:ss MM/dd}", endTime);
			#endif
			#endregion
		}

		// Copied code from Stack Overflow
		private TimeSpan GetIdleTime()
		{ 
			DateTime bootTime = DateTime.Now.AddMilliseconds(-Environment.TickCount);
			LASTINPUTINFO lii = new LASTINPUTINFO
			{
				cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO))
			};
			GetLastInputInfo(ref lii);
			DateTime lastInputTime = bootTime.AddMilliseconds(lii.dwTime);
			#region DEBUG
#if DEBUG
			//Console.WriteLine("Boot Time       : " + bootTime);
			Console.WriteLine("Idle Time    : {0:mm\\:ss}", DateTime.Now.Subtract(lastInputTime));
#endif
			#endregion
			return DateTime.Now.Subtract(lastInputTime);
		}

		public void Start()
		{
			_timer.Start();
			#if DEBUG
			Console.WriteLine("Sleep Timer  : Start");
			#endif
		}

		public void Stop()
		{
			_timer.Stop();
			#if DEBUG
			Console.WriteLine("Sleep Timer  : Stop");
			#endif
		}

		/// <summary>
		/// Restart Timer and dispose of resources. This will decrease system usage.
		/// </summary>
		private void RestartTimer()
		{
			SleeperCounter = 0;
			#if DEBUG
			Console.WriteLine("Restart Time    : " + DateTime.Now);
			#endif
			Stop();
			_timer.Dispose();
			_timer = new Timer(SleepWaitTime) { AutoReset = true };
			_timer.Elapsed += Timer_Elapsed;
			Start();
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
			#if DEBUG
			Console.WriteLine("Computer Awoken");
			#endif
			// Sets the time for the last woken time so that computer doesn't go back to sleep.
			LastWake = DateTime.Now;
		}
	}
}
