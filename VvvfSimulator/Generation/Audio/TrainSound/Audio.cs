using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Forms;
using VvvfSimulator.Data.BaseFrequency;
using VvvfSimulator.Generation.Video.ControlInfo;
using VvvfSimulator.GUI.Simulator.RealTime.Controller.Design2;
using VvvfSimulator.Vvvf;
using VvvfSimulator.Vvvf.Calculation;
using static VvvfSimulator.Data.TrainAudio.Struct;
using static VvvfSimulator.Generation.Audio.RealTime;
using static VvvfSimulator.Generation.Audio.TrainSound.AudioFilter;
using static VvvfSimulator.Generation.GenerateCommon;
using static VvvfSimulator.Vvvf.Model.Struct;
using static VvvfSimulator.Vvvf.MyMath;
using Design2 = VvvfSimulator.GUI.Simulator.RealTime.Controller.Design2.Design2;

namespace VvvfSimulator.Generation.Audio.TrainSound
{
    public class Audio
    {
        public readonly struct TrainSoundResult(double main, double white, double surround)
        {
            public readonly double Main = main;
            public readonly double White = white;
            public readonly double Surround = surround;
        }

        public class WhiteNoiseMixer(ISampleProvider main, ISampleProvider white) : ISampleProvider
        {
            private readonly ISampleProvider _main = main;
            private readonly ISampleProvider _white = white;

            public WaveFormat WaveFormat => _main.WaveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                int mainRead = _main.Read(buffer, offset, count);
                float[] whiteBuffer = new float[count];
                int whiteRead = _white.Read(whiteBuffer, 0, count);

                int maxRead = Math.Max(mainRead, whiteRead);
                for (int i = 0; i < maxRead; i++)
                {
                    float mainSample = (i < mainRead) ? buffer[offset + i] : 0f;
                    float whiteSample = (i < whiteRead) ? whiteBuffer[i] : 0f;
                    buffer[offset + i] = mainSample + whiteSample;
                }
                return maxRead;
            }
        }

        // Low-pass filter previous value for surround signal
        private static double _surroundLpfPrev = 0.0;

        public class SurroundStereoProvider(ISampleProvider mono, ISampleProvider surround) : ISampleProvider
        {
            private readonly ISampleProvider _mono = mono;
            private readonly ISampleProvider _surround = surround;
            private readonly WaveFormat _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(mono.WaveFormat.SampleRate, 2);
            private readonly float[] _shortDelayBuffer = new float[Math.Max(1, (int)(mono.WaveFormat.SampleRate * 0.009))];
            private readonly float[] _longDelayBuffer = new float[Math.Max(1, (int)(mono.WaveFormat.SampleRate * 0.023))];
            private float[] _monoBuffer = [];
            private float[] _surroundBuffer = [];
            private int _shortDelayIndex = 0;
            private int _longDelayIndex = 0;

            public WaveFormat WaveFormat => _waveFormat;

            private static float ClampSample(float x)
            {
                if (x > 0.98f) return 0.98f;
                if (x < -0.98f) return -0.98f;
                return x;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                if (_mono.WaveFormat.Channels != 1)
                    throw new InvalidOperationException("SurroundStereoProvider expects mono input.");
                if (_surround.WaveFormat.Channels != 1)
                    throw new InvalidOperationException("SurroundStereoProvider expects mono surround control input.");

                int frameCount = count / 2;
                if (_monoBuffer.Length < frameCount)
                    _monoBuffer = new float[frameCount];
                if (_surroundBuffer.Length < frameCount)
                    _surroundBuffer = new float[frameCount];

                int monoRead = _mono.Read(_monoBuffer, 0, frameCount);
                int surroundRead = _surround.Read(_surroundBuffer, 0, frameCount);

                int read = Math.Max(monoRead, surroundRead);
                for (int i = 0; i < read; i++)
                {
                    float dry = (i < monoRead) ? _monoBuffer[i] : 0f;
                    float surroundIn = (i < surroundRead) ? _surroundBuffer[i] : 0f;

                    float delayedShort = _shortDelayBuffer[_shortDelayIndex];
                    float delayedLong = _longDelayBuffer[_longDelayIndex];

                    _shortDelayBuffer[_shortDelayIndex] = surroundIn + delayedShort * 0.16f;
                    _longDelayBuffer[_longDelayIndex] = surroundIn + delayedLong * 0.24f;

                    _shortDelayIndex = (_shortDelayIndex + 1) % _shortDelayBuffer.Length;
                    _longDelayIndex = (_longDelayIndex + 1) % _longDelayBuffer.Length;

                    float sideL = surroundIn * 0.42f + delayedLong * 0.30f - delayedShort * 0.15f;
                    float sideR = -surroundIn * 0.42f + delayedShort * 0.30f - delayedLong * 0.15f;

                    float left = ClampSample(dry * 0.90f + sideL * 0.72f);
                    float right = ClampSample(dry * 0.90f + sideR * 0.72f);

                    int outputIndex = offset + i * 2;
                    buffer[outputIndex] = left;
                    buffer[outputIndex + 1] = right;
                }

                return read * 2;
            }
        }

