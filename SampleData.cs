using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSampler
{

    public class SampleData
    {
        // Root
        SamplePlayer samplePlayer;

        // Raw Data
        public float[] data;

        // Config
        public float volume = 100; // 0..100
        public float fadeInTime = 0; // in ms
        public float fadeOutTime = 0; // in ms
        public bool loop = false;
        public int loopstart = 0;
        public int loopstop = 0;

        public bool isValid()
        {
            return (data != null) && (data.Length > 0);
        }

        public int samples
        {
            get { if (data != null) return data.Length / 2; else return 0; }
        }


        public SampleData(SamplePlayer _samplePlayer, string fn)
        {
            this.samplePlayer = _samplePlayer;
            // Load A File
            if (fn.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase))
            {
                // Load a Wav File
                loadWavFile(fn);
                if (isValid())
                    loopstop = samples-1;
            }
            else if (fn.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase))
            {
                loadMP3File(fn);
                if (isValid())
                    loopstop = samples - 1;
            }
            else
            {
                throw new Exception("Unsupported File Type");
            }
        }

        private struct floatblock
        {
            public int len;
            public int use;
            public float[] data;
            public floatblock(int _len) { len = _len; data = new float[len]; use = 0; }
            public void release() { data = null; len = use = 0; }
        }

        /*
        private void loadRawWavFile(WaveFileReader wfr)
        {
            // Direct Read
            data = new float[wfr.SampleCount * wfr.WaveFormat.Channels];
            if (data == null)
                throw new Exception("Out of Memory");
            int length = (int)wfr.SampleCount;
            int channels = wfr.WaveFormat.Channels;

            int bytesPerSample = wfr.WaveFormat.Channels * wfr.WaveFormat.BitsPerSample / 8;
            int bytesPerChannel = wfr.WaveFormat.BitsPerSample / 8;

            if (wfr.Length >= Int32.MaxValue)
                throw new Exception("WAV File is too long");
            byte[] bytebuffer = new byte[wfr.Length];
            if (bytebuffer == null)
                throw new Exception("Out of Memory");
            if (wfr.Read(bytebuffer, 0, (int)wfr.Length) != (int)wfr.Length)
                throw new Exception("Read Error");
            if (wfr.Length != wfr.SampleCount * wfr.WaveFormat.Channels * (wfr.WaveFormat.BitsPerSample / 8))
                throw new Exception("Strange WAV File Length - Inforation mismatch");

            switch (wfr.WaveFormat.BitsPerSample)
            {
                case 32:
                    for (int i = 0; i < (length * channels); i++)
                    {
                        int sig = BitConverter.ToInt32(bytebuffer, i * 4);
                        data[i] = (float)sig / Int32.MaxValue;
                    }
                    break;
                case 24:
                    byte[] temp24 = new byte[4];
                    for (int i = 0; i < (length * channels); i++)
                    {
                        temp24[1] = bytebuffer[i * 3 + 0];
                        temp24[2] = bytebuffer[i * 3 + 1];
                        temp24[3] = bytebuffer[i * 3 + 2];
                        int sig = BitConverter.ToInt32(temp24, 0);
                        data[i] = (float)sig / Int32.MaxValue;
                    }
                    break;
                case 16:
                    for (int i = 0; i < (length * channels); i++)
                    {
                        Int16 sig = BitConverter.ToInt16(bytebuffer, i * 2);
                        data[i] = (float)sig / Int16.MaxValue;
                    }
                    break;
                case 8:
                    for (int i = 0; i < (length * channels); i++)
                    {
                        char sig = BitConverter.ToChar(bytebuffer, i);
                        data[i] = (float)sig / 127;
                    }
                    break;
                default:
                    throw new Exception("Bad Wave File Bit WIdth");
            }
            wfr.Close();
        }
        */

        private void loadSamplesFromStereoSampleProvider(ISampleProvider sampleProvider)
        {
            List<floatblock> dataimd = new List<floatblock>();
            int maxN = 1024 * 2;
            float[] bytebuffer = new float[maxN];
            int n = sampleProvider.Read(bytebuffer, 0, maxN);
            int smps = 0;
            while (n > 0)
            {
                floatblock fbk = new floatblock(n);
                for (int i = 0; i < n; i++)
                    fbk.data[i] = bytebuffer[i];
                fbk.use = n;
                smps += n;
                dataimd.Add(fbk);
                n = sampleProvider.Read(bytebuffer, 0, maxN);
            }
            data = new float[smps];
            if (data == null)
                throw new Exception("Out of Memory");
            // length = smps / 2;
            int idx = 0;
            foreach (floatblock f in dataimd)
            {
                Array.Copy(f.data, 0, data, idx, f.use);
                idx += f.use;
            }
            foreach (floatblock f in dataimd)
            {
                f.release();
                dataimd.Remove(f);
            }
            dataimd = null;
        }

        private void loadWavFile(string fn)
        {
            WaveFileReader wfrBase = new WaveFileReader(fn);
            if ((wfrBase.WaveFormat.SampleRate != samplePlayer.sampleRate) || (wfrBase.WaveFormat.Channels != 2))
            {
                // Must reformat or resample
                WaveFormat newformat = WaveFormat.CreateIeeeFloatWaveFormat(samplePlayer.sampleRate, 2);
                ResamplerDmoStream wfr2 = new NAudio.Wave.ResamplerDmoStream(wfrBase, newformat);
                loadSamplesFromStereoSampleProvider(wfr2.ToSampleProvider());
                wfr2.Close();
            } else
            {
                // Take it one by one
                loadSamplesFromStereoSampleProvider(wfrBase.ToSampleProvider());
            }
            wfrBase.Close();
        }
        private void loadMP3File(string fn)
        {
            Mp3FileReader wfrBase = new Mp3FileReader(fn);
            WaveFormat newformat = WaveFormat.CreateIeeeFloatWaveFormat(samplePlayer.sampleRate, 2);
            ResamplerDmoStream wfr2 = new NAudio.Wave.ResamplerDmoStream(wfrBase, newformat);
            loadSamplesFromStereoSampleProvider(wfrBase.ToSampleProvider());
            wfr2.Close();
            wfrBase.Close();
        }

        public void store(BinaryWriter bwr)
        {
            bwr.Write((int)0); // Version
            bwr.Write(isValid());
            if (!isValid()) return;
            bwr.Write(data.Length);
            for (int i = 0; i < data.Length; i++)
                bwr.Write(data[i]);
            bwr.Write(volume);
            bwr.Write(fadeInTime);
            bwr.Write(fadeOutTime);
            bwr.Write(loop);
            bwr.Write(loopstart);
            bwr.Write(loopstop);
        }

        public SampleData(SamplePlayer _samplePlayer, BinaryReader brd)
        {
            samplePlayer = _samplePlayer;
            int ver = brd.ReadInt32();
            bool valid = brd.ReadBoolean();
            if (!valid)
            {
                data = null;
                loop = false;
                volume = 100.0f;
                fadeInTime = fadeOutTime = 0.0f;
                loopstart = loopstop = 0;
                return;
            }
            int len = brd.ReadInt32();
            data = new float[len];
            if (data == null)
                throw new Exception("Out of Memory");
            for (int i = 0; i < len; i++)
                data[i] = brd.ReadSingle();
            volume = brd.ReadSingle();
            fadeInTime = brd.ReadSingle();
            fadeOutTime = brd.ReadSingle();
            loop = brd.ReadBoolean();
            loopstart = brd.ReadInt32();
            loopstop = brd.ReadInt32();
        }


        public void playTo(ref float[] wave, int offset, int count, ref bool active, ref int sample, ref bool on, ref float fadestate)
        {
            for (int i = 0; i < count / 2; i++)
            {
                if (active)
                {
                    float s1 = data[2 * sample + 0];
                    float s2 = data[2 * sample + 1];
                    sample++;
                    if (loop && on)
                    {
                        if ((loopstop <= loopstart) || (loopstop >= data.Length/2))
                        {
                            // Bad Loopstop definition
                            if (sample >= data.Length / 2)
                                sample = loopstart;
                        } else
                        {
                            if (sample >= loopstop)
                                sample = loopstart;
                        }
                    } else
                    {
                        // No loop
                        if (sample >= data.Length / 2)
                            active = false; // Done
                    }
                    if (on)
                    {
                        if (fadeInTime > 0)
                        {
                            if (fadestate < 1.0f)
                                fadestate += 1.0f / (fadeInTime * samplePlayer.sampleRate / 1000.0f);
                            if (fadestate > 1.0f)
                                fadestate = 1.0f;
                        }
                        else
                            fadestate = 1.0f;
                    }
                    else
                    {
                        if (fadeOutTime > 0)
                        {
                            if (fadestate > 0.0f)
                                fadestate -= 1.0f / (fadeOutTime * samplePlayer.sampleRate / 1000.0f);
                            if (fadestate < 0.0f)
                                fadestate = 0.0f;
                            if (fadestate == 0.0f)
                                active = false;
                        }
                        else
                        {
                            fadestate = 0.0f;
                            active = false;
                        }
                    }
                    s1 *= fadestate * volume / 100.0f;
                    s2 *= fadestate * volume / 100.0f;
                    wave[offset + 2 * i + 0] += s1;
                    wave[offset + 2 * i + 1] += s2;
                }
            }
        }


    }


}
