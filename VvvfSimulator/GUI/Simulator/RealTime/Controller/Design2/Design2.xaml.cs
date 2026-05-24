using System;
using System.Media;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VvvfSimulator.Generation;
using VvvfSimulator.GUI.Resource.Class;
using VvvfSimulator.GUI.Resource.Language;
using VvvfSimulator.GUI.Resource.Theme;
using VvvfSimulator.GUI.Simulator.RealTime.Setting;
using VvvfSimulator.GUI.Util;
using VvvfSimulator.Vvvf;
using static System.Math;
using static VvvfSimulator.Vvvf.MyMath;
using static VvvfSimulator.Generation.Audio.RealTime;
using static VvvfSimulator.Vvvf.Model.Struct;
using static VvvfSimulator.Data.Vvvf.Struct.PulseControl.Pulse;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Key = System.Windows.Input.Key;

namespace VvvfSimulator.GUI.Simulator.RealTime.Controller.Design2
{
    /// <summary>
    /// Mascon.xaml の相互作用ロジック
    /// </summary>
    public partial class Design2 : Window, IController
    {
        public static Parameter Param;
        public static readonly ViewModel Model = new();
        private int CalculatePrecision = 10000;
        public int MasconPosition = 0;
        public DeviceMode CurrentMode = DeviceMode.KeyBoard;
        public string MasconComPort = "";
        public SerialPort serialPort = new();
        public partial class ViewModel : ViewModelBase
        {
            private Brush _FB = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush FB { get { return _FB; } set { _FB = value; RaisePropertyChanged(nameof(FB)); } }

            private Brush _B14 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B14 { get { return _B14; } set { _B14 = value; RaisePropertyChanged(nameof(B14)); } }

            private Brush _B13 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B13 { get { return _B13; } set { _B13 = value; RaisePropertyChanged(nameof(B13)); } }

            private Brush _B12 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B12 { get { return _B12; } set { _B12 = value; RaisePropertyChanged(nameof(B12)); } }

            private Brush _B11 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B11 { get { return _B11; } set { _B11 = value; RaisePropertyChanged(nameof(B11)); } }

            private Brush _B10 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B10 { get { return _B10; } set { _B10 = value; RaisePropertyChanged(nameof(B10)); } }

            private Brush _B9 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B9 { get { return _B9; } set { _B9 = value; RaisePropertyChanged(nameof(B9)); } }

            private Brush _B8 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B8 { get { return _B8; } set { _B8 = value; RaisePropertyChanged(nameof(B8)); } }

            private Brush _B7 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B7 { get { return _B7; } set { _B7 = value; RaisePropertyChanged(nameof(B7)); } }

            private Brush _B6 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B6 { get { return _B6; } set { _B6 = value; RaisePropertyChanged(nameof(B6)); } }

            private Brush _B5 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B5 { get { return _B5; } set { _B5 = value; RaisePropertyChanged(nameof(B5)); } }

            private Brush _B4 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B4 { get { return _B4; } set { _B4 = value; RaisePropertyChanged(nameof(B4)); } }


            private Brush _B3 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B3 { get { return _B3; } set { _B3 = value; RaisePropertyChanged(nameof(B3)); } }


            private Brush _B2 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B2 { get { return _B2; } set { _B2 = value; RaisePropertyChanged(nameof(B2)); } }


            private Brush _B1 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B1 { get { return _B1; } set { _B1 = value; RaisePropertyChanged(nameof(B1)); } }

            private Brush _B0 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B0 { get { return _B0; } set { _B0 = value; RaisePropertyChanged(nameof(B0)); } }


            private Brush _N = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush N { get { return _N; } set { _N = value; RaisePropertyChanged(nameof(N)); } }

            private Brush _P0 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P0 { get { return _P0; } set { _P0 = value; RaisePropertyChanged(nameof(P0)); } }

            private Brush _P1 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P1 { get { return _P1; } set { _P1 = value; RaisePropertyChanged(nameof(P1)); } }

            private Brush _P2 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P2 { get { return _P2; } set { _P2 = value; RaisePropertyChanged(nameof(P2)); } }

            private Brush _P3 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P3 { get { return _P3; } set { _P3 = value; RaisePropertyChanged(nameof(P3)); } }

            private Brush _P4 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P4 { get { return _P4; } set { _P4 = value; RaisePropertyChanged(nameof(P4)); } }

            private Brush _P5 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P5 { get { return _P5; } set { _P5 = value; RaisePropertyChanged(nameof(P5)); } }

            private Brush _P6 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P6 { get { return _P6; } set { _P6 = value; RaisePropertyChanged(nameof(P6)); } }

            private Brush _P7 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P7 { get { return _P7; } set { _P7 = value; RaisePropertyChanged(nameof(P7)); } }

            private Brush _P8 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P8 { get { return _P8; } set { _P8 = value; RaisePropertyChanged(nameof(P8)); } }


            static double ChangeRate = 5;

            private double _MasconP8 = 0;
            public double MasconP8 { get { return _MasconP8; } set { _MasconP8 = value; RaisePropertyChanged(nameof(MasconP8)); } }

            private double _MasconP7 = 0;
            public double MasconP7 { get { return _MasconP7; } set { _MasconP7 = value; RaisePropertyChanged(nameof(MasconP7)); } }

            private double _MasconP6 = 0;
            public double MasconP6 { get { return _MasconP6; } set { _MasconP6 = value; RaisePropertyChanged(nameof(MasconP6)); } }

            private double _MasconP5 = 0;
            public double MasconP5 { get { return _MasconP5; } set { _MasconP5 = value; RaisePropertyChanged(nameof(MasconP5)); } }

            private double _MasconP4 = 0;
            public double MasconP4 { get { return _MasconP4; } set { _MasconP4 = value; RaisePropertyChanged(nameof(MasconP4)); } }

            private double _MasconP3 = 0;
            public double MasconP3 { get { return _MasconP3; } set { _MasconP3 = value; RaisePropertyChanged(nameof(MasconP3)); } }

            private double _MasconP2 = 0;
            public double MasconP2 { get { return _MasconP2; } set { _MasconP2 = value; RaisePropertyChanged(nameof(MasconP2)); } }

            private double _MasconP1 = 0;
            public double MasconP1 { get { return _MasconP1; } set { _MasconP1 = value; RaisePropertyChanged(nameof(MasconP1)); } }

            private double _MasconP0 = 0;
            public double MasconP0 { get { return _MasconP0; } set { _MasconP0 = value; RaisePropertyChanged(nameof(MasconP0)); } }

            private double _MasconFB = 0;
            public double MasconFB { get { return _MasconFB; } set { _MasconFB = value; RaisePropertyChanged(nameof(MasconFB)); } }

            private double _MasconB10 = 0;
            public double MasconB10 { get { return _MasconB10; } set { _MasconB10 = value; RaisePropertyChanged(nameof(MasconB10)); } }

