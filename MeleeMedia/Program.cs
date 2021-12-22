using MeleeMedia.Audio;
using MeleeMedia.Video;
using System;
using System.Drawing;
using System.IO;

namespace MeleeMediaCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                var inf = args[0];
                var outf = args[1];

                int frameWidth = 448;
                int frameHeight = 336;
                int frameRate = 30;

                string loopPoint = "00:00:00";
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-loop")
                        loopPoint = args[i + 1];
                }

                if (!File.Exists(inf))
                {
                    Console.WriteLine($"{inf} not found");
                    return;
                }

                if (!TimeSpan.TryParse(loopPoint, out TimeSpan ts))
                {
                    Console.WriteLine("Loop in wrong format, expected: hh:mm:ss");
                    return;
                }

                var iext = Path.GetExtension(inf).ToLower();
                var oext = Path.GetExtension(outf).ToLower();

                switch (iext)
                {
                    case ".dsp":
                    case ".wav":
                    case ".hps":
                    case ".mp3":
                    case ".aiff":
                    case ".wma":
                    case ".m4a":
                        var dsp = new DSP(inf);
                        dsp.SetLoopFromTimeSpan(ts);

                        switch (oext)
                        {
                            case ".wav":
                            case ".dsp":
                            case ".hps":
                                dsp.ExportFormat(outf);
                                break;
                            default:
                                Console.WriteLine($"Unsupported export format " + oext);
                                return;
                        }
                        break;

                    case ".mth":
                        if (oext == ".mp4")
                            VideoConverter.MTHtoMP4(new MTH(inf), outf);
                        else
                            Console.WriteLine($"Unsupported export format " + oext);
                        break;
                    case ".mp4":
                        if (oext == ".mth")
                            VideoConverter.MP4toMTH(inf, frameWidth, frameHeight, frameRate).Save(outf);
                        else
                            Console.WriteLine($"Unsupported export format " + oext);
                        break;
                    case ".thp":
                        if (oext == ".png" ||
                            oext == ".jpeg" ||
                            oext == ".jpg")
                        {
                            var thp = new THP(File.ReadAllBytes(inf));
                            using (var bmp = thp.ToBitmap())
                                bmp.Save(outf);
                        }
                        else
                            Console.WriteLine($"Unsupported export format " + oext);
                        break;
                    case ".png":
                    case ".jpeg":
                    case ".jpg":
                        if (oext == ".thp")
                            using (var bmp = new Bitmap(inf))
                                THP.FromBitmap(bmp).Save(outf);
                        else
                            Console.WriteLine($"Unsupported export format " + oext);
                        break;
                    default:
                        Console.WriteLine($"Unsupported format " + inf);
                        return;
                }

            }
            else
                PrintHelp();
        }

        /// <summary>
        /// 
        /// </summary>
        static void PrintHelp()
        {
            Console.WriteLine($"MeleeMedia.exe (input) (output)");
            Console.WriteLine();
            Console.WriteLine($"Example: MeleeMedia.exe in.mp4 out.mth");
            Console.WriteLine($"Example: MeleeMedia.exe in.mp3 out.dps -loop 00:00:25");
            Console.WriteLine();
            Console.WriteLine("Supported Formats:");
            Console.WriteLine("\tVideo - mth, mp4");
            Console.WriteLine("\tImage - thp, png, jpeg, jpg");
            Console.WriteLine("\tAudio Input - dsp, wav, hps, mp3, aiff, wma, m4a");
            Console.WriteLine("\tAudio Output - dsp, wav, hps");
            Console.WriteLine("\tSpecify Loop -loop [d.]hh:mm:ss[.fffffff]");

        }
    }
}
