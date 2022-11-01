using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MiniSampler
{


    public class SamplePlayer
    {

        public bool isOpen;
        public int sampleRate = 48000;
        public string[] devNames;
        private int _openDev;
        public int openDev
        {
            get { return _openDev;  }
        }

        public double max1 = -120;
        public double max2 = -120;
        public double level1 = -120;
        public double level2 = -120;

        private class MixProvider: ISampleProvider
        {
            private WaveFormat _WaveFormat;
            public WaveFormat WaveFormat { get { return _WaveFormat; } }

            private SamplePlayer samplePlayer;

            public int Read(float[] buffer, int offset, int count)
            {
                // Array.Clear(buffer, offset, count);
                for (int i = 0; i < count; i++) buffer[i] = 0.0f;
                samplePlayer.playTo(ref buffer, offset, count);

                // Extract Signal Level
                double l1, l2,d1,d2;
                l1 = l2 = d1=d2=0;
                for (int i=0;i<count;i++)
                {
                    if ((i & 1) == 0) 
                    {
                        d1 += buffer[i];
                        l1 += buffer[i] * buffer[i];
                    } else
                    {
                        d2 += buffer[i];
                        l2 += buffer[i] * buffer[i];
                    }
                }
                int N = count / 2;
                d1 /= (double)N;
                d2 /= (double)N;
                l1 = Math.Sqrt(l1 / N - d1 * d1);
                l2 = Math.Sqrt(l2 / N - d2 * d2);
                if (l1 < 1e-20) l1 = 1e-20;
                if (l2 < 1e-20) l2 = 1e-20;
                l1 = 20.0 * Math.Log10(l1);
                l2 = 20.0 * Math.Log10(l2);
                if (l1 < -130) l1 = -130;
                if (l2 < -130) l2 = -130;
                if (l1 > samplePlayer.max1)
                    samplePlayer.max1 = l1;
                else
                {
                    if (samplePlayer.max1 > -130)
                        samplePlayer.max1 -= 0.5;
                    else
                        samplePlayer.max1 = -130;
                }
                if (l2 > samplePlayer.max2)
                    samplePlayer.max2 = l2;
                else
                {
                    if (samplePlayer.max2 > -130)
                        samplePlayer.max2 -= 0.5;
                    else
                        samplePlayer.max2 = -130;
                }
                samplePlayer.level1 = l1; //  samplePlayer.level1 * 0.9 + l1 * 0.1;
                samplePlayer.level2 = l2; //  samplePlayer.level2 * 0.9 + l2 * 0.1;
                return count;
            }

            public MixProvider(SamplePlayer _samplePlayer, int sampleRate)
            {
                samplePlayer = _samplePlayer;
                _WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
            }
        }


        private MixProvider mixProvider;
        private WaveOut waveOut;

        private class SampleItemPlayer
        {
            public bool active;
            public bool on;
            public SampleData data;
            public int pos;
            public float fadeval;
            public int id;

            public SampleItemPlayer()
            {
                active = false;
                id = -1;
            }

            public void playTo(ref float[] wave, int offset, int length)
            {
                if (!active) return;
                data.playTo(ref wave, offset, length, ref active, ref pos, ref on, ref fadeval);
                if (!active) data = null;
            }

            public void start(int _id, SampleData _data)
            {
                if (active) return;
                id = _id;
                data = _data;
                on = true;
                pos = 0;
                fadeval = (data.fadeInTime == 0.0f) ? 1.0f : 0.0f;
                active = true;
            }
        }

        private int maxChannels = 16;
        private SampleItemPlayer[] sampleItemPlayer;
        private int noteid = 0;

        public void playTo(ref float[] buffer, int offset, int length)
        {
            for (int i = 0; i < maxChannels; i++)
                sampleItemPlayer[i].playTo(ref buffer, offset, length);
        }

        private bool tryOpen(int dev)
        {
            try
            {
                WaveOut newWaveOut = new WaveOut();
                newWaveOut.DeviceNumber = dev;
                newWaveOut.DesiredLatency = 50;
                newWaveOut.NumberOfBuffers = 4;
                MixProvider newMixProvider = new MixProvider(this, sampleRate);
                newWaveOut.Init(newMixProvider);
                newWaveOut.Play();
                // Success --> Replace current driver with the new one
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                    mixProvider = null;
                }
                waveOut = newWaveOut;
                mixProvider = newMixProvider;
                _openDev = dev;

                isOpen = true;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public SamplePlayer()
        {
            isOpen = false;
            if (WaveOut.DeviceCount <= 0)
            {
                devNames = null;
                throw new Exception("No Audio Devices Found");
            }
            devNames = new string[WaveOut.DeviceCount];
            for (int i = 0; i < WaveOut.DeviceCount; i++)
                devNames[i] = WaveOut.GetCapabilities(i).ProductName;

            // Start Mixer
            sampleItemPlayer = new SampleItemPlayer[maxChannels];
            for (int i = 0; i < maxChannels; i++)
                sampleItemPlayer[i] = new SampleItemPlayer();


            _openDev = -2; // None
            waveOut = null;
            mixProvider = null;
        }

        public bool open(int n)
        {
            return tryOpen(n);
        }

        public int playon(SampleData data)
        {
            for (int i=0;i<maxChannels;i++)
                if (sampleItemPlayer[i].active == false)
                {
                    int id = noteid++;
                    sampleItemPlayer[i].start(id, data);
                    return id;
                }
            return -1;
        }

        public void playoff(int id)
        {
            if (id < 0) return;
            for (int i=0;i<maxChannels;i++)
            {
                if (sampleItemPlayer[i].id == id) 
                    sampleItemPlayer[i].on = false;
            }
        }

        public bool stillPlaying(int id)
        {
            if (id < 0) return false;
            for (int i=0;i<maxChannels;i++)
                if (sampleItemPlayer[i].id == id)
                    return sampleItemPlayer[i].active;
            return false;
        }

        public void stopHard()
        {
            for (int i=0;i<maxChannels;i++)
            {
                sampleItemPlayer[i].active = false;
            }
        }


    }
}