        // -------- TRAIN SOUND --------------
        public static double CalculateHarmonicSounds(Domain control, List<HarmonicData> harmonics)
        {
            double sound = 0;
            for (int harmonic = 0; harmonic < harmonics.Count; harmonic++)
            {
                HarmonicData harmonic_data = harmonics[harmonic];
                var amplitude_data = harmonic_data.Amplitude;

                if (harmonic_data.Range.Start > control.GetBaseWaveFrequency()) continue;
                if (harmonic_data.Range.End >= 0 && harmonic_data.Range.End < control.GetBaseWaveFrequency()) continue;

                double harmonic_freq = harmonic_data.Harmonic * control.GetBaseWaveFrequency();

                if (harmonic_data.Disappear != -1 && harmonic_freq > harmonic_data.Disappear) continue;
                double t = control.GetBaseWaveTime() * control.GetBaseWaveAngleFrequency();
                double sine_val;

                double amplitude = amplitude_data.StartValue + (amplitude_data.EndValue - amplitude_data.StartValue) / (amplitude_data.End * 2 - harmonic_data.Amplitude.Start * 2) * (control.GetBaseWaveFrequency() * 2 - harmonic_data.Amplitude.Start * 2);
                if (amplitude > amplitude_data.MaximumValue) amplitude = amplitude_data.MaximumValue;
                if (amplitude < amplitude_data.MinimumValue) amplitude = amplitude_data.MinimumValue;

                if (control.GetBaseWaveFrequency() > 0)
                {
                    if (harmonic_data.Harmonic >= 1 && harmonic_data.Harmonic < 3)
                        sine_val = (control.GetAmp() * 100 + 100) * 0.005 * 1 * GetToothHarmonic(t * harmonic_data.Harmonic, 9);
                    else if (harmonic_data.Harmonic >= 3 && harmonic_data.Harmonic < 14.5)
                        sine_val = (control.GetAmp() * 100 * 0.001 + 0.03) * 0.3 * GetToothHarmonic(t * harmonic_data.Harmonic, 3);
                    else if (harmonic_data.Harmonic == 29 || harmonic_data.Harmonic == 36)
                    {
                        double random_harmonic = harmonic_data.Harmonic + GetAirplaneRandomOffset(control.GetTime(), -0.5, 0.5, 0.0012);
                        sine_val = (0.04 * GetTooth(t * random_harmonic) + 0.04 * GetTooth(t * harmonic_data.Harmonic)) * 0.7 * (control.GetAmp()+0.3);
                    }
                    else if (harmonic_data.Harmonic == 72 || harmonic_data.Harmonic == 99)
                        sine_val = (control.GetAmp() * 100 * 0.002 + 0.01) * 5 * Functions.Sine(t * harmonic_data.Harmonic);
                    else sine_val = (control.GetAmp() * 100 * 0.002 + 0.01) * 5 * GetToothHarmonic(t * harmonic_data.Harmonic, 2);
                }
                else sine_val = Functions.Sine(t * harmonic_data.Harmonic);

                double amplitude_disappear = (harmonic_freq + 0.2 * harmonic_data.Disappear > harmonic_data.Disappear) ?
                    ((harmonic_data.Disappear - harmonic_freq) / 0.2 / harmonic_data.Disappear) : 1;

                sine_val *= amplitude * (harmonic_data.Disappear == -1 ? 1 : amplitude_disappear);
                sound += sine_val;
            }
            return sound;
        }

