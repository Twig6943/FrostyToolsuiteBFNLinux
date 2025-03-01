# How to use this thing

# This guide already assumes you have werz's [downgrade patch](https://www.dropbox.com/scl/fi/57szcf9d5cn83jzedu6jf/PVZBFNPREEAAC.7z?rlkey=9ltff7i5tm7mlavdg8ws26hjx&st=ck880b7u&dl=0) for bfn ❗❗❗

1.Download the binaries from the [releases section](https://github.com/Twig6943/FrostyToolsuiteBFNLinux/releases)

2.Add the following dlloverrides to your wineprefix via `winecfg` :


```
winmm
RtWorkQ
```


(Should be set to `native,builtin`)

3.Run `taskmgr` inside the wineprefix

4.Select the .exe for the fmm bfn linux build

5.Load & apply the mods you want

6.Add the `GAME_DATA_DIR` and the path to your modpack (for most people its just `C:\Program Files\EA Games\Plants vs Zombies Garden Warfare\ModData\Default`) 

(You need to get the path for that folder using a wine/windows explorer)

![image](https://github.com/user-attachments/assets/201b2a05-787c-4c91-bf0a-a8f1af8ff79e)

7.Launch the game and everything should work.


