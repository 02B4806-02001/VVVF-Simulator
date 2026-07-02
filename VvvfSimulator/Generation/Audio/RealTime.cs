using System;
using System.IO.Ports;
using static System.Math;
using static VvvfSimulator.Vvvf.Model.Struct;
using static VvvfSimulator.Vvvf.MyMath;
using static VvvfSimulator.GUI.Simulator.RealTime.Controller.Design2.Design2;
using System.Configuration;

namespace VvvfSimulator.Generation.Audio
{
    public class RealTime
    {
        // ---------- COMMON ---------------
        public class Parameter(Data.Vvvf.Struct VvvfSound, Data.TrainAudio.Struct TrainSound)
        {
            public double FrequencyChangeRate { get; set; } = 0;
            public double SmoothedFrequencyChangeRate { get; set; } = 0;
            public double VoltageMultiply { get; set; } = 0;
            public double Delta_Amp { get; set; } = 0;
            public double SmoothedDeltaAmp { get; set; } = 0; // low-pass filtered delta amp
            public double DeltaAmpFilterK { get; set; } = 12.0; // smoothing constant (s^-1)
            // Smoothed magnitude for voltage multiply (to avoid abrupt jumps from slider)
            public double SmoothedVoltageMag { get; set; } = double.NaN;
            // Current sign for smoothed voltage; flipping is controlled to avoid passing through zero
            public int VoltageSign { get; set; } = 1;
            public double DAmp_dt { get; set; } = 0;
            public double FreeRunDt { get; set; } = 0;
            public double Amplitude { get; set; } = 1;
            // Second-order amplitude dynamics to simulate inertia/overshoot/damped oscillation
            public double AmpPosition { get; set; } = 0;
            public double AmpVelocity { get; set; } = 0;
            public bool AmpInitialized { get; set; } = false;
            // Physical parameters (tuneable)
            public double AmpMass { get; set; } = 1.0; // m
            // stiffness k = (2*pi*fn)^2 * m, default fn ~ 6 Hz -> k ~ 1420
            // lower stiffness for slower oscillation by default (gentler)
            public double AmpStiffness { get; set; } = 700.0; // k
            // increase damping ratio toward critical to reduce oscillation amplitude
            public double AmpDampingRatio { get; set; } = 0.3; // zeta (near-critical)
            public double PulseWithTime { get; set; } = 0;
            public bool IsBraking { get; set; } = false;
            public bool Quit { get; set; } = false;
            public bool IsFreeRunning { get; set; } = false;
            public Domain Control { get; } = new(TrainSound.MotorSpec);
            public Data.Vvvf.Struct VvvfSoundData { get; } = VvvfSound;
            public Data.TrainAudio.Struct TrainSoundData { get; } = TrainSound;
            public string AudioDeviceId { get; set; } = new NAudio.CoreAudioApi.MMDeviceEnumerator().GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia).ID;

            // Slip coupling: base frequency change rate gets k * amp change rate
            // Low-pass filter for real-time audio amplitude (IIR)
            public double AudioAmpLpfPrev { get; set; } = 0.0;
            public double AudioAmpLpfAlpha { get; set; } = 0.7; // 0..1, higher = smoother

            public double PreviousModelVoltage { get; set; } = double.NaN;
            public double ModelVoltageRate { get; set; } = 0;
            public double SmoothedModelVoltageRate { get; set; } = 0;

            // Added for Model.Voltage low-pass filter
            public double SmoothedModelVoltage { get; set; } = double.NaN;
            public double ModelVoltageLpfAlpha { get; set; } = 0.8; // 0..1, closer to 1 = slower/smoother
        }
        public class VvvfSoundParameter(Data.Vvvf.Struct VvvfSound, Data.TrainAudio.Struct TrainSound) : Parameter(VvvfSound, TrainSound)
        {
            public Mode OutputMode { get; set; } = Mode.Line;
            public SerialPort? Port { get; set; }
            public enum Mode
            {
                Line, Phase, PhaseCurrent, Unbalanced_Overlap, Balanced_Overlap_L, Balanced_Overlap_R
            }
        }
        public class TrainSoundParameter(Data.Vvvf.Struct VvvfSound, Data.TrainAudio.Struct TrainSound) : Parameter(VvvfSound, TrainSound)
        {
        }