            private double _MasconB9 = 0;
            public double MasconB9 { get { return _MasconB9; } set { _MasconB9 = value; RaisePropertyChanged(nameof(MasconB9)); } }

            private double _MasconB8 = 0;
            public double MasconB8 { get { return _MasconB8; } set { _MasconB8 = value; RaisePropertyChanged(nameof(MasconB8)); } }

            private double _MasconB7 = 0;
            public double MasconB7 { get { return _MasconB7; } set { _MasconB7 = value; RaisePropertyChanged(nameof(MasconB7)); } }

            private double _MasconB6 = 0;
            public double MasconB6 { get { return _MasconB6; } set { _MasconB6 = value; RaisePropertyChanged(nameof(MasconB6)); } }

            private double _MasconB5 = 0;
            public double MasconB5 { get { return _MasconB5; } set { _MasconB5 = value; RaisePropertyChanged(nameof(MasconB5)); } }

            private double _MasconB4 = 0;
            public double MasconB4 { get { return _MasconB4; } set { _MasconB4 = value; RaisePropertyChanged(nameof(MasconB4)); } }

            private double _MasconB3 = 0;
            public double MasconB3 { get { return _MasconB3; } set { _MasconB3 = value; RaisePropertyChanged(nameof(MasconB3)); } }

            private double _MasconB2 = 0;
            public double MasconB2 { get { return _MasconB2; } set { _MasconB2 = value; RaisePropertyChanged(nameof(MasconB2)); } }

            private double _MasconB1 = 0;
            public double MasconB1 { get { return _MasconB1; } set { _MasconB1 = value; RaisePropertyChanged(nameof(MasconB1)); } }

            private double _MasconB0 = 0;
            public double MasconB0 { get { return _MasconB0; } set { _MasconB0 = value; RaisePropertyChanged(nameof(MasconB0)); } }



			private double _SineFrequency = 0;
			public double SineFrequency { get { return _SineFrequency; } set { _SineFrequency = value; RaisePropertyChanged(nameof(SineFrequency)); } }

			private double _Speed = 0;
            public double Speed { get { return _Speed; } set { _Speed = value; RaisePropertyChanged(nameof(Speed)); } }

            private double _SmoothedSpeed = 0;
            public double SmoothedSpeed { get { return _SmoothedSpeed; } set { _SmoothedSpeed = value; RaisePropertyChanged(nameof(SmoothedSpeed)); } }

            private double _Accelerate = 0;
            public double Accelerate { get { return _Accelerate; } set { _Accelerate = value; RaisePropertyChanged(nameof(Accelerate)); } }

            private string _Ratio = "";
            public string Ratio { get { return _Ratio; } set { _Ratio = value; RaisePropertyChanged(nameof(Ratio)); } }

            private double _Deceleration = 0;
            public double Deceleration { get { return _Deceleration; } set { _Deceleration = value; RaisePropertyChanged(nameof(Deceleration)); } }

            private double _MotorRPM = 0;
            public double MotorRPM { get { return _MotorRPM; } set { _MotorRPM = value; RaisePropertyChanged(nameof(MotorRPM)); } }

            private double _Distance = 0;
            public double Distance { get { return _Distance; } set { _Distance = value; RaisePropertyChanged(nameof(Distance)); } }

            private string _Gear = "";
            public string Gear { get { return _Gear; } set { _Gear = value; RaisePropertyChanged(nameof(Gear)); } }

            private string _Amplitude = "";
            public string Amplitude { get { return _Amplitude; } set { _Amplitude = value; RaisePropertyChanged(nameof(Amplitude)); } }

			private double _Voltage = 0;
			public double Voltage { get { return _Voltage; } set { _Voltage = value; RaisePropertyChanged(nameof(Voltage)); } }

			private string _PulseState = "";
            public string PulseState { get { return _PulseState; } set { _PulseState = value; RaisePropertyChanged(nameof(PulseState)); } }

            //private double _SetAcceleration = 0;
            //public double SetAcceleration { get { return _SetAcceleration; } set { _SetAcceleration = value; RaisePropertyChanged(nameof(SetAcceleration)); } }

            //private double _SetDeceleration = 0;
            //public double SetDeceleration { get { return _SetDeceleration; } set { _SetDeceleration = value; RaisePropertyChanged(nameof(SetDeceleration)); } }
        };
        public Design2(Parameter parameter)
        {
            Param = parameter;
			CalculatePrecision = (Param is VvvfSoundParameter) ? Properties.Settings.Default.RealTime_VVVF_Controller_Design2_CalculatePrecision :
				Properties.Settings.Default.RealTime_Train_Controller_Design2_CalculatePrecision;
            InitializeComponent();
            SetState(0);
            DataContext = Model;
            SetWindowContentVisibility();
        }

        public double GetSpeed(double SineFrequency, int motor_pole_pair, int Gear1, int Gear2, double WheelDiameter, double SlipRate)
        {
            double MotorRPM = SineFrequency * 60 / motor_pole_pair * 2;
            double WheelRPM = MotorRPM * Gear1 / Gear2;
            double C = WheelDiameter * Math.PI / 1000;
            const double conv = 0.06;
            double Speed = WheelRPM * C * conv * (100 - SlipRate) / 100; //slip rate (%)
            return Speed; //km/h
        }

