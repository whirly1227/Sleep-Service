using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace SleepService
{
	public partial class Program
	{
		static void Main(string[] args)
		{
			#region DEBUG
#if DEBUG
			Sleeper _sleeper = new Sleeper();
			_sleeper.Start();
			Console.Read();
#endif
			#endregion

			var exitCode = HostFactory.Run(x =>
			{
				x.Service<Sleeper>(s =>
				{
					s.ConstructUsing(sleeper => new Sleeper());
					s.WhenStarted(sleeper => sleeper.Start());
					s.WhenStopped(sleeper => sleeper.Stop());
				});

				x.RunAsLocalSystem();

				x.SetServiceName("MySleepService");
				x.SetDescription("My Sleep Service");
				x.SetDescription("Sleeps the computer based on the number set in the SleepServiceParams.txt file.");
			});

			Environment.ExitCode = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
			Console.Read();
		}
	}
}