        public static double GetTooth(double x)
        {
            return Math.Asin(Math.Sin(0.25 * x % M_PI_2 - M_PI_4));
        }

        public static double GetToothHarmonic(double x, int num_harmonics)
        {
            double result = 0.0;
            for (int n = 1; n <= num_harmonics; n++)
            {
                double amplitude = (2.0 / M_PI) * Math.Pow(-1, n + 1) / n;
                result += amplitude * Math.Sin(n * x);
            }
            return result;
        }

        public static double GetAirplaneRandomOffset(double currentTime, double min, double max, double interval)
        {
            if (interval <= 0)
                return min;

            long intervalIndex = (long)Math.Floor(currentTime / interval);
            int seed = HashCode.Combine(intervalIndex, min, max, interval);
            Random random = new(seed);
            return min + (max - min) * random.NextDouble();
        }

        public static double GetSquare(double x, double duty_cycle)
        {
            double remainder = x % M_2PI;
            if (remainder < 0) remainder += 1.0;

            const double epsilon = 1e-10;
            if (remainder < duty_cycle - epsilon) return 1;
            else return 0;
        }

        public static double GetPeakFilter(double x, double freq, double Q, double peak)
        {
            double amp = -(Math.Pow(x - freq, 2) / freq);
            double quality = Math.Pow(Q, amp);
            double final = peak * (quality + 1 / peak);
            return final;
        }

