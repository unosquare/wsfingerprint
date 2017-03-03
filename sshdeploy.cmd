mkdir tools
if not exist tools\nuget.exe powershell -command "&Invoke-WebRequest -Uri \"https://dist.nuget.org/win-x86-commandline/latest/nuget.exe\" -OutFile \"tools\nuget.exe\""
if not exist tools\SSHDeploy.1.0.3\tools\SshDeploy.exe tools\nuget.exe install SSHDeploy -OutputDirectory tools
tools\SSHDeploy.1.0.3\tools\SshDeploy.exe monitor -s Unosquare.WaveShare.FingerprintModule.Sample\bin\Debug t "/home/pi/deploy" -h 172.16.17.47 -u pi -w raspberry --pre "pkill -f Unosquare.WaveShare.FingerprintModule.Sample"