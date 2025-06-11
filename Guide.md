# This guide already assumes you have werz's [downgrade patch](https://www.dropbox.com/scl/fi/57szcf9d5cn83jzedu6jf/PVZBFNPREEAAC.7z?rlkey=9ltff7i5tm7mlavdg8ws26hjx&st=ck880b7u&dl=0) for bfn ❗❗❗

# Steam users

The reason why I'm recommending the environment variable is that EA App doesn't allow launch options with special characters. If you have the game on steam just put this to the launch options section; `-dataPath ModData/Default` and skip `step 6`

# How to use this thing

1.Download the binaries from the [releases section](https://github.com/Twig6943/FrostyToolsuiteBFNLinux/releases) (Run as administrator and goto step 5 if you are on windows.)

2.Add the following dlloverrides to your wineprefix via `winecfg` :


```
winmm
RtWorkQ
```


(Should be set to `native,builtin`)

3.Run `taskmgr` inside the wineprefix

4.Select the .exe for the fmm bfn linux build

5.Load & apply the mods you want

6.Add the `GAME_DATA_DIR` environment variable and as for its variable put the path to your modpack folder's location

(You need to get the ModData path using a wine/windows explorer and not your native linux file explorer ❗❗❗)

`/home/twig/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/PVZ Battle for Neighborville/ModData/Default` ❌

`Z:\home\twig\.var\app\com.valvesoftware.Steam\.local\share\Steam\steamapps\common\PVZ Battle for Neighborville\ModData\Default` ✅

![](/assets/1.png)

7.Launch the game and everything should work.
