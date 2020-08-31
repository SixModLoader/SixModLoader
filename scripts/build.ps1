"../out", "../out/bin", "../out/mods" | ForEach { MD $_ }
dotnet build ../SixModLoader.sln -c Release