        public void StartTask()
        {

            Task.Run(() =>
            {
                while (!Param.Quit)
                {
                    Domain Control = Param.Control.Clone();
                    Control.GetCarrierInstance().UseSimpleFrequency = true;

                    Random rand = new();
                    Model.SineFrequency = Control.ElectricalState.BaseWaveFrequency;
                    PulseHarmonic harmonic = new PulseHarmonic();
                    double SineX = Control.GetBaseWaveAngleFrequency() * Control.GetBaseWaveTime() + harmonic.InitialPhase;

                    double freq = Control.GetBaseWaveFrequency();
                    double voltmutiply = Design2.Param.VoltageMultiply;
                    double a = (0.02214286 * freq + 0.02) / 100f;
                    double? amp = Control.ElectricalState.BaseWaveAmplitude;
                    double str1170 = freq / GetChangingValue(38, 120, 100, 150, freq, false);
                    double str1000 = freq / GetChangingValue(38, 96, 100, 120, freq, false);
                    double str15 = GetChangingValue_Pow(45, 1.06, 60, 0.8, 0.7, freq, true);
                    double str3 = voltmutiply > 0 ? 1.33 : 1.272;

                    if (Control.ElectricalState.IsNone)
                        Model.PulseState = LanguageManager.GetString("Simulator.RealTime.Controller.PulseState.None");
                    else
                    {
                        int PulseCount = Control.ElectricalState.PulsePattern.PulseMode.PulseCount;
                        Model.PulseState = Control.ElectricalState.PulsePattern.PulseMode.PulseType switch
                        {
                            PulseTypeName.ASYNC => Control.GetCarrierInstance().CalculateCarrierFrequency(Control.GetTime(), Control.ElectricalState).ToString("F0") + " Hz",
                            PulseTypeName.SYNC => PulseCount.ToString(),
                            PulseTypeName.CHM => Math.Abs(Param.VoltageMultiply) < 0.01 ? "Off" :
                                /*freq < 800 / 15d ?
                                    amp < 0.680 ? "CHM 15-1 H3" :
                                    amp < 0.914 ? "CHM 15-2 H2" :
                                    amp < 1.080 ? "CHM 15-3 H2" :
                                    amp < 1.119 ? "CHM 15-4 H3" :
                                    amp < 1.132 ? "CHM 15-4 H2" :
                                    amp < 1.174 ? "CHM 15-5 H2" :
                                    amp < 1.200 ? "CHM 15-6 H2" :
                                    amp < 1.230 ? "CHM 11-5 H1" :
                                    "CHM 7-3 H1" :
                                freq < 800 / 13d ?
                                    amp < 0.784 ? "CHM 13-1 H3" :
                                    amp < 1.007 ? "CHM 13-2 H2" :
                                    amp < 1.089 ? "CHM 13-3 H3" :
                                    amp < 1.113 ? "CHM 13-3 H2" :
                                    amp < 1.166 ? "CHM 13-4 H2" :
                                    amp < 1.200 ? "CHM 13-5 H2" :
                                    amp < 1.230 ? "CHM 9-4 H1" :
                                    "CHM 5-2 H1" :
                                freq < 800 / 11d ?
                                    amp < 0.850 ? "CHM 11-1 H3" :
                                    amp < 1.030 ? "CHM 11-2 H3" :
                                    amp < 1.085 ? "CHM 11-2 H2" :
                                    amp < 1.159 ? "CHM 11-3 H2" :
                                    amp < 1.200 ? "CHM 11-4 H2" :
                                    amp < 1.230 ? "CHM 7-3 H1" :
                                    "CHM 5-2 H1" :
                                freq < 800 / 9d - 0.1 && voltmutiply >= 0 ?
                                    amp < 0.750 ? "CHM 9-0 H2" :
                                    amp < 1.021 ? "CHM 9-1 H2" :
                                    amp < 1.143 ? "CHM 9-2 H2" :
                                    amp < 1.200 ? "CHM 9-3 H2" :
                                    amp < 1.230 ? "CHM 7-3 H1" :
                                    "CHM 5-2 H1" :
                                freq < 800 / 9d && voltmutiply >= 0 ?
                                    amp < 0.750 ? "CHM 9-0 H2" :
                                    amp < 1.021 ? "CHM 9-1 H2" :
                                    amp < 1.143 ? "CHM 9-2 H2" :
                                    amp < 1.200 ? "CHM 9-3 H2" :
                                    amp < 1.230 ? "CHM 7-3 H1" :
                                    amp < 1.300 ? "CHM 5-2 H1" :
                                    "CHM 3-1 H1" :
                                freq < 800 / 9d && voltmutiply < 0 ?
                                    amp < 0.750 ? "CHM 9-0 H2" :
                                    amp < 1.021 ? "CHM 9-1 H2" :
                                    amp < 1.143 ? "CHM 9-2 H2" :
                                    amp < 1.200 ? "CHM 9-3 H2" :
                                    amp < 1.230 ? "CHM 7-3 H1" :
                                    amp < 1.250 ? "CHM 5-2 H1" :
                                    "CHM 3-1 H1" :
                                freq < 800 / 7d ?
                                    amp < 0.840 ? "CHM 7-0 H3" :
                                    amp < 1.113 ? "CHM 7-1 H3" :
                                    amp < 1.200 ? "CHM 7-2 H3" :
                                    amp < 1.230 ? "CHM 7-3 H1" :
                                    amp < 1.280 ? "CHM 5-2 H1" :
                                    amp < 1.310 ? "CHM 3-1 H1" :
                                    "1" :
                                freq < 800 / 5d ?
                                    amp < 1.022 ? "CHM 5-0 H2" :
                                    amp < 1.200 ? "CHM 5-1 H2" :
                                    amp < 1.280 ? "CHM 5-2 H1" :
                                    amp < 1.310 ? "CHM 3-1 H1" :
                                    "1" : "1",*/
                                "CHM " + PulseCount.ToString(),
                            PulseTypeName.SHE => "SHE " + PulseCount.ToString(),
                            PulseTypeName.HO => "HO " + PulseCount.ToString(),
                            PulseTypeName.BBCS => "BBCS " + PulseCount.ToString(),
                            PulseTypeName.CSVS => "CSVS " + PulseCount.ToString(),
                            PulseTypeName.AZCS =>
                                /*PulseCount == 7 ? amp < 0.1 ? "Off" :
                                freq < 60 ?
                                    amp < str1170 ? "1170 Hz" :
                                    amp < str1000 ? "1000 Hz" :
                                    amp < str15 ? "CSVS 15" :
                                    amp < 1.17 ? "BSS 7" :
                                    amp < 1.20 ? "BSS 5" :
                                    amp < str3 ? "BSS 3" :
                                    "1" :
                                freq < 1000 / 11d ?
                                    amp < str1170 ? "1170 Hz" :
                                    amp < str1000 ? "1000 Hz" :
                                    amp < 1.17 ? "BSS 7" :
                                    amp < 1.20 ? "BSS 5" :
                                    amp < str3 ? "BSS 3" :
                                    "1" :
                                freq < 1000 / 7d ?
                                    amp < str1170 ? "1170 Hz" :
                                    amp < str1000 ? "1000 Hz" :
                                    amp < 1.20 ? "BSS 5" :
                                    amp < str3 ? "BSS 3" :
                                    "1" :
                                freq < 1000 / 3d ?
                                    amp < str1170 ? "1170 Hz" :
                                    amp < str1000 ? "1000 Hz" :
                                    amp < str3 ? "BSS 3" :
                                    "1" : "1" :*/
                                "AZCS " + PulseCount.ToString()
                            ,
                            _ => PulseCount.ToString(),
                        };

                        Model.SineFrequency = Param.Control.GetBaseWaveFrequency();
                        double Gear1 = Properties.Settings.Default.RealTime_Controller_Design_Gear1;
                        double Gear2 = Properties.Settings.Default.RealTime_Controller_Design_Gear2;
                        double Poles = Properties.Settings.Default.RealTime_Controller_Design_MotorPoles;
                        double WheelDiameter = Properties.Settings.Default.RealTime_Controller_Design_WheelDiameter;
                        double SlipRate = Properties.Settings.Default.RealTime_Controller_Design_SlipRate;//转差率
                        double AccelChangeRate = Properties.Settings.Default.RealTimeMasconAccelFrequencyChangeRate;
                        double BrakeChangeRate = Properties.Settings.Default.RealTimeMasconBrakeFrequencyChangeRate;

                        double rawSpeed = GetSpeed(Param.Control.GetBaseWaveFrequency(), (int)Poles, (int)Gear1, (int)Gear2, WheelDiameter, SlipRate);
                        // Low-pass filter for Speed display (alpha between 0 and 1, lower is smoother)
                        double speedAlpha = 0.002;
                        Model.SmoothedSpeed = speedAlpha * rawSpeed + (1 - speedAlpha) * Model.SmoothedSpeed;
                        Model.Speed = Model.SmoothedSpeed;
                        double sineFrequencyForAcceleration = Control.GetBaseWaveFrequency() + 1e-6;
                        double effectiveFrequencyChangeRate;
                        //effectiveFrequencyChangeRate = !Param.IsBraking ? Param.SmoothedFrequencyChangeRate * 1.0 / sineFrequencyForAcceleration * (Model.Voltage + 1e-6) : Param.SmoothedFrequencyChangeRate;
                        effectiveFrequencyChangeRate = Param.SmoothedFrequencyChangeRate;
                        Model.Accelerate = effectiveFrequencyChangeRate * Model.SmoothedSpeed / sineFrequencyForAcceleration / MyMath.M_2PI;

                        double decel_func = (0.5 - 1.3) / (65 - 47) * (Control.GetBaseWaveFrequency() - 47) + 1.3;
                        double decel = decel_func < 0.5 ? 0.5 : decel_func > 1.3 ? 1.3 : decel_func;
                        //Model.Speed = decel;
                        Model.Ratio = Gear1 + ":" + Gear2 + " (" + Gear2 / Gear1;
                        Model.MotorRPM = 120 * Param.Control.GetBaseWaveFrequency() / Poles;
                        //Model.WheelRPM = Model.MotorRPM * Gear1 / Gear2; 
                        //Model.SetAcceleration = -100 / AccelChangeRate / BrakeChangeRate;
                        //Model.SetDeceleration = 100 / BrakeChangeRate  / AccelChangeRate * 7 / 4;
                        Model.Deceleration = Model.Speed * Model.Speed / (2 * Model.Distance) / 3.6; //推荐减速度
                        double Distance = 1200;
                        Model.Distance = -Param.Control.GetBaseWaveTime() * Model.Speed / 3.6 + Distance;
                        //while (Model.Distance < -0.15) Model.Distance += Distance;
                        //while (Model.Distance < 0.0001) Model.Distance = 0.0001;
                        string gear = (Gear2 / Gear1).ToString("F3");
                        Model.Gear = Gear1 + ":" + Gear2 + "(" + gear + ")";
                        //Model.Amplitude = (Param.Control.GetVideoSineAmplitude() * 100).ToString("F1");
                        Model.Amplitude = (Control.GetAmp() > 0.001 ?(Param.VoltageMultiply >= 0 ? Control.GetAmp() : -Control.GetAmp()) * 100 : 0).ToString("F1") + "%";

                        /*Model.MasconP8 = AccelChangeRate * 1.00;
                        Model.MasconP7 = AccelChangeRate * 0.88;
                        Model.MasconP6 = AccelChangeRate * 0.76;
                        Model.MasconP5 = AccelChangeRate * 0.64;
                        Model.MasconP4 = AccelChangeRate * 0.52;
                        Model.MasconP3 = AccelChangeRate * 0.37;
                        Model.MasconP2 = AccelChangeRate * 0.22;
                        Model.MasconP1 = AccelChangeRate * -0.04;
                        Model.MasconP0 = AccelChangeRate * -0.00;

                        Model.MasconB0 = BrakeChangeRate * -0.00;
                        Model.MasconB1 = BrakeChangeRate * -0.04;
                        Model.MasconB2 = BrakeChangeRate * -0.20;
                        Model.MasconB3 = BrakeChangeRate * -0.30;
                        Model.MasconB4 = BrakeChangeRate * -0.40;
                        Model.MasconB5 = BrakeChangeRate * -0.48;
                        Model.MasconB6 = BrakeChangeRate * -0.55;
                        Model.MasconB7 = BrakeChangeRate * -0.62;
                        Model.MasconB8 = BrakeChangeRate * -0.69;
                        Model.MasconB9 = BrakeChangeRate * -0.76;
                        Model.MasconB10 = BrakeChangeRate * -0.85;
                        Model.MasconFB = BrakeChangeRate * -1.00;*/
                    }
                }
            });
            Task.Run(() => {
                while (!Param.Quit)
                {
                    System.Threading.Thread.Sleep(10);
					Domain Control = Param.Control.Clone();
					Control.GetCarrierInstance().UseSimpleFrequency = true;

					double InitialPhase = MyMath.M_PI_6;
					{
						if (Param is VvvfSoundParameter VvvfParam)
						{
							InitialPhase = VvvfParam.OutputMode switch
							{
								VvvfSoundParameter.Mode.Phase => 0,
								VvvfSoundParameter.Mode.PhaseCurrent => 0,
								_ => MyMath.M_PI_6,
							};
						}
					}

					PhaseState[] Pwm = GenerateBasic.WaveForm.GetUVWCycle(Control, InitialPhase, CalculatePrecision, false);

                    double voltage = GenerateBasic.Fourier.GetVoltageRate(ref Pwm) * 100;
                    Model.Voltage = voltage;
					//Model.Voltage = Calculate.GetChangingValue_Pow(0, 50, 0.6, 6, 1, Param.Control.GetVideoSineAmplitude(), true);
					//Model.Voltage = (Param.DAmp_dt * (0.01728571 * Control.GetBaseWaveFrequency() + 0.05) + Control.GetAmp() * (0.01728571 * Param.FrequencyChangeRate / M_2PI)) * 100;

					List<(double X, double Pwm)> Data = [];

					for (int i = 0; i < Pwm.Length; i++)
                    {
                        double NewPwmLine = 0;
                        if (Param is VvvfSoundParameter VvvfParam)
                        {
                            NewPwmLine = VvvfParam.OutputMode switch
                            {
                                VvvfSoundParameter.Mode.Line => Pwm[i].U - Pwm[i].V,
                                VvvfSoundParameter.Mode.Phase => (Pwm[i].U - 1) * 2,
                                VvvfSoundParameter.Mode.PhaseCurrent => Pwm[i].U - Pwm[i].V * 0.5 - Pwm[i].W * 0.5,
                                VvvfSoundParameter.Mode.Unbalanced_Overlap => Pwm[i].U - Pwm[i].V * 0.8,
                                VvvfSoundParameter.Mode.Balanced_Overlap_L => (Pwm[i].U - Pwm[i].V * 1.1 + Pwm[i].W * 0.1) * 0.9,
                                VvvfSoundParameter.Mode.Balanced_Overlap_R => Pwm[i].U - Pwm[i].V * 0.9 - Pwm[i].W * 0.1,
                                _ => 0
                            };
                        }
                        else if (Param is TrainSoundParameter TrainParam)
                        {
                            NewPwmLine = Pwm[i].U - Pwm[i].V;
                        }

                        if (Data.Count == 0 || Data[^1].Pwm != NewPwmLine)
                            Data.Add(((double)i / Pwm.Length, NewPwmLine));
                    }
                    Data.Add((1.0, Data[^1].Pwm));

                    double GetY(double Pwm)
                    {
                        return -Pwm * WaveformViewer.ActualHeight / 2.0 * (1 / 2.0 - 1 / 10.0) + WaveformViewer.ActualHeight / 2.0;
                    }
                    double GetX(double Ratio)
                    {
                        return Ratio * WaveformViewer.ActualWidth;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PathFigure Figure = new()
                        {
                            StartPoint = new(GetX(Data[0].X), GetY(Data[0].Pwm))
                        };
                        for (int i = 0; i < Data.Count - 1; i++)
                        {
                            Figure.Segments.Add(new LineSegment()
                            {
                                Point = new()
                                {
                                    X = GetX(Data[i].X),
                                    Y = GetY(Data[i].Pwm),
                                },
                                IsStroked = true,
                            });
                            Figure.Segments.Add(new LineSegment()
                            {
                                Point = new()
                                {
                                    X = GetX(Data[i + 1].X),
                                    Y = GetY(Data[i].Pwm),
                                },
                                IsStroked = true,
                            });
                        }
                        Figure.Segments.Add(new LineSegment()
                        {
                            Point = new()
                            {
                                X = GetX(Data[^1].X),
                                Y = GetY(Data[^1].Pwm),
                            },
                            IsStroked = true,
                        });

                        PathGeometry Geometry = new();
                        Geometry.Figures.Add(Figure);

                        System.Windows.Shapes.Path Path = new()
                        {
                            Stroke = ThemeManager.GetBrush("RealtimeControllerWaveformBrush"),
                            StrokeThickness = 3.0,
                            Data = Geometry,
                        };
                        WaveformViewer.Children.Clear();
                        WaveformViewer.Children.Add(Path);
                    });

                }
            });
        }
        private void SetColor(int c, SolidColorBrush brush)
        {
            if (c == 0) Model.N = brush;

            if (c == -1) Model.B0 = brush;
            if (c == -2) Model.B1 = brush;
            if (c == -3) Model.B2 = brush;
            if (c == -4) Model.B3 = brush;
            if (c == -5) Model.B4 = brush;
            if (c == -6) Model.B5 = brush;
            if (c == -7) Model.B6 = brush;
            if (c == -8) Model.B7 = brush;
            if (c == -9) Model.B8 = brush;
            if (c == -10) Model.B9 = brush;
            if (c == -11) Model.B10 = brush;
            if (c == -12) Model.FB = brush;

            if (c == 1) Model.P0 = brush;
            if (c == 2) Model.P1 = brush;
            if (c == 3) Model.P2 = brush;
            if (c == 4) Model.P3 = brush;
            if (c == 5) Model.P4 = brush;
            if (c == 6) Model.P5 = brush;
            if (c == 7) Model.P6 = brush;
            if (c == 8) Model.P7 = brush;
            if (c == 9) Model.P8 = brush;
        }


