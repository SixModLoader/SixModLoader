# Installation

Recommended way to install SixModLoader is using [SixModLoader.Launcher](https://github.com/SixModLoader/SixModLoader.Launcher).

0. Install [.NET Core 3.1+](https://dotnet.microsoft.com/download/dotnet-core) *or not recommended [Mono](https://www.mono-project.com/download/stable/)*
1. Download launcher from [release page](https://github.com/SixModLoader/SixModLoader.Launcher/releases/latest)
   *(recommended .net core `linux` or `win`, or `net472` for mono/windows only, but only if you have to)*
2. Extract launcher executable from zip to server directory
3. Just run `SixModLoader[.exe] launch "LocalAdmin[.exe] 7777"` and it will download/update SixModLoader, configure Doorstop and launch SCP:SL with mod loader injected!

*TIP for people using hosting providers, if your provider allows replacing LocalAdmin/MultiAdmin, you can replace it with launcher and write your arguments in `jumploader.txt`*

Manual installation is a bit more complicated.
1. Extract [SixModLoader zip](https://github.com/SixModLoader/SixModLoader/releases/latest) to zip to server directory
2. Download Doorstop ([Unix](https://github.com/NeighTools/UnityDoorstop.Unix) or [Windows](https://github.com/NeighTools/UnityDoorstop))
3. Configure Doorstop to load `SixModLoader/bin/SixModLoader.dll`
4. Launch and you should be done *but without auto updates :(*