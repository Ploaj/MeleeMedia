using System;
using static MeleeMedia.Audio.GcAdpcmMath;

namespace MeleeMedia.Audio
{
    public static class GcAdpcmDecoder
    {
        public static short[] Decode(byte[] adpcm, short[] coefficients)
        {
            var SampleCount = ByteCountToSampleCount(adpcm.Length);
            //config = config ?? new GcAdpcmParameters { SampleCount = ByteCountToSampleCount(adpcm.Length) };
            var pcm = new short[SampleCount];

            if (SampleCount == 0)
            {
                return pcm;
            }

            int frameCount = SampleCount.DivideByRoundUp(SamplesPerFrame);
            int currentSample = 0;
            int outIndex = 0;
            int inIndex = 0;
            short hist1 = 0; //config.History1;
            short hist2 = 0; //config.History2;

            for (int i = 0; i < frameCount; i++)
            {
                byte predictorScale = adpcm[inIndex++];
                int scale = (1 << GetLowNibble(predictorScale)) * 2048;
                int predictor = GetHighNibble(predictorScale);
                short coef1 = coefficients[predictor * 2];
                short coef2 = coefficients[predictor * 2 + 1];

                int samplesToRead = Math.Min(SamplesPerFrame, SampleCount - currentSample);

                for (int s = 0; s < samplesToRead; s++)
                {
                    int adpcmSample = s % 2 == 0 ? GetHighNibbleSigned(adpcm[inIndex]) : GetLowNibbleSigned(adpcm[inIndex++]);
                    int distance = scale * adpcmSample;
                    int predictedSample = coef1 * hist1 + coef2 * hist2;
                    int correctedSample = predictedSample + distance;
                    int scaledSample = (correctedSample + 1024) >> 11;
                    short clampedSample = Clamp16(scaledSample);

                    hist2 = hist1;
                    hist1 = clampedSample;

                    pcm[outIndex++] = clampedSample;
                    currentSample++;
                }
            }
            return pcm;
        }

        public static byte GetPredictorScale(byte[] adpcm, int sample)
        {
            return adpcm[sample / SamplesPerFrame * BytesPerFrame];
        }
    }
}