        private void SetState(int at)
        {
            int at_abs = (at < 0) ? -at : at;
            bool nega = at < 0;

            double AccelFrequencyChangeRate = Properties.Settings.Default.RealTimeMasconAccelFrequencyChangeRate;
            double BrakeFrequencyChangeRate = Properties.Settings.Default.RealTimeMasconBrakeFrequencyChangeRate;
            double WheelDiameter = Properties.Settings.Default.RealTime_Controller_Design_WheelDiameter;
            double Gear1 = Properties.Settings.Default.RealTime_Controller_Design_Gear1;
            double Gear2 = Properties.Settings.Default.RealTime_Controller_Design_Gear2;
            double Poles = Properties.Settings.Default.RealTime_Controller_Design_MotorPoles;
            double SlipRate = Properties.Settings.Default.RealTime_Controller_Design_SlipRate;
            //Param.Control.GetFrequencyChangeRate() = (nega ? -1 : 1) * ((at_abs - 1 < 0) ? 0 : at_abs - 1) * Math.PI / 8 * FrequencyChangeRate;
            double AccelSpeedChangeRate = Math.PI * 2 * AccelFrequencyChangeRate / (0.0036 * WheelDiameter * Math.PI * Gear1 / Gear2 * 2 / Poles * (100 - SlipRate) / 100);
            double BrakeSpeedChangeRate = Math.PI * 2 * BrakeFrequencyChangeRate / (0.0036 * WheelDiameter * Math.PI * Gear1 / Gear2 * 2 / Poles * (100 - SlipRate) / 100);

            /*double rate = 3;
            double initial = 2;
            double final = rate * Model.Speed + initial;
            //bool bool = Model.Accelerate < FrequencyChangeRate;
            bool bool = false;
            double Accel = AccelSpeedChangeRate / AccelFrequencyChangeRate;
            double Brake = BrakeSpeedChangeRate / BrakeFrequencyChangeRate;
            if (at == 9) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP8 * Accel;
            else if (at == 8) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP7 * Accel;
            else if (at == 7) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP6 * Accel;
            else if (at == 6) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP5 * Accel;
            else if (at == 5) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP4 * Accel;
            else if (at == 4) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP3 * Accel;
            else if (at == 3) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP2 * Accel;
            else if (at == 2) Param.Control.GetFrequencyChangeRate() = bool ? final : Model.MasconP1 * Accel;
            else if (at == 1) Param.Control.GetFrequencyChangeRate() = Model.MasconP0 * Accel;

            else if (at == -1) Param.Control.GetFrequencyChangeRate() = Model.MasconB0 * Brake;
            else if (at == -2) Param.Control.GetFrequencyChangeRate() = Model.MasconB1 * Brake;
            else if (at == -3) Param.Control.GetFrequencyChangeRate() = Model.MasconB2 * Brake;
            else if (at == -4) Param.Control.GetFrequencyChangeRate() = Model.MasconB3 * Brake;
            else if (at == -5) Param.Control.GetFrequencyChangeRate() = Model.MasconB4 * Brake;
            else if (at == -6) Param.Control.GetFrequencyChangeRate() = Model.MasconB5 * Brake;
            else if (at == -7) Param.Control.GetFrequencyChangeRate() = Model.MasconB6 * Brake;
            else if (at == -8) Param.Control.GetFrequencyChangeRate() = Model.MasconB7 * Brake;
            else if (at == -9) Param.Control.GetFrequencyChangeRate() = Model.MasconB8 * Brake;
            else if (at == -10) Param.Control.GetFrequencyChangeRate() = Model.MasconB9 * Brake;
            else if (at == -11) Param.Control.GetFrequencyChangeRate() = Model.MasconB10 * Brake;
            else if (at == -12) Param.Control.GetFrequencyChangeRate() = Model.MasconFB * Brake;*/

            //if (Param.Control.GetFrequencyChangeRate() > FrequencyChangeRate / M_2PI) Param.Control.GetFrequencyChangeRate() = FrequencyChangeRate * M_2PI;

            bool pre_braking = Param.IsBraking;
            Param.IsFreeRunning = at == 114514;
            Param.IsBraking = Param.FrequencyChangeRate < 0;

            SolidColorBrush inactive = (SolidColorBrush)ThemeManager.GetBrush("RealtimeControllerDefaultBackgroundBrush");

            if (at == 0)
            {
                for (int i = -13; i < 10; i++)
                {
                    SetColor(i, inactive);
                }
                SetColor(0, (SolidColorBrush)ThemeManager.GetBrush("MasconNeutralBrush"));
                return;
            }
            else
                SetColor(0, inactive);

            for (int i = 1; i <= 13; i++)
            {
                SolidColorBrush target;
                if (nega) target = (SolidColorBrush)ThemeManager.GetBrush("MasconB3Brush");
                else target = (SolidColorBrush)ThemeManager.GetBrush($"MasconP3Brush");

                if (at_abs >= i)
                    SetColor(nega ? -i : i, target);
                else
                    SetColor(nega ? -i : i, inactive);

                SetColor(nega ? i : -i, inactive);
            }
        }
        private void SetWindowContentVisibility()
        {
            Visibility VvvfItemVisibility = (Param is VvvfSoundParameter) ? Visibility.Visible : Visibility.Collapsed;
            MenuItem_Vvvf.Visibility = VvvfItemVisibility;
            MenuItem_Vvvf_Separator.Visibility = VvvfItemVisibility;
        }

