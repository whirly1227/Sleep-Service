using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Threading;

namespace SleepService
{
	class WakeUP
	{
		[DllImport("kernel32.dll")]
		public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes,
																  bool bManualReset,
																string lpTimerName);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWaitableTimer(SafeWaitHandle hTimer,
													[In] ref long pDueTime,
															  int lPeriod,
														   IntPtr pfnCompletionRoutine,
														   IntPtr lpArgToCompletionRoutine,
															 bool fResume);

		public event EventHandler Woken;

		private BackgroundWorker bgWorker = new BackgroundWorker();

		public WakeUP()
		{
			bgWorker.DoWork += new DoWorkEventHandler(BgWorker_DoWork);
			bgWorker.RunWorkerCompleted +=
			  new RunWorkerCompletedEventHandler(BgWorker_RunWorkerCompleted);
		}

		public void SetWakeUpTime(DateTime time)
		{
			bgWorker.RunWorkerAsync(time.ToFileTime());
		}

		void BgWorker_RunWorkerCompleted(object sender,
					  RunWorkerCompletedEventArgs e)
		{
			Woken?.Invoke(this, new EventArgs());
		}

		private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			long waketime = (long)e.Argument;

			using (SafeWaitHandle handle =
					  CreateWaitableTimer(IntPtr.Zero, true,
					  this.GetType().Assembly.GetName().Name.ToString() + "Timer"))
			{
				if (SetWaitableTimer(handle, ref waketime, 0,
									   IntPtr.Zero, IntPtr.Zero, true))
				{
					using (EventWaitHandle wh = new EventWaitHandle(false,
														   EventResetMode.AutoReset))
					{
						wh.SafeWaitHandle = handle;
						wh.WaitOne();
					}
				}
				else
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}
			}
		}

	}
}