        public static TrainSoundResult CalculateTrainSound(Domain Control, Data.TrainAudio.Struct Data)
        {
            Vvvf.Calculation.Common.CalculatePhsaseState(Control, 0);

            //PwmCalculateValues calculated_Values = YamlVvvfWave.CalculateYaml(Control, sound_data);
            PhaseState value = Vvvf.Calculation.Common.CalculatePhsaseState(Control, 0);
            Random rnd = new();

            double motorPwmSound = (Control.Motor.Parameter.DiffTe + Control.Motor.Parameter.DiffIdq0[1]/15.0) * Math.Pow(10, Data.MotorVolumeDb);
            double motorSound = CalculateHarmonicSounds(Control, Data.HarmonicSound);
            double gearSound = CalculateHarmonicSounds(Control, Data.GearSound);

            double rawInv = value.U - value.V;
            double invAlpha = 0.8;
            double invPrev = Control.GetInvLpfPrev();
            invPrev = invAlpha * invPrev + (1.0 - invAlpha) * rawInv;
            Control.SetInvLpfPrev(invPrev);
            double inv = invPrev;
            double invamp = Control.GetBaseWaveFrequency() < 80 ? GetChangingValue_Sin(2, 0.01, 30, 0.03, Control.GetBaseWaveFrequency(), true) : GetChangingValue(60, 0.03, 110, 0.005, Control.GetBaseWaveFrequency(), true);

            double PWMamp1 = GetChangingValue_Pow(0, 0.5, 9, 0.1, 0.8, Control.GetBaseWaveFrequency(), true);
            double PWMamp2 = GetChangingValue_Pow(40, 0.5, 92, 1, 2, Design2.Model.Voltage, true);
            double PWMamp3 = GetChangingValue_Sin(1, 2, 20, 1, Design2.Model.Voltage, true);
            double PWMamp4 = GetChangingValue_Sin(0, 0.04, 1, 1, Design2.Model.Voltage, true);
            double PWMamp = PWMamp1 * 0.6 * PWMamp3 * PWMamp4;

            double rawWhite = 0.4 * Functions.Sine(Control.GetTime() * M_2PI * rnd.NextDouble());
            double whiteAlpha = GetChangingValue_Sin(30, 0.87, 100, 0.72, Control.GetBaseWaveFrequency(), true); 

            double[] wPrev = Control.GetWhiteNoiseLpfPrev();
            wPrev[0] = 0.93 * wPrev[0] + (1.0 - 0.93) * rawWhite;
            wPrev[1] = whiteAlpha * wPrev[1] + (1.0 - whiteAlpha) * wPrev[0];
            double white = wPrev[1];

            double whiteamp = Control.GetBaseWaveFrequency() / 50 + 0.2 < 2 ? 2 : Control.GetBaseWaveFrequency() / 50 + 0.2;
            double whitefamp = whiteamp > 7 ? 7 : whiteamp;
            double t = Control.GetBaseWaveTime() * Control.GetBaseWaveAngleFrequency();
            double jointamp = (Control.GetBaseWaveAngleFrequency() / M_2PI) / 70 + 0.3 > 1 ? 1 : (Control.GetBaseWaveAngleFrequency() / M_2PI) / 70 + 0.3;
            double a = 0.008;
            double joint = Control.GetBaseWaveFrequency() >= 20 ? jointamp * 1.0 * (0.25 * GetTooth(t * a) + 0.25 * GetTooth(t * a - M_PI_4 * 8 / 9) + 0.25 * GetTooth(t * a - M_PI_4 * 3 * 8 / 9) + 0.25 * GetTooth(t * a - M_PI * 8 / 9)) : 0;
            double _300hz = 0.005 * GetToothHarmonic(Control.GetTime() * M_2PI * 60, 8) - 0.003 * GetToothHarmonic(Control.GetTime() * M_2PI * 60, 3);
            double sd = 0.4 * GetSquare(Control.GetTime() * M_2PI * 600, -12 * (Math.CopySign(Control.GetAmp(), Design2.Param.VoltageMultiply) + 0.5));
            double _sd = (Math.CopySign(Control.GetAmp(), Design2.Param.VoltageMultiply)) < -0.5 ? sd : 0;
            //double mtr = 0.2 * GetSquare(Control.GetTime() * M_2PI * 260, M_2PI * (Math.Abs(Design2.Param.VoltageMultiply)-0.1));

            double motorCurrent = Math.Sqrt(Control.Motor.Parameter.Idq0[0] * Control.Motor.Parameter.Idq0[0] + Control.Motor.Parameter.Idq0[1] * Control.Motor.Parameter.Idq0[1]);
            double currentNorm = Math.Tanh(motorCurrent * 0.12);
            double torqueNorm = Math.Tanh(Math.Abs(Control.Motor.Parameter.Te) * 0.08);
            double slipNorm = Math.Min(1.0, Math.Abs(Control.Motor.Parameter.Ωsl) / (Math.Abs(Control.Motor.Parameter.Ωe) + 1e-6));
            double surroundDepth = 0.5 + 0.35 * currentNorm + 0.24 * torqueNorm + 0.24 * slipNorm;

            double mainSignal = (motorPwmSound * PWMamp * 1.5 + inv * 0.3 * invamp + 4 * (motorSound + gearSound) + jointamp * 0.3 * joint) * Math.Pow(10, Data.TotalVolumeDb);
            double whiteSignal = (whitefamp * 0.3 * white + _sd*0) * Math.Pow(10, Data.TotalVolumeDb);
            // apply a simple first-order low-pass filter to smooth the surround signal
            double rawSurround = (inv * invamp * 30 + (motorSound + gearSound) * 15 + white * 0.04 + motorPwmSound * PWMamp2 * 3 + jointamp * 6 * joint) * surroundDepth;
            double surroundAlpha = 0.97; // tuning: closer to 1 = smoother
            _surroundLpfPrev = surroundAlpha * _surroundLpfPrev + (1.0 - surroundAlpha) * rawSurround;
            double surroundSignal = _surroundLpfPrev * Math.Pow(10, Data.TotalVolumeDb);
            //double signal = (1 * inv + 1 * motorPwmSound + 0 * gearSound) * Math.Pow(10, Data.TotalVolumeDb);

            return new TrainSoundResult(mainSignal, whiteSignal, surroundSignal);
            //return new TrainSoundResult(0, signal, 0);
        }