        //
        // Controller Device Interface
        //
        public int GetPosition()
        {
            return MasconPosition;
        }
        public void PrepareController()
        {
            if (CurrentMode == DeviceMode.PicoMascon)
            {
                if (serialPort.IsOpen) serialPort.Close();
                try
                {
                    serialPort = new SerialPort(MasconComPort)
                    {
                        BaudRate = 9600,
                        Parity = Parity.None,
                        StopBits = StopBits.One,
                        DataBits = 8,
                        Handshake = Handshake.None,
                        DtrEnable = true
                    };
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(OnReceived);
                    serialPort.Open();
                }
                catch (Exception ex)
                {
					DialogBox.Show(ex.Message, LanguageManager.GetString("Generic.Title.Error"), [DialogBoxButton.Ok], DialogBoxIcon.Error);
				}
			}
            else if (CurrentMode == DeviceMode.KeyBoard)
            {
                if (serialPort.IsOpen) serialPort.Close();
            }
        }
        public DeviceMode GetControllerMode()
        {
            return CurrentMode;
        }
        public void SetControllerMode(DeviceMode mode)
        {
            CurrentMode = mode;
        }
        public string GetComPort()
        {
            return MasconComPort;
        }
        public void SetComPort(string port)
        {
            MasconComPort = port;
        }
        public Window GetInstance()
        {
            return this;
        }