        public static double GetChangingValue(double x1, double y1, double x2, double y2, double x, bool limit)
        {
            double final = y1 + (y2 - y1) / (x2 - x1) * (x - x1);
            if (limit == true)
            {
                if (y1 < y2) return final < y1 ? y1 : final > y2 ? y2 : final;
                else if (y1 == y2) return y1;
                else return final > y1 ? y1 : final < y2 ? y2 : final;
            }
            else return final;
        }

        public static double GetChangingValue_Pow(double x1, double y1, double x2, double y2, double a, double x, bool limit)
        {
            double final = y1 + (y2 - y1) * Pow((x - x1) / (x2 - x1), a);
            if (limit == true)
            {
                if (x < x1) return y1;
                else if (x > x2) return y2;
                else return final;
            }
            else return final;
        }

        public static double GetChangingValue_Sin(double x1, double y1, double x2, double y2, double x, bool limit)
        {
            double final = -0.5 * ((y2 - y1) * Cos(M_PI * (x - x1) / (x2 - x1)) - (y1 + y2));
            if (limit == true)
            {
                if (x < x1) return y1;
                else if (x > x2) return y2;
                else return final;
            }
            else return final;
        }

        public static int RealTimeFrequencyControl(Domain Control, Parameter Param, double dt)
        {
            Control.SetBraking(Param.IsBraking);
            Control.SetVoltageMultiply(Param.VoltageMultiply);
            // Param.IsFreeRunning represents free-run (coasting) state, not power-off.
            // Use SetFreeRun so Domain.ProcessControlParameter handles free-run correctly.
            Control.SetFreeRun(Param.IsFreeRunning);
            double sin_freq = Control.GetBaseWaveFrequency();

            // Low-pass filter for Model.Voltage to prevent infinite change rates
            if (double.IsNaN(Param.SmoothedModelVoltage))
                Param.SmoothedModelVoltage = Model.Voltage;
            else
            {
                double voltAlpha = Param.ModelVoltageLpfAlpha;
                Param.SmoothedModelVoltage = voltAlpha * Param.SmoothedModelVoltage + (1.0 - voltAlpha) * Model.Voltage;
            }
            double filteredVoltage = Param.SmoothedModelVoltage;

            if (double.IsNaN(Param.PreviousModelVoltage))
                Param.PreviousModelVoltage = filteredVoltage;
            Param.ModelVoltageRate = (filteredVoltage - Param.PreviousModelVoltage) / dt;
            Param.PreviousModelVoltage = filteredVoltage;
            
            // Remove the Pow scaling from the smoothing logic too, to ensure exact zero-integral 
            // over a closed loop change in Model.Voltage
            double voltageDelta = Param.ModelVoltageRate - Param.SmoothedModelVoltageRate;
            Param.SmoothedModelVoltageRate += voltageDelta * 12.0 * dt;

            // Smooth VoltageMutiply magnitude from UI to avoid abrupt amplitude jumps.
            // Maintain sign separately so the magnitude smoothing does not pass through zero when
            // the user flips the handle from positive to negative.
            double targetMag = Abs(Param.VoltageMultiply);
            int rawSign = Param.VoltageMultiply > 0 ? 1 : (Param.VoltageMultiply < 0 ? -1 : 0);
            if (double.IsNaN(Param.SmoothedVoltageMag))
            {
                Param.SmoothedVoltageMag = targetMag;
                Param.VoltageSign = rawSign == 0 ? (Param.VoltageSign == 0 ? 1 : Param.VoltageSign) : rawSign;
            }
            else
            {
                // If user flipped the handle sign, switch sign immediately and set magnitude
                // to target so the value jumps from e.g. +0.7 to -0.6 without passing through zero.
                Param.SmoothedVoltageMag = targetMag;
                if (rawSign != 0 && rawSign != Param.VoltageSign) Param.VoltageSign = rawSign;
            }
            // compute signed smoothed value for sign-aware use
            double signedSmoothedVoltage = Param.VoltageSign * Param.SmoothedVoltageMag;
            // store signed magnitude to preserve the sign
            Param.VoltageMultiply = signedSmoothedVoltage;

            // use second-order mass-spring-damper to simulate amplitude inertia, overshoot and damping
            double currentAmp = Control.GetAmp();
            // amplitude state is non-negative in this model; use magnitude as target
            double targetAmp = Param.SmoothedVoltageMag;

            // initialize state on first use
            if (!Param.AmpInitialized)
            {
                Param.AmpPosition = currentAmp;
                Param.AmpVelocity = 0;
                Param.AmpInitialized = true;
            }

            // physical parameters
            double m = Param.AmpMass; // mass
            double k = Param.AmpStiffness; // stiffness
            double zeta = Param.AmpDampingRatio; // damping ratio
            double c = 2.0 * zeta * Sqrt(k * m); // critical damping factor

            // compute force: spring towards target and viscous damping
            double x = Param.AmpPosition;
            double v = Param.AmpVelocity;
            double springForce = -k * (x - targetAmp);
            double dampingForce = -c * v;
            double accel = (springForce + dampingForce) / m;

            // integrate (semi-implicit Euler)
            v += accel * dt;
            x += v * dt;

            // enforce bounds
            if (x < 0) { x = 0; v = 0; }

            double new_amp = x;
            // keep an unlimited copy before applying modulation limits
            double new_amp_unlimited = new_amp;
            double limit = 65 / sin_freq / M_4_PI * 1.245 + 0.03;

            // reintroduce original delta-amplitude limiting behavior
            double? VideoSineAmp = Control.ElectricalState.BaseWaveAmplitude;
            double VideoSineAmplitude = VideoSineAmp ?? 0.0;
            double a1, a2;
            //a1 = GetChangingValue(0.5, 1.6, GetChangingValue_Sin(60,0.9,110,0.8,sin_freq,true), 0.4, Model.Voltage/100, true); //bj5/13
            //a1 = GetChangingValue_Pow(0.2, 0.9, 0.9, 0.17, 2, Model.Voltage/100, true); //yongji
            a1 = GetChangingValue_Pow(0, 10, 1.2, 0.8, 2, VideoSineAmplitude,true) * GetChangingValue_Sin(0, 8, 0.5, 1, VideoSineAmplitude, true); //A4
            //a1 = GetChangingValue_Pow(0.4, 4, 1.0, 0.5, 0.3, VideoSineAmplitude, true); //01A03
            //a1 = (VideoSineAmplitude < 1 ? GetChangingValue_Pow(0, 1, 0.4, 2, 0.6, VideoSineAmplitude, true) : GetChangingValue_Pow(1, 2, 1.1, 1.5, 0.3, VideoSineAmplitude, true)) * GetChangingValue_Pow(0.4, 1, 0.8, 0.5, 0.6, VideoSineAmplitude, true); //N1000
            a2 = GetChangingValue_Pow(0.3, 0.6, 0.9, 1.0, 0.3, VideoSineAmplitude,true); //N1000    ⬆️N1000 
            //a2 = GetChangingValue_Sin(20, 1.4, 110, 0.9, sin_freq, true); //bj5/13
            //a1 = GetChangingValue_Pow(0, 2, 1.15, 1, 3, VideoSineAmplitude, true); //03A01
            //a1 = 1;
            //a2 = 1 * GetChangingValue_Pow(0.3, 1, 2, 4, 2, VideoSineAmplitude, true); //小以豆
            //a1 = 3;
            //a2 = 1.2;

            a1 *= GetChangingValue_Sin(0, 1.5, 130, 0.5, sin_freq, true);
            a2 *= GetChangingValue_Sin(0, 1.5, 130, 0.5, sin_freq, true);

            double appliedDelta;
            double rawDelta;
            double abs_delta_amp;
            rawDelta = new_amp - currentAmp;
            abs_delta_amp = ((60 * Pow(Abs(rawDelta), 4) + 85 * Pow(Abs(rawDelta), 2) + 8 * Abs(rawDelta)) * GetChangingValue_Sin(0, 8, 0.3, 1, VideoSineAmplitude, true)) * dt; //amp响应更快 例：西门子IGBT程序
            //abs_delta_amp = (4 * Pow(Abs(rawDelta), 1.6) + 5 * Pow(Abs(rawDelta), 0.9)) * dt; //amp变化更均匀 例：阿尔斯通Onix程序 01A03
            double delta_amp = rawDelta >= 0 ? abs_delta_amp : -abs_delta_amp;
            Param.DAmp_dt = delta_amp / (dt > 0 ? dt : 1e-6);

            if (delta_amp > a1 * dt) appliedDelta = a1 * dt;
            else if (delta_amp < -a2 * dt) appliedDelta = -a2 * dt;
            else appliedDelta = delta_amp;

            void ControlAmplitude()
            {
                if (Abs(Param.VoltageMultiply) < 0.001)
                {
                    appliedDelta += (new_amp > 0 ? -1 : 1) * 1 * dt;
                    new_amp = 0;
                }
            }
            ControlAmplitude();

            new_amp = currentAmp + appliedDelta;

            Random rnd = new();
            double randomRatio = rnd.NextDouble() * 0.08 + 0.12;
            double randomStiff = rnd.NextDouble() * 3000 + 1000;
            Param.AmpDampingRatio = GetChangingValue_Pow(1, randomRatio, 6, 0.55, 0.4, abs_delta_amp, true);
            Param.AmpStiffness = GetChangingValue_Pow(1, randomStiff, 12, 600, 0.4, abs_delta_amp, true) * 1;
            Param.AmpPosition = new_amp;
            Param.AmpVelocity = v;
            Param.Delta_Amp = appliedDelta;
            // low-pass filter delta_amp to reduce error/noise
            Param.SmoothedDeltaAmp += (Param.Delta_Amp - Param.SmoothedDeltaAmp) * Param.DeltaAmpFilterK * dt;
            // copy to limited_amp for modulation limits; can clamp here if needed
            double limited_amp = new_amp;
            limited_amp = Min(new_amp, 1.28 / GetChangingValue(0, 0.03, 60, 1.245, sin_freq, false)); //LIMIT LIMIT LIMIT

            { //SZ16Z
                double targetPulseWidth;

                if (sin_freq <= 20) targetPulseWidth = 1000.0;
                else if (sin_freq > 20 && sin_freq <= 60)
                {
                    targetPulseWidth = Param.PulseWithTime + (!Param.IsBraking ? -300 : -300) * dt;
                    if (targetPulseWidth > 800.0 && Param.IsBraking) targetPulseWidth = 800.0;
                    if (targetPulseWidth < 500.0) targetPulseWidth = 500.0;
                }
                else if (sin_freq > 60 && sin_freq <= 120)
                {
                    targetPulseWidth = Param.PulseWithTime + (!Param.IsBraking ? 270 : -270) * dt;
                    if (targetPulseWidth > 800.0 && !Param.IsBraking) targetPulseWidth = 800.0;
                    if (targetPulseWidth < 800.0 && Param.IsBraking) targetPulseWidth = 800.0;
                }
                else if (sin_freq > 120 && sin_freq <= 150)
                {
                    targetPulseWidth = Param.PulseWithTime + (!Param.IsBraking ? 270 : -270) * dt;
                    if (targetPulseWidth > 1000.0 && !Param.IsBraking) targetPulseWidth = 1000.0;
                    if (targetPulseWidth < 1000.0 && Param.IsBraking) targetPulseWidth = 1000.0;
                }
                else if (sin_freq > 150 && sin_freq <= 180)
                {
                    targetPulseWidth = Param.PulseWithTime + (!Param.IsBraking ? 270 : -270) * dt;
                    if (targetPulseWidth > 1200.0 && !Param.IsBraking) targetPulseWidth = 1200.0;
                    if (targetPulseWidth < 1200.0 && Param.IsBraking) targetPulseWidth = 1200.0;
                }
                else if (sin_freq > 180 && sin_freq <= 210)
                {
                    targetPulseWidth = Param.PulseWithTime + (!Param.IsBraking ? 270 : -270) * dt;
                    if (targetPulseWidth > 1400.0 && !Param.IsBraking) targetPulseWidth = 1400.0;
                    if (targetPulseWidth < 1400.0 && Param.IsBraking) targetPulseWidth = 1400.0;
                }
                else if (sin_freq > 210 && sin_freq <= 240)
                {
                    targetPulseWidth = Param.PulseWithTime + (!Param.IsBraking ? 270 : -270) * dt;
                    if (targetPulseWidth > 1600.0 && !Param.IsBraking) targetPulseWidth = 1600.0;
                    if (targetPulseWidth < 1600.0 && Param.IsBraking) targetPulseWidth = 1600.0;
                }
                else targetPulseWidth = 1600.0;

                Param.PulseWithTime = targetPulseWidth;
            }//SZ16Z

            // apply modulation index limit to a separate limited amplitude
            // (limited_amp already assigned earlier)
            if (limited_amp < sin_freq * 0.0001 && (targetAmp - currentAmp) < -0.01) limited_amp = 0;

            // Simplified linear mapping with first-order smoothing:
            // Note: `Param.FrequencyChangeRate` provided by the controller is already
            // in angular-frequency change units (rad/s^2). Do not multiply by M_2_PI again.
            double targetFreqChange = Param.FrequencyChangeRate;
            double kFreqFilter = 3; // s^-1
            // discrete IIR: x += (target - x) * k * dt
            Param.SmoothedFrequencyChangeRate += (targetFreqChange - Param.SmoothedFrequencyChangeRate) * kFreqFilter * dt;
            double sin_new_angle_freq = Control.GetBaseWaveAngleFrequency() + Param.SmoothedFrequencyChangeRate * dt;

            if (Param.IsBraking && sin_freq < 5)
                sin_new_angle_freq += GetChangingValue_Pow(0, -0.94 * Param.SmoothedFrequencyChangeRate, 5, 0, 0.4, sin_freq, true) * dt;

            if (sin_new_angle_freq < 0) sin_new_angle_freq = 0;
            if (new_amp < 0.001) new_amp = 0;

            // compute simple first-order low-pass filtered amplitude for audio-only use
            double alpha = Param.AudioAmpLpfAlpha;
            double audioFilteredAmp = alpha * Param.AudioAmpLpfPrev + (1.0 - alpha) * limited_amp;
            Param.AudioAmpLpfPrev = audioFilteredAmp;

            void ControlFreq()
            {
                double final = sin_freq * Control.GetAmp();
                double k = 1;
                //Control.SetControlFrequency(final / k);
                Control.SetControlFrequency(sin_freq);
            }

            if (!Control.IsFreeRun())
            {
                if (Control.IsBaseWaveTimeChangeAllowed())
                {
                    if (sin_new_angle_freq != 0)
                    {
                        double amp = Control.GetBaseWaveAngleFrequency() / sin_new_angle_freq;
                        Control.MultiplyBaseWaveTime(amp);
                    }
                    else
                        Control.SetBaseWaveTime(0);
                }

                ControlFreq();
                Control.SetBaseWaveAngleFrequency(sin_new_angle_freq);
                // Use unfiltered limited_amp for control to preserve slip dynamics
                Control.SetAmp(limited_amp);
                Control.SetUnlimitedAmp(new_amp*0.5+0.5);
                double change_rate = Param.SmoothedFrequencyChangeRate + 0; //24
                Control.SetRealBaseWaveFrequency(Control.GetBaseWaveFrequency() - CopySign(Pow(Abs(change_rate), 0.3), change_rate) * 0.2);

                // Keep control amplitude unfiltered; audio can reference Param.AudioAmpLpfPrev
                Param.Amplitude = Param.FreeRunDt < -0.05 ? 0 : limited_amp;
                //Param.Amplitude = Abs(Param.VoltageMultiply) < 0.01 && delta_amp < 0.001 ? 0 : 1;
            }


            if (Param.Quit) return 0;

            //Control.ProcessControlParameter(dt, Param.VvvfSoundData.JerkSetting);

            return -1;
        }
    }
}