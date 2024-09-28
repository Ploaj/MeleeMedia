using MeleeMedia.Audio;
using MeleeMedia.Video;
using MeleeMediaCLI.Video;
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

                // video options
                long video_compression = 25L;
                int frameWidth = -1; // 448;
                int frameHeight = -1; // 336;
                string outputImageFormat = "bmp";

                // image options
                long image_compression = 99L;

                string loopPoint = "00:00:00";
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-loop")
                    {
                        loopPoint = args[i + 1];
                    }

                    if (args[i] == "-comp")
                    {
                        long.TryParse(args[i + 1], out video_compression);
                        image_compression = video_compression;
                    }

                    if (args[i] == "-res" && i + 2 < args.Length)
                    {
                        if (int.TryParse(args[i + 1], out frameWidth) &&
                            int.TryParse(args[i + 2], out frameHeight))
                        {
                            Console.WriteLine($"Frame Size set to {frameWidth} {frameHeight}");
                        }
                    }

                    if (args[i] == "-ext")
                    {
                        outputImageFormat = args[i + 1];
                    }
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
                    case ".brstm":
                        {
                            var dsp = new DSP();
                            dsp.FromBRSTM(inf);

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
                        }
                        break;
                    case ".dsp":
                    case ".wav":
                    case ".hps":
                    case ".mp3":
                    case ".aiff":
                    case ".wma":
                    case ".m4a":
                        {
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
                        }
                        break;

                    case ".mth":
                        if (oext == "")
                        {
                            if (!VideoConverter.MTHtoImages(new MTH(inf), outf, outputImageFormat))
                            {
                                Console.WriteLine($"Unsupported export format " + oext);
                            }
                        }
                        else
                        if (oext == ".mp4")
                        {
                            VideoConverter.MTHtoMP4(inf, outf);
                        }
                        else
                        {
                            Console.WriteLine($"Unsupported export format " + oext);
                        }
                        break;
                    case ".mp4":
                        if (oext == ".mth")
                            VideoConverter.MP4toMTH(inf, outf, frameWidth, frameHeight, video_compression);
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
                                bmp.ToTHP(image_compression).Save(outf);
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
            Console.WriteLine("\tSpecify Compression (default 25) -comp 50");

        }
    }
}