        // Controller Input
        private void ControllerHandle_Keyboard(System.Windows.Input.Key key)
        {
            Random random = new();
            int p = Properties.Settings.Default.RealTime_Controller_Design_MotorPoles;
            int m = Properties.Settings.Default.RealTime_Controller_Design_Gear1;
            int w = Properties.Settings.Default.RealTime_Controller_Design_Gear2;
            double d = Properties.Settings.Default.RealTime_Controller_Design_WheelDiameter;
            double s = Properties.Settings.Default.RealTime_Controller_Design_SlipRate;
            double acc = Properties.Settings.Default.RealTimeMasconAccelFrequencyChangeRate;
            double dec = Properties.Settings.Default.RealTimeMasconBrakeFrequencyChangeRate;

            /*if (key.Equals((Key)Properties.Settings.Default.RealTimeMasconAccelerateKey))
                _SliderChangeRate.Value = _SliderChangeRate.Value - 1 * 16 / acc / dec * b;
            else if (key.Equals((Key)Properties.Settings.Default.RealTimeMasconBrakeKey))
                _SliderChangeRate.Value = _SliderChangeRate.Value + 1 * 16 / acc / dec * b;
            else if (key.Equals((Key)Properties.Settings.Default.RealTimeMasconNeutralKey))
                _SliderChangeRate.Value = (Math.Pow(0.005*Model.Speed,1.3)+0.3) * 16 / acc / dec;*/

            double f = 0.5;

            if (key.Equals((Key)Properties.Settings.Default.RealTimeMasconAccelerateKey))
            {
                _SliderChangeRate.Value = !Param.IsBraking ? _SliderChangeRate.Value - 5 * f : _SliderChangeRate.Value - 4 * f;
                //_SliderVoltage.Value = !Param.IsBraking ? _SliderVoltage.Value - 20 : _SliderVoltage.Value - 14;
            }
            else if (key.Equals((Key)Properties.Settings.Default.RealTimeMasconBrakeKey))
            {
                _SliderChangeRate.Value = !Param.IsBraking ? _SliderChangeRate.Value + 5 * f : _SliderChangeRate.Value + 4 * f;
                //_SliderVoltage.Value = !Param.IsBraking ? _SliderVoltage.Value + 20 : _SliderVoltage.Value + 14;
            }
            else if (key.Equals((Key)Properties.Settings.Default.RealTimeMasconNeutralKey))
            { 
                _SliderChangeRate.Value = (Pow(0.04 * Model.Speed, 2) + 6) / acc / dec;
                //_SliderVoltage.Value = 0;
                //Param.Control.SetAmp(0);
            }

            //_SliderChangeRate.Value = 0;

            /*else if (key.Equals((System.Windows.Input.Key)46))
            {
                double a = 0.04;
                double Acc = 3.6; //Accerlation
                double decel = 2.1;
                double fix = 0.001 * Math.Pow(Model.Speed, 1.02);
                double mvs = 43; //Max Voltage Speed
                double mas = 75; //Max AccelateRate Speed
                double mrs = 74; //Max Running Speed
                if (Model.Deceleration < decel && !Param.Control.IsBraking())
                {
                    if (Model.Speed >= 0 && Model.Speed < 46) _SliderChangeRate.Value = -3.0 * 2;
                    else _SliderChangeRate.Value = -102 / (Model.Speed - 12) * 2;
                }
                else if (Model.Deceleration < decel)
                {
                    if (-Model.Accelerate < Model.Deceleration + a) _SliderChangeRate.Value += 0.05;
                    else if (-Model.Accelerate >= Model.Deceleration - a) _SliderChangeRate.Value -= 0.05;
                    if (MasconPosition > -1) MasconPosition = -1;
                }
                else if (Model.Deceleration >= decel)
                {
                    if (-Model.Accelerate < Model.Deceleration + a) _SliderChangeRate.Value += 0.05;
                    else if (-Model.Accelerate >= Model.Deceleration - a) _SliderChangeRate.Value -= 0.05;
                    if (MasconPosition > -1) MasconPosition = -1;
                }
            }*/

            else if (key.Equals((System.Windows.Input.Key)69))
            {
                double a = 0.4;
                double k = 1200;
                double valueV = 107; //电压手柄最大值（最大170）
                double valueA = 24; //加速度手柄最大值（最大26）
                double Acc = 3.6; //Accerlation
                double decel = 1.6;
                double fix = 0.001 * Math.Pow(Model.Speed, 1.02);
                double mps = 44; //Max Power Speed
                double mrs = 77; //Max Running Speed

                if (Model.Deceleration < decel)
                {
                    if (Model.Speed < mps)
                    {
                        _SliderChangeRate.Value = GetChangingValue(0, -0.2, Sqrt(6), -valueA, Sqrt(Model.Speed), true);
                        if (Model.Speed < 25) _SliderVoltage.Value = GetChangingValue(Sqrt(1), -50, Sqrt(4), -85, Sqrt(Model.Speed), true);
                        else _SliderVoltage.Value = GetChangingValue(Sqrt(25), -85, Sqrt(29), -valueV, Sqrt(Model.Speed), true);
                    }
                    else if (Model.Speed < mrs + 1 && !Param.IsBraking) _SliderChangeRate.Value = -(k / Model.Speed + valueA - k / mps);
                    else if (Model.Speed < mrs + 2 && !Param.IsBraking)
                    {
                        _SliderChangeRate.Value = (Pow(0.07 * Model.Speed, 1.3) + 10) / acc / dec;
                        _SliderVoltage.Value = 0;
                    }
                }
                else if (Model.Deceleration < decel)
                {
                    if (-Model.Accelerate < Model.Deceleration + a) _SliderChangeRate.Value += 0.5;
                    else if (-Model.Accelerate >= Model.Deceleration - a) _SliderChangeRate.Value -= 0.5;
                    if (MasconPosition < -100) MasconPosition = -100;
                }
                else if (Model.Deceleration >= decel)
                {
                    if (-Model.Accelerate < Model.Deceleration + a) _SliderChangeRate.Value += 0.5;
                    else if (-Model.Accelerate >= Model.Deceleration - a) _SliderChangeRate.Value -= 0.5;
                    if (MasconPosition < -100) MasconPosition = -100;
                }
            }

            //else if (key.Equals((System.Windows.Input.Key)47)) _SliderChangeRate.Value = random.NextDouble() * -3 - 1;

            else if (key.Equals((System.Windows.Input.Key)67))
            {
                _SliderVoltage.Value /= 10000;
                Param.Amplitude = 0;
                Param.FreeRunDt = -1;
            }
            else if (key.Equals((System.Windows.Input.Key)47)) _SliderVoltage.Value += 5;
            else if (key.Equals((System.Windows.Input.Key)46)) _SliderVoltage.Value -= 5;
            else if (key.Equals((System.Windows.Input.Key)49)) _SliderVoltage.Value = -_SliderVoltage.Value;
            else if (key.Equals((System.Windows.Input.Key)48)) _SliderVoltage.Value = -160;

            //if (MasconPosition > (int)Model.SetAcceleration) MasconPosition = (int)Model.SetAcceleration;
            //if (MasconPosition < (int)Model.SetDeceleration) MasconPosition = (int)Model.SetDeceleration;

            //if (MasconPosition > 100) MasconPosition = 100;
            //if (MasconPosition < -100) MasconPosition = -100;

            SetState(MasconPosition);
        }

