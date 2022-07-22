## ServiceManager
C# Utility to remotely manage services through execute-assembly either with credentials or using your current session.

```
ServiceManager.exe - Remotely manage services with .Net's ServiceController

Usage:
ServiceManager.exe [ computer name ] [ service name ] [ action ] [ additional flags ]

/STATUS            Display the status of one or all service(s).
/RESTART           Restart a service.
/STOP              Stop a service.
/START             Start a service.
/USER[:value]      Specify user name.
/PASSWORD[:value]  Specify user password.
/DOMAIN[:value]    Specify user domain.
```

Initial project from https://www.codeproject.com/Articles/4242/Command-Line-Windows-Services-Manager. I mostly removed some unnecessary functionality and fixed some logic bugs.