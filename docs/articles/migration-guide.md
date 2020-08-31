# Migrating from Exiled 2.0
1. Make sure to have unmodified game files (especially `Assembly-CSharp.dll`), run game update with `validate` option
2. Clear all current Exiled dlls
3. [Install SixModLoader](installation.md)
4. Download [`SixModLoader.Compatibility.dll`](https://github.com/SixModLoader/SixModLoader.Compatibility.Exiled/releases) and drop it into `SixModLoader/mods`
5. Run server once so it will download Exiled and create default `config.yml` 
6. Move Exiled config to `SixModLoader/mods/Exiled/config.yml`