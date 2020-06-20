# MeleeMedia Tool 

[Download Latest](https://github.com/Ploaj/MeleeMedia/releases) [![Build status](https://ci.appveyor.com/api/projects/status/6tr5rq9adkkj7dv9?svg=true)](https://ci.appveyor.com/project/Ploaj/meleemedia)

MeleeMedia is a command line tool that can convert to and from .mth, .hps, and .thp found in Super Smash Bros Melee.

## Requirements

.NET Framework 4.6.1

## Usage

```python
MeleeMedia.exe (input) (output)

Example: MeleeMedia.exe in.mp4 out.mth
Example: MeleeMedia.exe in.mp3 out.dps -loop 00:00:25.34325

Supported Formats:
Video - mth, mp4
Image - thp, png, jpeg, jpg
Audio Input - dsp, wav, hps, mp3, aiff, wma, m4a
Audio Output - dsp, wav, hps
Specify Loop "-loop [d.]hh:mm:ss[.fffffff]"
```

## References and Thanks

* **CSCore**
* Copyright (c) 2017 Florian R.
* MIT License: https://github.com/filoe/cscore/blob/master/license.md
* Used for converting common audio formats


* **AForge**
* Copyright (c) 2013 AForge.NET
* GPL v3 License: http://www.aforgenet.com/framework/license.html
* Used for converting to and from mp4


* **VGAudio**
* Copyright (c) 2016 Alex Barney
* MIT License: https://raw.githubusercontent.com/Thealexbarney/VGAudio/master/LICENSE
* used for encoding audio