        public static void ExportWavFile(GenerationParameter Parameter, int SamplingFrequency, bool raw, string path)
        {
            static void AddSample(float value, BufferedWaveProvider provider)
            {
                byte[] soundSample = BitConverter.GetBytes((float)value);
                if (!BitConverter.IsLittleEndian) Array.Reverse(soundSample);
                provider.AddSamples(soundSample, 0, 4);
            }

            static void Write(BufferedWaveProvider bufferedWaveProvider, ISampleProvider sampleProvider, WaveFileWriter writer)
            {
                int sourceBytes = bufferedWaveProvider.BufferedBytes;
                if (sourceBytes <= 0) return;

                int inputSampleCount = sourceBytes / sizeof(float);
                int outputSampleCount = inputSampleCount * sampleProvider.WaveFormat.Channels / bufferedWaveProvider.WaveFormat.Channels;
                int outputBytes = outputSampleCount * sizeof(float);

                byte[] buffer = new byte[outputBytes];
                int bytesRead = sampleProvider.ToWaveProvider().Read(buffer, 0, outputBytes);
                writer.Write(buffer, 0, bytesRead);
            }

            static void DownSample(int NewSamplingRate, string InputPath, string OutputPath, bool DeleteOld)
            {
                using (var reader = new AudioFileReader(InputPath))
                {
                    var resampler = new WdlResamplingSampleProvider(reader, NewSamplingRate);
                    WaveFileWriter.CreateWaveFile16(OutputPath, resampler);
                }

                if (DeleteOld) File.Delete(InputPath);
            }

            Data.Vvvf.Struct vvvfData = Parameter.VvvfData;
            Data.TrainAudio.Struct soundData = Parameter.TrainData;
            StructCompiled baseFreqData = Parameter.BaseFrequencyData;
            GUI.TaskViewer.TaskProgress progressData = Parameter.Progress;

            Domain Domain = new(soundData.MotorSpec);

            int DownSampledFrequency = 60000;
            string pathTemp = Path.GetDirectoryName(path) + "\\" + "temp-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".wav";

            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SamplingFrequency, 1);
            var bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
            {
                BufferLength = 4096
            };
            var whiteWaveProvider = new BufferedWaveProvider(waveFormat)
            {
                BufferLength = 4096
            };
            var surroundWaveProvider = new BufferedWaveProvider(waveFormat)
            {
                BufferLength = 4096
            };

            ISampleProvider sampleProvider = bufferedWaveProvider.ToSampleProvider();
            if (soundData.UseFilters) sampleProvider = new MonauralFilter(sampleProvider, soundData.GetFilteres(SamplingFrequency));
            if (soundData.UseConvolutionFilter) sampleProvider = new CppConvolutionFilter(sampleProvider, 4096, soundData.GetImpulseResponse(SamplingFrequency));

            ISampleProvider finalSampleProvider = new WhiteNoiseMixer(sampleProvider, whiteWaveProvider.ToSampleProvider());
            ISampleProvider surroundSampleProvider = new SurroundStereoProvider(finalSampleProvider, surroundWaveProvider.ToSampleProvider());
            WaveFileWriter writer = new(raw ? path : pathTemp, surroundSampleProvider.WaveFormat);

            progressData.Total = baseFreqData.GetEstimatedSteps(1.0 / SamplingFrequency) + (raw ? 0 : 100);
            while (true)
            {
                Data.Vvvf.Analyze.Calculate(Domain, vvvfData);
                TrainSoundResult result = CalculateTrainSound(Domain, soundData);
                AddSample((float)result.Main, bufferedWaveProvider);
                AddSample((float)result.White, whiteWaveProvider);
                AddSample((float)result.Surround, surroundWaveProvider);

                if (bufferedWaveProvider.BufferedBytes == bufferedWaveProvider.BufferLength)
                    Write(bufferedWaveProvider, surroundSampleProvider, writer);
                progressData.Progress++;

                bool flag_continue = Data.BaseFrequency.Analyze.CheckForFreqChange(Domain, baseFreqData, vvvfData, 1.0 / SamplingFrequency);
                bool flag_cancel = progressData.Cancel;
                if (!flag_continue || flag_cancel) break;
            }

            if (bufferedWaveProvider.BufferedBytes > 0)
                Write(bufferedWaveProvider, surroundSampleProvider, writer);

            writer.Close();

            if (!raw)
            {
                DownSample(DownSampledFrequency, pathTemp, path, true);
                progressData.Progress += 100;
            }
        }
    }
}
