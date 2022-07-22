using System;
using System.Collections;
using System.ServiceProcess;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace ServiceManager
{
	class Manager
	{
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				if (args.Length == 0)
				{
					GetInstruction();
				}
				else
				{
					Process(args);
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
			}
		}
		public static void Process(string[] args)
		{
			string user = null, password = null, domain = null;
			int timeout = int.MinValue;

			if (args.Length > 2) {
				for (int i = 1; i < args.Length; i++)
				{
					if (args[i].ToUpper().StartsWith(Args.USER))
					{
						user = args[i].Substring(Args.USER.Length + 1);
					}
					else if (args[i].ToUpper().StartsWith(Args.PASSWORD))
					{
						password = args[i].Substring(Args.PASSWORD.Length + 1);
					}
					else if (args[i].ToUpper().StartsWith(Args.DOMAIN))
					{
						domain = args[i].Substring(Args.DOMAIN.Length + 1);
					}
				}
			}

			if (user != null && password != null && domain != null)
			{
				if (!ImpersonationUtil.Impersonate(user, password, domain))
				{
					Console.WriteLine("No such account found, Impersonation failed.");
					return;
				}
			}

			if (args.Length == 1 || args[1].StartsWith("/") || args[1].ToUpper().Equals(Args.STATUS))
			{
				GetServices(args[0]);
			}

			else if (!args[1].StartsWith("/"))
			{
				try
				{
					ServiceController service = new ServiceController(args[1], args[0]);

					if (args.Length == 2 || args[2].ToUpper().Equals(Args.STATUS))
					{
						GetService(service);
					}
					else if (args[2].ToUpper().Equals(Args.RESTART))
					{
						RestartService(service, timeout);
					}
					else if (args[2].ToUpper().Equals(Args.START))
					{
						StartService(service, timeout);
					}
					else if (args[2].ToUpper().Equals(Args.STOP))
					{
						StopService(service, timeout);
					}
					else
					{
						throw new ArgumentException("No such action : " + args[2]);
					}
				}

				finally
				{
					if (user != null && password != null && domain != null)
					{
						ImpersonationUtil.UnImpersonate();
					}
				}
			}
			else { GetInstruction(); }
		}

		private static void GetServices(string cName)
		{
			ServiceController[] services = ServiceController.GetServices(cName);
			foreach (ServiceController service in services)
			{
				Console.WriteLine(string.Format("{0} [ {1} ]",
					service.ServiceName, service.Status.ToString()));
			}
		}

		private static void GetService(ServiceController service)
		{
			string serviceName = service.ServiceName;
			string displayName = service.DisplayName;
			ServiceControllerStatus status = service.Status;

			Console.WriteLine(string.Format("Service Name                 : {0}", serviceName));
			Console.WriteLine(string.Format("Display Name                 : {0}", displayName));
			Console.WriteLine(string.Format("Service Status               : {0}", status.ToString()));
			Console.WriteLine(string.Format("Service Type                 : {0}", service.ServiceType.ToString()));
			Console.WriteLine(string.Format("Service Can Stop             : {0}", service.CanStop));
			Console.WriteLine(string.Format("Service Can Pause / Continue : {0}", service.CanPauseAndContinue));
			Console.WriteLine(string.Format("Service Can Shutdown         : {0}", service.CanShutdown));

			ServiceController[] dependedServices = service.DependentServices;
			Console.Write(string.Format("{0} Depended Service(s)        : ", dependedServices.Length.ToString()));

			int pos = 0;
			foreach (ServiceController dService in dependedServices)
			{
				Console.Write(string.Format("{0}{1}",
					((dependedServices.Length > 1 && pos > 0) ? ", " : string.Empty), dService.ServiceName));

				pos++;
			}

			Console.WriteLine();
		}

		private static void StartService(ServiceController service, int timeout)
		{
			if (ServiceControllerStatus.Stopped == service.Status)
			{

				Console.WriteLine("Starting service '{0}' on '{1}' ...",
					service.ServiceName, service.MachineName);

				service.Start();

				if (int.MinValue != timeout)
				{
					TimeSpan t = TimeSpan.FromSeconds(timeout);
					service.WaitForStatus(ServiceControllerStatus.Running, t);

				}
				else service.WaitForStatus(ServiceControllerStatus.Running);

				Console.WriteLine("Started service '{0}' on '{1}'",
					service.ServiceName, service.MachineName);
			}
			else
			{
				Console.WriteLine("Can not start service '{0}' on '{1}'",
					service.ServiceName, service.MachineName);

				Console.WriteLine("Service State '{0}'", service.Status.ToString());
			}
		}

		private static void StopService(ServiceController service, int timeout)
		{
			if (service.CanStop)
			{
				Console.WriteLine("Stopping service '{0}' on '{1}' ...",
					service.ServiceName, service.MachineName);

				service.Stop();

				if (int.MinValue != timeout)
				{
					TimeSpan t = TimeSpan.FromSeconds(timeout);
					service.WaitForStatus(ServiceControllerStatus.Stopped, t);

				}
				else service.WaitForStatus(ServiceControllerStatus.Stopped);

				Console.WriteLine("Stopped service '{0}' on '{1}'",
					service.ServiceName, service.MachineName);

			}
			else
			{
				Console.WriteLine("Can not stop service '{0}' on '{1}'",
					service.ServiceName, service.MachineName);

				Console.WriteLine("Service State '{0}'", service.Status.ToString());
			}
		}

		private static void RestartService(ServiceController service, int timeout)
		{
			if (ServiceControllerStatus.Stopped != service.Status)
			{
				StopService(service, timeout);
			}

			StartService(service, timeout);
		}

		public static void GetInstruction()
		{
			Console.WriteLine(Instruction);
		}

		public static readonly string Instruction = "ServiceManager.exe - Remotely manage services with .Net's ServiceController\r\n\r\n"
													+ "Usage:\r\n"
													+ "svcmgr [ computer name ] [ service name ] [ action ] [ additional flags ]\r\n\r\n"
													+ Args.STATUS + "            Display the status of one or all service(s).\r\n"
													+ Args.RESTART + "           Restart a service.\r\n"
													+ Args.STOP + "              Stop a service.\r\n"
													+ Args.START + "             Start a service.\r\n"
													+ Args.USER + "[:value]      Specify user name.\r\n"
													+ Args.PASSWORD + "[:value]  Specify user password.\r\n"
													+ Args.DOMAIN + "[:value]    Specify user domain.";
	}
	public class Args
	{
		public const string STATUS = "/STATUS";
		public const string RESTART = "/RESTART";
		public const string STOP = "/STOP";
		public const string START = "/START";
		public const string USER = "/USER";
		public const string PASSWORD = "/PASSWORD";
		public const string DOMAIN = "/DOMAIN";
	}

	public class ImpersonationUtil
	{
		[DllImport("advapi32.dll", CharSet = CharSet.Auto)]
		public static extern int LogonUser(
			string lpszUserName,
			String lpszDomain,
			String lpszPassword,
			int dwLogonType,
			int dwLogonProvider,
			ref IntPtr phToken);

		[DllImport("advapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
		public extern static int DuplicateToken(
			IntPtr hToken,
			int impersonationLevel,
			ref IntPtr hNewToken);

		private const int LOGON32_LOGON_INTERACTIVE = 2;
		private const int LOGON32_LOGON_NETWORK_CLEARTEXT = 4;
		private const int LOGON32_PROVIDER_DEFAULT = 0;
		private static WindowsImpersonationContext impersonationContext;

		public static bool Impersonate(string logon, string password, string domain)
		{
			Console.WriteLine("Working with impersonation");
			WindowsIdentity tempWindowsIdentity;
			IntPtr token = IntPtr.Zero;
			IntPtr tokenDuplicate = IntPtr.Zero;

			if (LogonUser(logon, domain, password, LOGON32_LOGON_INTERACTIVE,
					LOGON32_PROVIDER_DEFAULT, ref token) != 0)
			{

				if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
				{
					tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
					impersonationContext = tempWindowsIdentity.Impersonate();
					if (null != impersonationContext) return true;
				}
			}

			return false;
		}

		public static void UnImpersonate()
		{
			impersonationContext.Undo();
		}
	}
}