            private void OnReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!serialPort.IsOpen) return;
            String read = serialPort.ReadExisting();

            if (CurrentMode == DeviceMode.PicoMascon)
            {
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        int current = Int32.Parse(read);
                        MasconPosition = current - 5;
                        SetState(MasconPosition);
                    });
                }
                catch (Exception) { }
            }

        }

        private void OnMasconMouseEvent(object sender, MouseEventArgs e)
        {
            if (sender is not Border border) return;
            string? tag = border.Tag.ToString();
            if (tag == null) return;
            MasconPosition = int.Parse(tag);
            SetState(MasconPosition);
        }

        //
        // Window Event
        //
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Input.Key key = e.Key;

            if (CurrentMode == DeviceMode.KeyBoard) ControllerHandle_Keyboard(key);
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem Item) return;
            string[]? Tag = Item.Tag?.ToString()?.Split('_');
            if (Tag == null) return;

            if (Tag[0].Equals("DeviceSetting"))
            {
                MasconDevice rtds = new(this);
                rtds.ShowDialog();
            }
            else if (Tag[0].Equals("CalculateDivision"))
            {
                IntegerNumberInput Input = new(this, LanguageManager.GetString("Simulator.RealTime.Controller.Design2.MenuItem.CalculateDivision"), 0, CalculatePrecision);
                if (!Input.IsEnteredValueValid()) return;
                CalculatePrecision = Input.GetEnteredValue();
                switch (Param is VvvfSoundParameter)
                {
                    case true:
                        Properties.Settings.Default.RealTime_VVVF_Controller_Design2_CalculatePrecision = CalculatePrecision;
                        break;
                    default:
                        Properties.Settings.Default.RealTime_Train_Controller_Design2_CalculatePrecision = CalculatePrecision;
                        break;
                }
                Properties.Settings.Default.Save();
            }
            else if (Tag[0].Equals("Vvvf"))
            {
                if (Param is not VvvfSoundParameter) return;
                if (Tag[1].Equals("WaveForm"))
                {
                    MenuItem[] Candidates = [MenuItem_Vvvf_WaveForm_Line, MenuItem_Vvvf_WaveForm_Phase, MenuItem_Vvvf_WaveForm_PhaseCurrent, MenuItem_Vvvf_WaveForm_Unbalanced_Overlap, MenuItem_Vvvf_WaveForm_Balanced_Overlap_L, MenuItem_Vvvf_WaveForm_Balanced_Overlap_R];
                    VvvfSoundParameter.Mode[] Modes = (VvvfSoundParameter.Mode[])Enum.GetValues(typeof(VvvfSoundParameter.Mode));

                    for (int i = 0; i < Candidates.Length; i++)
                    {
                        MenuItem Candidate = Candidates[i];
                        bool Selected = Candidate == Item;
                        Candidate.IsChecked = Selected;
                        if (Selected) ((VvvfSoundParameter)Param).OutputMode = Modes[i];
                    }
                }
            }
            else if (Tag[0].Equals("TrainSettings"))
            {
                List<DialogInputWindow.InputContext> inputs =
                [
                    new("车轮直径（200~2000mm)", DialogInputWindow.InputContextMode.TextBox, Properties.Settings.Default.RealTime_Controller_Design_WheelDiameter, typeof(double)),
                    new("小齿轮齿数（10~50)", DialogInputWindow.InputContextMode.TextBox, Properties.Settings.Default.RealTime_Controller_Design_Gear1, typeof(int)),
                    new("大齿轮齿数（20~200)", DialogInputWindow.InputContextMode.TextBox, Properties.Settings.Default.RealTime_Controller_Design_Gear2, typeof(int)),
                    new("电机极数（1~32)", DialogInputWindow.InputContextMode.TextBox, Properties.Settings.Default.RealTime_Controller_Design_MotorPoles, typeof(int)),
                    new("转差率（0%~20%)", DialogInputWindow.InputContextMode.TextBox, Properties.Settings.Default.RealTime_Controller_Design_SlipRate, typeof(double)),
                ];
                DialogInputWindow InputWindow = new(
                    this,
                    LanguageManager.GetString("参数设置"),
                    inputs
                );
                InputWindow.ShowDialog();
                if (InputWindow.Contexts == null)
                {
                    //SystemSounds.Beep.Play();
                    return;
                }
                Properties.Settings.Default.RealTime_Controller_Design_WheelDiameter = InputWindow.GetValue<double>(0);
                Properties.Settings.Default.RealTime_Controller_Design_Gear1 = InputWindow.GetValue<int>(1);
                Properties.Settings.Default.RealTime_Controller_Design_Gear2 = InputWindow.GetValue<int>(2);
                Properties.Settings.Default.RealTime_Controller_Design_MotorPoles = InputWindow.GetValue<int>(3);
                Properties.Settings.Default.RealTime_Controller_Design_SlipRate = InputWindow.GetValue<double>(4);
                Properties.Settings.Default.Save();
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (serialPort.IsOpen) serialPort.Close();
            Param.Quit = true;
        }
        private void OnWindowControlButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string? tag = btn.Tag.ToString();
            if (tag == null) return;

            if (tag.Equals("Close"))
                Close();
            else if (tag.Equals("Maximize"))
            {
                if (WindowState.Equals(WindowState.Maximized))
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
            }
            else if (tag.Equals("Minimize"))
                WindowState = WindowState.Minimized;
        }

        private void SliderChangeRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double p = Properties.Settings.Default.RealTime_Controller_Design_MotorPoles;
            double m = Properties.Settings.Default.RealTime_Controller_Design_Gear1;
            double w = Properties.Settings.Default.RealTime_Controller_Design_Gear2;
            double d = Properties.Settings.Default.RealTime_Controller_Design_WheelDiameter;
            double s = Properties.Settings.Default.RealTime_Controller_Design_SlipRate;
            double acc = Properties.Settings.Default.RealTimeMasconAccelFrequencyChangeRate;
            double dec = Properties.Settings.Default.RealTimeMasconBrakeFrequencyChangeRate;

            if (SetTextBox)
            {
                Ignore_TextBoxChangeRate_TextChanged = true;
                //_TextBoxChangeRate.Text = (Param.FrequencyChangeRate / M_2PI * 0.0072 * s * d * m / p / w).ToString("F2");
                //_TextBoxChangeRate.Text = (-e.NewValue > 0 ? -e.NewValue / 26 * acc : -e.NewValue / 20 * dec).ToString("F2");
                //_TextBoxChangeRate.Text = (-_SliderChangeRate.Value > 0 ? -_SliderChangeRate.Value * 5 / 1.3 : -_SliderChangeRate.Value * 5).ToString("F1") + "%";
            }
            else
            {
                SetTextBox = true;
            }
            SetState((int)Math.Round(-e.NewValue));
            MasconPosition = (int)Math.Round(-e.NewValue);
            double at = -e.NewValue;
            bool nega = at < 0;
            double at_abs = Math.Abs(at);
            double AccelChangeRate = Properties.Settings.Default.RealTimeMasconAccelFrequencyChangeRate;

            double ChangeRate = M_2PI * at / (0.0036 * d * Math.PI * m / w * 2 / p * (100 - s));
            Param.FrequencyChangeRate = !nega ? ChangeRate * 5 / 1.3 * acc : ChangeRate * 5 * dec;

            // Ensure FreeRunning flag correctly reflects neutral (at == 0)
            Param.IsFreeRunning = at == 114514;
            Param.IsBraking = Param.FrequencyChangeRate < 0;
        }

        private void SliderVoltage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SetTextBox)
            {
                double final1 = Pow(-_SliderVoltage.Value, 2) / 700;
                Ignore_TextBoxVoltage_TextChanged = true;
                //_TextBoxVoltage.Text = (-_SliderVoltage.Value).ToString("F1") + "%";
                //_TextBoxVoltage.Text = (-_SliderVoltage.Value >= 0 ? final1 : -final1).ToString("F1") + "%";
            }
            else
            {
                SetTextBox = true;
            }
			double final2 = Pow(-e.NewValue, 2) / 700 / 100f;
			// Param.VoltageMultiply = Abs(e.NewValue) / 100f < 0.0 ? 0 : -e.NewValue / 100f;
			Param.VoltageMultiply = -e.NewValue / 100f;
			Param.FreeRunDt = 0;
            //Param.VoltageMultiply = -e.NewValue >= 0 ? final2 : -final2;
        }

        private void MySlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 根据滚轮方向改变Slider的Value
            // 步长可以自行调整，例如使用Slider的SmallChange属性
            //double step = _SliderChangeRate.SmallChange;
            double step = 0.43;
            if (e.Delta > 0)
            {
                _SliderChangeRate.Value += step;
            }
            else
            {
                _SliderChangeRate.Value -= step;
            }
            // 标记事件为已处理，防止事件继续向上冒泡（可选）
            e.Handled = true;
        }

        private bool Ignore_TextBoxChangeRate_TextChanged = false;
        private bool Ignore_TextBoxVoltage_TextChanged = false;
        private bool SetTextBox = true;

        private void TextBoxChangeRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            if (Ignore_TextBoxChangeRate_TextChanged)
            {
                Ignore_TextBoxChangeRate_TextChanged = false;
                return;
            }
            SetTextBox = false;
            double val = ParseTextBox.ParseDouble((TextBox)sender, double.MinValue, -_SliderChangeRate.Value);
            _SliderChangeRate.Value = -val;
        }
        private void TextBoxVoltage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null) return;
            if (Ignore_TextBoxVoltage_TextChanged)
            {
                Ignore_TextBoxVoltage_TextChanged = false;
                return;
            }
            SetTextBox = false;
            double val = ParseTextBox.ParseDouble((TextBox)sender, double.MinValue, -_SliderVoltage.Value);
            _SliderVoltage.Value = val;
        }
    }
}
