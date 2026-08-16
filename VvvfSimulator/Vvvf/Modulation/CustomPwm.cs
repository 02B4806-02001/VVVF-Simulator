using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using static VvvfSimulator.Data.Vvvf.Struct.PulseControl.Pulse;
using static VvvfSimulator.Data.Vvvf.Analyze;
using static VvvfSimulator.GUI.Simulator.RealTime.Controller.Design2.Design2;
using VvvfSimulator.Generation.Video.ControlInfo;

namespace VvvfSimulator.Vvvf.Modulation
{
    public class CustomPwm
    {
        private const int MaxPwmLevel = 2;

        public byte SwitchCount = 0;
        public double ModulationIndexDivision = 0.0;
        public double MinimumModulationIndex = 0.0;
        public uint BlockCount = 0;
        public (double SwitchAngle, byte Output)[] SwitchAngleTable = Array.Empty<(double, byte)>();
        public byte[] StartLevelTable = Array.Empty<byte>();

        public CustomPwm()
        {
        }

        public CustomPwm(Stream? St)
        {
            if (St == null) throw new Exception();

            St.Seek(0, SeekOrigin.Begin);
            byte SwitchCount = (byte)St.ReadByte();

            byte[] DivisionRaw = new byte[8];
            St.ReadExactly(DivisionRaw, 0, 8);
            double Division = BitConverter.ToDouble(DivisionRaw, 0);

            byte[] StartModulationRaw = new byte[8];
            St.ReadExactly(StartModulationRaw, 0, 8);
            double StartModulation = BitConverter.ToDouble(StartModulationRaw, 0);

            byte[] LengthRaw = new byte[4];
            St.ReadExactly(LengthRaw, 0, 4);
            uint Length = BitConverter.ToUInt32(LengthRaw, 0);

            this.SwitchCount = SwitchCount;
            this.ModulationIndexDivision = Division;
            this.MinimumModulationIndex = StartModulation;
            this.BlockCount = Length;

            this.SwitchAngleTable = new (double, byte)[this.BlockCount * this.SwitchCount];
            this.StartLevelTable = new byte[this.BlockCount];

            for (int i = 0; i < this.BlockCount; i++)
            {
                this.StartLevelTable[i] = (byte)St.ReadByte();

                for (int j = 0; j < this.SwitchCount; j++)
                {
                    byte[] LevelRaw = new byte[1];
                    byte[] SwitchAngleRaw = new byte[8];
                    St.ReadExactly(LevelRaw, 0, 1);
                    St.ReadExactly(SwitchAngleRaw, 0, 8);
                    byte Output = LevelRaw[0];
                    double SwitchAngle = BitConverter.ToDouble(SwitchAngleRaw, 0);
                    this.SwitchAngleTable[i * this.SwitchCount + j] = new(SwitchAngle, Output);
                }
            }

            St.Close();
        }

        public delegate int GetPwmDelegate(double M, double X);

        public virtual int GetPwm(double M, double X)
        {
            // If there are no blocks or no switch count, return baseline 0
            if (BlockCount == 0 || SwitchCount == 0) return 0;

            int blocks = (int)BlockCount;

            // Protect against zero division and enforce modulation index limits
            if (ModulationIndexDivision == 0.0)
            {
                // Whole table applies for any modulation index
                M = MinimumModulationIndex;
            }
            else
            {
                double maxModulation = MinimumModulationIndex + ModulationIndexDivision * (blocks - 1);
                M = Math.Clamp(M, MinimumModulationIndex, maxModulation);
            }

            int Index = (int)((M - MinimumModulationIndex) / (ModulationIndexDivision == 0.0 ? 1.0 : ModulationIndexDivision));
            Index = Math.Clamp(Index, 0, blocks - 1);

            (double SwitchAngle, byte Output)[] Alpha = new (double SwitchAngle, byte Output)[SwitchCount];
            byte StartLevel = StartLevelTable[Index];

            for (int i = 0; i < SwitchCount; i++)
            {
                Alpha[i] = SwitchAngleTable[Index * SwitchCount + i];
            }

            return CustomPwm.GetPwm(ref Alpha, X, StartLevel);
        }

        public static int GetPwm(ref (double SwitchAngle, byte Output)[] Alpha, double X, byte StartLevel)
        {
            X %= MyMath.M_2PI;
            int Orthant = (int)(X / MyMath.M_PI_2);
            double Angle = X % MyMath.M_PI_2;

            if ((Orthant & 0x01) == 1)
                Angle = MyMath.M_PI_2 - Angle;
            int Pwm = StartLevel;
            for (int i = 0; i < Alpha.Length; i++)
            {
                (double SwitchAngle, byte Output) = Alpha[i];
                if (SwitchAngle <= Angle) Pwm = Output;
                else break;
            }

            if (Orthant > 1)
                Pwm = MaxPwmLevel - Pwm;

            return Pwm;
        }
    }
    public static class CustomPwmPresets
    {
        public static bool Loaded = false;
        private static bool Loading = false;
        public static void Load()
        {
            if (Loaded || Loading) return;
            Loading = true;
            Task t = Task.Run(() =>
            {
                L2Chm3Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm3Default.bin"));
                L2Chm3Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm3Alt1.bin"));
                L2Chm3Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm3Alt2.bin"));

                L2Chm5Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm5Default.bin"));
                L2Chm5Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm5Alt1.bin"));
                L2Chm5Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm5Alt2.bin"));
                L2Chm5Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm5Alt3.bin"));

                L2Chm7Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm7Default.bin"));
                L2Chm7Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm7Alt1.bin"));
                L2Chm7Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm7Alt2.bin"));
                L2Chm7Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm7Alt3.bin"));
                L2Chm7Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm7Alt4.bin"));
                L2Chm7Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm7Alt5.bin"));

                L2Chm9Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Default.bin"));
                L2Chm9Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt1.bin"));
                L2Chm9Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt2.bin"));
                L2Chm9Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt3.bin"));
                L2Chm9Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt4.bin"));
                L2Chm9Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt5.bin"));
                L2Chm9Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt6.bin"));
                L2Chm9Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt7.bin"));
                L2Chm9Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm9Alt8.bin"));

                L2Chm11Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Default.bin"));
                L2Chm11Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt1.bin"));
                L2Chm11Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt2.bin"));
                L2Chm11Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt3.bin"));
                L2Chm11Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt4.bin"));
                L2Chm11Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt5.bin"));
                L2Chm11Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt6.bin"));
                L2Chm11Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt7.bin"));
                L2Chm11Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt8.bin"));
                L2Chm11Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt9.bin"));
                L2Chm11Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt10.bin"));
                L2Chm11Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm11Alt11.bin"));

                L2Chm13Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Default.bin"));
                L2Chm13Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt1.bin"));
                L2Chm13Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt2.bin"));
                L2Chm13Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt3.bin"));
                L2Chm13Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt4.bin"));
                L2Chm13Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt5.bin"));
                L2Chm13Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt6.bin"));
                L2Chm13Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt7.bin"));
                L2Chm13Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt8.bin"));
                L2Chm13Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt9.bin"));
                L2Chm13Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt10.bin"));
                L2Chm13Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt11.bin"));
                L2Chm13Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt12.bin"));
                L2Chm13Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm13Alt13.bin"));

                L2Chm15Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Default.bin"));
                L2Chm15Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt1.bin"));
                L2Chm15Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt2.bin"));
                L2Chm15Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt3.bin"));
                L2Chm15Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt4.bin"));
                L2Chm15Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt5.bin"));
                L2Chm15Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt6.bin"));
                L2Chm15Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt7.bin"));
                L2Chm15Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt8.bin"));
                L2Chm15Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt9.bin"));
                L2Chm15Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt10.bin"));
                L2Chm15Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt11.bin"));
                L2Chm15Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt12.bin"));
                L2Chm15Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt13.bin"));
                L2Chm15Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt14.bin"));
                L2Chm15Alt15 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt15.bin"));
                L2Chm15Alt16 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt16.bin"));
                L2Chm15Alt17 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt17.bin"));
                L2Chm15Alt18 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt18.bin"));
                L2Chm15Alt19 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt19.bin"));
                L2Chm15Alt20 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt20.bin"));
                L2Chm15Alt21 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt21.bin"));
                L2Chm15Alt22 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt22.bin"));
                L2Chm15Alt23 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm15Alt23.bin"));

                L2Chm17Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Default.bin"));
                L2Chm17Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt1.bin"));
                L2Chm17Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt2.bin"));
                L2Chm17Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt3.bin"));
                L2Chm17Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt4.bin"));
                L2Chm17Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt5.bin"));
                L2Chm17Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt6.bin"));
                L2Chm17Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt7.bin"));
                L2Chm17Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt8.bin"));
                L2Chm17Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt9.bin"));
                L2Chm17Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt10.bin"));
                L2Chm17Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm17Alt11.bin"));

                L2Chm19Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Default.bin"));
                L2Chm19Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt1.bin"));
                L2Chm19Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt2.bin"));
                L2Chm19Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt3.bin"));
                L2Chm19Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt4.bin"));
                L2Chm19Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt5.bin"));
                L2Chm19Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt6.bin"));
                L2Chm19Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt7.bin"));
                L2Chm19Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt8.bin"));
                L2Chm19Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt9.bin"));
                L2Chm19Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt10.bin"));
                L2Chm19Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm19Alt11.bin"));

                L2Chm21Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Default.bin"));
                L2Chm21Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt1.bin"));
                L2Chm21Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt2.bin"));
                L2Chm21Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt3.bin"));
                L2Chm21Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt4.bin"));
                L2Chm21Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt5.bin"));
                L2Chm21Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt6.bin"));
                L2Chm21Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt7.bin"));
                L2Chm21Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt8.bin"));
                L2Chm21Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt9.bin"));
                L2Chm21Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt10.bin"));
                L2Chm21Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt11.bin"));
                L2Chm21Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt12.bin"));
                L2Chm21Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm21Alt13.bin"));

                L2Chm23Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Default.bin"));
                L2Chm23Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt1.bin"));
                L2Chm23Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt2.bin"));
                L2Chm23Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt3.bin"));
                L2Chm23Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt4.bin"));
                L2Chm23Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt5.bin"));
                L2Chm23Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt6.bin"));
                L2Chm23Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt7.bin"));
                L2Chm23Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt8.bin"));
                L2Chm23Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt9.bin"));
                L2Chm23Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt10.bin"));
                L2Chm23Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt11.bin"));
                L2Chm23Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt12.bin"));
                L2Chm23Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt13.bin"));
                L2Chm23Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm23Alt14.bin"));

                L2Chm25Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Default.bin"));
                L2Chm25Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt1.bin"));
                L2Chm25Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt2.bin"));
                L2Chm25Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt3.bin"));
                L2Chm25Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt4.bin"));
                L2Chm25Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt5.bin"));
                L2Chm25Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt6.bin"));
                L2Chm25Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt7.bin"));
                L2Chm25Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt8.bin"));
                L2Chm25Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt9.bin"));
                L2Chm25Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt10.bin"));
                L2Chm25Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt11.bin"));
                L2Chm25Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt12.bin"));
                L2Chm25Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt13.bin"));
                L2Chm25Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt14.bin"));
                L2Chm25Alt15 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt15.bin"));
                L2Chm25Alt16 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt16.bin"));
                L2Chm25Alt17 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt17.bin"));
                L2Chm25Alt18 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt18.bin"));
                L2Chm25Alt19 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt19.bin"));
                L2Chm25Alt20 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2Chm25Alt20.bin"));

                L3Chm1Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm1Default.bin"));

                L3Chm3Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm3Default.bin"));
                L3Chm3Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm3Alt1.bin"));
                L3Chm3Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm3Alt2.bin"));

                L3Chm5Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm5Default.bin"));
                L3Chm5Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm5Alt1.bin"));
                L3Chm5Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm5Alt2.bin"));
                L3Chm5Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm5Alt3.bin"));
                L3Chm5Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm5Alt4.bin"));

                L3Chm7Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm7Default.bin"));
                L3Chm7Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm7Alt1.bin"));
                L3Chm7Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm7Alt2.bin"));
                L3Chm7Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm7Alt3.bin"));
                L3Chm7Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm7Alt4.bin"));
                L3Chm7Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm7Alt5.bin"));
                L3Chm7Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm7Alt6.bin"));

                L3Chm9Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Default.bin"));
                L3Chm9Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Alt1.bin"));
                L3Chm9Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Alt2.bin"));
                L3Chm9Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Alt3.bin"));
                L3Chm9Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Alt4.bin"));
                L3Chm9Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Alt5.bin"));
                L3Chm9Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Alt6.bin"));
                L3Chm9Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm9Alt7.bin"));

                L3Chm11Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Default.bin"));
                L3Chm11Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt1.bin"));
                L3Chm11Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt2.bin"));
                L3Chm11Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt3.bin"));
                L3Chm11Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt4.bin"));
                L3Chm11Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt5.bin"));
                L3Chm11Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt6.bin"));
                L3Chm11Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt7.bin"));
                L3Chm11Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt8.bin"));
                L3Chm11Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt9.bin"));
                L3Chm11Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm11Alt10.bin"));

                L3Chm13Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Default.bin"));
                L3Chm13Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt1.bin"));
                L3Chm13Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt2.bin"));
                L3Chm13Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt3.bin"));
                L3Chm13Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt4.bin"));
                L3Chm13Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt5.bin"));
                L3Chm13Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt6.bin"));
                L3Chm13Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt7.bin"));
                L3Chm13Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt8.bin"));
                L3Chm13Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt9.bin"));
                L3Chm13Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt10.bin"));
                L3Chm13Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt11.bin"));
                L3Chm13Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt12.bin"));
                L3Chm13Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt13.bin"));
                L3Chm13Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm13Alt14.bin"));

                L3Chm15Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Default.bin"));
                L3Chm15Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt1.bin"));
                L3Chm15Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt2.bin"));
                L3Chm15Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt3.bin"));
                L3Chm15Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt4.bin"));
                L3Chm15Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt5.bin"));
                L3Chm15Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt6.bin"));
                L3Chm15Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt7.bin"));
                L3Chm15Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt8.bin"));
                L3Chm15Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt9.bin"));
                L3Chm15Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt10.bin"));
                L3Chm15Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt11.bin"));
                L3Chm15Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt12.bin"));
                L3Chm15Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt13.bin"));
                L3Chm15Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt14.bin"));
                L3Chm15Alt15 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt15.bin"));
                L3Chm15Alt16 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt16.bin"));
                L3Chm15Alt17 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm15Alt17.bin"));

                L3Chm17Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Default.bin"));
                L3Chm17Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt1.bin"));
                L3Chm17Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt2.bin"));
                L3Chm17Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt3.bin"));
                L3Chm17Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt4.bin"));
                L3Chm17Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt5.bin"));
                L3Chm17Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt6.bin"));
                L3Chm17Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt7.bin"));
                L3Chm17Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt8.bin"));
                L3Chm17Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt9.bin"));
                L3Chm17Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt10.bin"));
                L3Chm17Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt11.bin"));
                L3Chm17Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt12.bin"));
                L3Chm17Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt13.bin"));
                L3Chm17Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt14.bin"));
                L3Chm17Alt15 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt15.bin"));
                L3Chm17Alt16 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt16.bin"));
                L3Chm17Alt17 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt17.bin"));
                L3Chm17Alt18 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt18.bin"));
                L3Chm17Alt19 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm17Alt19.bin"));

                L3Chm19Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Default.bin"));
                L3Chm19Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt1.bin"));
                L3Chm19Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt2.bin"));
                L3Chm19Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt3.bin"));
                L3Chm19Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt4.bin"));
                L3Chm19Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt5.bin"));
                L3Chm19Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt6.bin"));
                L3Chm19Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt7.bin"));
                L3Chm19Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt8.bin"));
                L3Chm19Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt9.bin"));
                L3Chm19Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt10.bin"));
                L3Chm19Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt11.bin"));
                L3Chm19Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt12.bin"));
                L3Chm19Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt13.bin"));
                L3Chm19Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt14.bin"));
                L3Chm19Alt15 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt15.bin"));
                L3Chm19Alt16 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt16.bin"));
                L3Chm19Alt17 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt17.bin"));
                L3Chm19Alt18 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt18.bin"));
                L3Chm19Alt19 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt19.bin"));
                L3Chm19Alt20 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt20.bin"));
                L3Chm19Alt21 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt21.bin"));
                L3Chm19Alt22 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt22.bin"));
                L3Chm19Alt23 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt23.bin"));
                L3Chm19Alt24 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt24.bin"));
                L3Chm19Alt25 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm19Alt25.bin"));

                L3Chm21Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Default.bin"));
                L3Chm21Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt1.bin"));
                L3Chm21Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt2.bin"));
                L3Chm21Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt3.bin"));
                L3Chm21Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt4.bin"));
                L3Chm21Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt5.bin"));
                L3Chm21Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt6.bin"));
                L3Chm21Alt7 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt7.bin"));
                L3Chm21Alt8 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt8.bin"));
                L3Chm21Alt9 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt9.bin"));
                L3Chm21Alt10 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt10.bin"));
                L3Chm21Alt11 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt11.bin"));
                L3Chm21Alt12 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt12.bin"));
                L3Chm21Alt13 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt13.bin"));
                L3Chm21Alt14 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt14.bin"));
                L3Chm21Alt15 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt15.bin"));
                L3Chm21Alt16 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt16.bin"));
                L3Chm21Alt17 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt17.bin"));
                L3Chm21Alt18 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt18.bin"));
                L3Chm21Alt19 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt19.bin"));
                L3Chm21Alt20 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt20.bin"));
                L3Chm21Alt21 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt21.bin"));
                L3Chm21Alt22 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3Chm21Alt22.bin"));

                L2She3Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She3Default.bin"));
                L2She3Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She3Alt1.bin"));
                L2She5Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She5Default.bin"));
                L2She5Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She5Alt1.bin"));
                L2She5Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She5Alt2.bin"));
                L2She7Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She7Default.bin"));
                L2She7Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She7Alt1.bin"));
                L2She9Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She9Default.bin"));
                L2She9Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She9Alt1.bin"));
                L2She9Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She9Alt2.bin"));
                L2She9Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She9Alt3.bin"));
                L2She11Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She11Default.bin"));
                L2She11Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She11Alt1.bin"));
                L2She11Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She11Alt2.bin"));
                L2She11Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She11Alt3.bin"));
                L2She13Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She13Default.bin"));
                L2She13Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She13Alt1.bin"));
                L2She13Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She13Alt2.bin"));
                L2She13Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She13Alt3.bin"));
                L2She15Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She15Default.bin"));
                L2She15Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She15Alt1.bin"));
                L2She15Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She15Alt2.bin"));
                L2She15Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She15Alt3.bin"));
                L2She17Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She17Default.bin"));
                L2She17Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She17Alt1.bin"));
                L2She17Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She17Alt2.bin"));
                L2She17Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She17Alt3.bin"));
                L2She17Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She17Alt4.bin"));
                L2She17Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She17Alt5.bin"));
                L2She17Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She17Alt6.bin"));
                L2She19Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She19Default.bin"));
                L2She19Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She19Alt1.bin"));
                L2She19Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She19Alt2.bin"));
                L2She19Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She19Alt3.bin"));
                L2She19Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She19Alt4.bin"));
                L2She19Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She19Alt5.bin"));
                L2She19Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She19Alt6.bin"));
                L2She21Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She21Default.bin"));
                L2She21Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She21Alt1.bin"));
                L2She21Alt2 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She21Alt2.bin"));
                L2She21Alt3 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She21Alt3.bin"));
                L2She21Alt4 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She21Alt4.bin"));
                L2She21Alt5 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She21Alt5.bin"));
                L2She21Alt6 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L2She21Alt6.bin"));

                L3She1Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She1Default.bin"));
                L3She3Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She3Default.bin"));
                L3She3Alt1 = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She3Alt1.bin"));
                L3She5Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She5Default.bin"));
                L3She7Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She7Default.bin"));
                L3She9Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She9Default.bin"));
                L3She11Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She11Default.bin"));
                L3She13Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She13Default.bin"));
                L3She15Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She15Default.bin"));
                L3She17Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She17Default.bin"));
                L3She19Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She19Default.bin"));
                L3She21Default = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("VvvfSimulator.Vvvf.SwitchAngle.L3She21Default.bin"));
                Loaded = true;
            });
            t.Wait();
            Loading = false;
        }

        public static CustomPwm? GetCustomPwm(int Level, PulseTypeName PulseType, int PulseCount, PulseAlternative Alternative, 
            Model.Struct.Domain Domain, int Phase, double InitialPhase)
        {
            double sin_freq = Domain.GetBaseWaveFrequency();
            double Amplitude = Domain.ElectricalState.BaseWaveAmplitude.Value;
            (double SineX, _) = Calculation.Common.GetBaseWaveParameter(Domain, Phase, InitialPhase);

            return Level switch
            {
                2 => PulseType switch
                {
                    PulseTypeName.SHE => PulseCount switch
                    {
                        21 => Alternative switch
                        {
                            PulseAlternative.Default => L2She21Default,
                            PulseAlternative.Alt1 => L2She21Alt1,
                            PulseAlternative.Alt2 => L2She21Alt2,
                            PulseAlternative.Alt3 => L2She21Alt3,
                            PulseAlternative.Alt4 => L2She21Alt4,
                            PulseAlternative.Alt5 => L2She21Alt5,
                            PulseAlternative.Alt6 => L2She21Alt6,
                            _ => null,
                        },
                        19 => Alternative switch
                        {
                            PulseAlternative.Default => L2She19Default,
                            PulseAlternative.Alt1 => L2She19Alt1,
                            PulseAlternative.Alt2 => L2She19Alt2,
                            PulseAlternative.Alt3 => L2She19Alt3,
                            PulseAlternative.Alt4 => L2She19Alt4,
                            PulseAlternative.Alt5 => L2She19Alt5,
                            PulseAlternative.Alt6 => L2She19Alt6,
                            _ => null,
                        },
                        17 => Alternative switch
                        {
                            PulseAlternative.Default => L2She17Default,
                            PulseAlternative.Alt1 => L2She17Alt1,
                            PulseAlternative.Alt2 => L2She17Alt2,
                            PulseAlternative.Alt3 => L2She17Alt3,
                            PulseAlternative.Alt4 => L2She17Alt4,
                            PulseAlternative.Alt5 => L2She17Alt5,
                            PulseAlternative.Alt6 => L2She17Alt6,
                            _ => null,
                        },
                        15 => Alternative switch
                        {
                            PulseAlternative.Default => L2She15Default,
                            PulseAlternative.Alt1 => L2She15Alt1,
                            PulseAlternative.Alt2 => L2She15Alt2,
                            PulseAlternative.Alt3 => L2She15Alt3,
                            _ => null,
                        },
                        13 => Alternative switch
                        {
                            PulseAlternative.Default => L2She13Default,
                            PulseAlternative.Alt1 => L2She13Alt1,
                            PulseAlternative.Alt2 => L2She13Alt2,
                            PulseAlternative.Alt3 => L2She13Alt3,
                            _ => null,
                        },
                        11 => Alternative switch
                        {
                            PulseAlternative.Default => L2She11Default,
                            PulseAlternative.Alt1 => L2She11Alt1,
                            PulseAlternative.Alt2 => L2She11Alt2,
                            PulseAlternative.Alt3 => L2She11Alt3,
                            _ => null,
                        },
                        9 => Alternative switch
                        {
                            PulseAlternative.Default => L2She9Default,
                            PulseAlternative.Alt1 => L2She9Alt1,
                            PulseAlternative.Alt2 => L2She9Alt2,
                            PulseAlternative.Alt3 => L2She9Alt3,
                            _ => null,
                        },
                        7 => Alternative switch
                        {
                            PulseAlternative.Default => L2She7Default,
                            PulseAlternative.Alt1 => L2She7Alt1,
                            _ => null,
                        },
                        5 => Alternative switch
                        {
                            PulseAlternative.Default => L2She5Default,
                            PulseAlternative.Alt1 => L2She5Alt1,
                            PulseAlternative.Alt2 => L2She5Alt2,
                            _ => null,
                        },
                        3 => Alternative switch
                        {
                            PulseAlternative.Default => L2She3Default,
                            PulseAlternative.Alt1 => L2She3Alt1,
                            _ => null,
                        },
                        _ => null,
                    },
                    PulseTypeName.CHM => PulseCount switch
                    {
                        25 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm25Default,
                            PulseAlternative.Alt1 => L2Chm25Alt1,
                            PulseAlternative.Alt2 => L2Chm25Alt2,
                            PulseAlternative.Alt3 => L2Chm25Alt3,
                            PulseAlternative.Alt4 => L2Chm25Alt4,
                            PulseAlternative.Alt5 => L2Chm25Alt5,
                            PulseAlternative.Alt6 => L2Chm25Alt6,
                            PulseAlternative.Alt7 => L2Chm25Alt7,
                            PulseAlternative.Alt8 => L2Chm25Alt8,
                            PulseAlternative.Alt9 => L2Chm25Alt9,
                            PulseAlternative.Alt10 => L2Chm25Alt10,
                            PulseAlternative.Alt11 => L2Chm25Alt11,
                            PulseAlternative.Alt12 => L2Chm25Alt12,
                            PulseAlternative.Alt13 => L2Chm25Alt13,
                            PulseAlternative.Alt14 => L2Chm25Alt14,
                            PulseAlternative.Alt15 => L2Chm25Alt15,
                            PulseAlternative.Alt16 => L2Chm25Alt16,
                            PulseAlternative.Alt17 => L2Chm25Alt17,
                            PulseAlternative.Alt18 => L2Chm25Alt18,
                            PulseAlternative.Alt19 => L2Chm25Alt19,
                            PulseAlternative.Alt20 => L2Chm25Alt20,
                            _ => null
                        },
                        23 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm23Default,
                            PulseAlternative.Alt1 => L2Chm23Alt1,
                            PulseAlternative.Alt2 => L2Chm23Alt2,
                            PulseAlternative.Alt3 => L2Chm23Alt3,
                            PulseAlternative.Alt4 => L2Chm23Alt4,
                            PulseAlternative.Alt5 => L2Chm23Alt5,
                            PulseAlternative.Alt6 => L2Chm23Alt6,
                            PulseAlternative.Alt7 => L2Chm23Alt7,
                            PulseAlternative.Alt8 => L2Chm23Alt8,
                            PulseAlternative.Alt9 => L2Chm23Alt9,
                            PulseAlternative.Alt10 => L2Chm23Alt10,
                            PulseAlternative.Alt11 => L2Chm23Alt11,
                            PulseAlternative.Alt12 => L2Chm23Alt12,
                            PulseAlternative.Alt13 => L2Chm23Alt13,
                            PulseAlternative.Alt14 => L2Chm23Alt14,
                            _ => null
                        },
                        21 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm21Default,
                            PulseAlternative.Alt1 => L2Chm21Alt1,
                            PulseAlternative.Alt2 => L2Chm21Alt2,
                            PulseAlternative.Alt3 => L2Chm21Alt3,
                            PulseAlternative.Alt4 => L2Chm21Alt4,
                            PulseAlternative.Alt5 => L2Chm21Alt5,
                            PulseAlternative.Alt6 => L2Chm21Alt6,
                            PulseAlternative.Alt7 => L2Chm21Alt7,
                            PulseAlternative.Alt8 => L2Chm21Alt8,
                            PulseAlternative.Alt9 => L2Chm21Alt9,
                            PulseAlternative.Alt10 => L2Chm21Alt10,
                            PulseAlternative.Alt11 => L2Chm21Alt11,
                            PulseAlternative.Alt12 => L2Chm21Alt12,
                            PulseAlternative.Alt13 => L2Chm21Alt13,
                            _ => null
                        },
                        19 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm19Default,
                            PulseAlternative.Alt1 => L2Chm19Alt1,
                            PulseAlternative.Alt2 => L2Chm19Alt2,
                            PulseAlternative.Alt3 => L2Chm19Alt3,
                            PulseAlternative.Alt4 => L2Chm19Alt4,
                            PulseAlternative.Alt5 => L2Chm19Alt5,
                            PulseAlternative.Alt6 => L2Chm19Alt6,
                            PulseAlternative.Alt7 => L2Chm19Alt7,
                            PulseAlternative.Alt8 => L2Chm19Alt8,
                            PulseAlternative.Alt9 => L2Chm19Alt9,
                            PulseAlternative.Alt10 => L2Chm19Alt10,
                            PulseAlternative.Alt11 => L2Chm19Alt11,
                            _ => null
                        },
                        17 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm17Default,
                            PulseAlternative.Alt1 => L2Chm17Alt1,
                            PulseAlternative.Alt2 => L2Chm17Alt2,
                            PulseAlternative.Alt3 => L2Chm17Alt3,
                            PulseAlternative.Alt4 => L2Chm17Alt4,
                            PulseAlternative.Alt5 => L2Chm17Alt5,
                            PulseAlternative.Alt6 => L2Chm17Alt6,
                            PulseAlternative.Alt7 => L2Chm17Alt7,
                            PulseAlternative.Alt8 => L2Chm17Alt8,
                            PulseAlternative.Alt9 => L2Chm17Alt9,
                            PulseAlternative.Alt10 => L2Chm17Alt10,
                            PulseAlternative.Alt11 => L2Chm17Alt11,
                            _ => null
                        },
                        15 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm15Default,
                            PulseAlternative.Alt1 => WrapSelectedPreset(SelectL2Chm15Alt1(Domain)),
                            PulseAlternative.Alt2 => L2Chm15Alt2,
                            PulseAlternative.Alt3 => L2Chm15Alt3,
                            PulseAlternative.Alt4 => L2Chm15Alt4,
                            PulseAlternative.Alt5 => L2Chm15Alt5,
                            PulseAlternative.Alt6 => L2Chm15Alt6,
                            PulseAlternative.Alt7 => L2Chm15Alt7,
                            PulseAlternative.Alt8 => L2Chm15Alt8,
                            PulseAlternative.Alt9 => L2Chm15Alt9,
                            PulseAlternative.Alt10 => L2Chm15Alt10,
                            PulseAlternative.Alt11 => L2Chm15Alt11,
                            PulseAlternative.Alt12 => L2Chm15Alt12,
                            PulseAlternative.Alt13 => L2Chm15Alt13,
                            PulseAlternative.Alt14 => L2Chm15Alt14,
                            PulseAlternative.Alt15 => L2Chm15Alt15,
                            PulseAlternative.Alt16 => L2Chm15Alt16,
                            PulseAlternative.Alt17 => L2Chm15Alt17,
                            PulseAlternative.Alt18 => L2Chm15Alt18,
                            PulseAlternative.Alt19 => L2Chm15Alt19,
                            PulseAlternative.Alt20 => L2Chm15Alt20,
                            PulseAlternative.Alt21 => L2Chm15Alt21,
                            PulseAlternative.Alt22 => L2Chm15Alt22,
                            PulseAlternative.Alt23 => L2Chm15Alt23,
                            _ => null
                        },
                        13 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm13Default,
                            PulseAlternative.Alt1 => L2Chm13Alt1,
                            PulseAlternative.Alt2 => L2Chm13Alt2,
                            PulseAlternative.Alt3 => L2Chm13Alt3,
                            PulseAlternative.Alt4 => L2Chm13Alt4,
                            PulseAlternative.Alt5 => L2Chm13Alt5,
                            PulseAlternative.Alt6 => L2Chm13Alt6,
                            PulseAlternative.Alt7 => L2Chm13Alt7,
                            PulseAlternative.Alt8 => L2Chm13Alt8,
                            PulseAlternative.Alt9 => L2Chm13Alt9,
                            PulseAlternative.Alt10 => L2Chm13Alt10,
                            PulseAlternative.Alt11 => L2Chm13Alt11,
                            PulseAlternative.Alt12 => L2Chm13Alt12,
                            PulseAlternative.Alt13 => L2Chm13Alt13,
                            _ => null
                        },
                        11 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm11Default,
                            PulseAlternative.Alt1 => WrapSelectedPreset(SelectL2Chm11Alt1(Domain)),
                            PulseAlternative.Alt2 => L2Chm11Alt2,
                            PulseAlternative.Alt3 => WrapSelectedPreset(SelectL2Chm11Alt3(Domain)),
                            PulseAlternative.Alt4 => L2Chm11Alt4,
                            PulseAlternative.Alt5 => L2Chm11Alt5,
                            PulseAlternative.Alt6 => L2Chm11Alt6,
                            PulseAlternative.Alt7 => L2Chm11Alt7,
                            PulseAlternative.Alt8 => L2Chm11Alt8,
                            PulseAlternative.Alt9 => L2Chm11Alt9,
                            PulseAlternative.Alt10 => L2Chm11Alt10,
                            PulseAlternative.Alt11 => L2Chm11Alt11,
                            _ => null
                        },
                        9 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm9Default,
                            PulseAlternative.Alt1 => WrapSelectedPreset(SelectL2Chm9Alt1(Domain)),
                            PulseAlternative.Alt2 => L2Chm9Alt2,
                            PulseAlternative.Alt3 => L2Chm9Alt3,
                            PulseAlternative.Alt4 => L2Chm9Alt4,
                            PulseAlternative.Alt5 => L2Chm9Alt5,
                            PulseAlternative.Alt6 => L2Chm9Alt6,
                            PulseAlternative.Alt7 => L2Chm9Alt7,
                            PulseAlternative.Alt8 => L2Chm9Alt8,
                            _ => null
                        },
                        7 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm7Default,
                            PulseAlternative.Alt1 => WrapSelectedPreset(SelectL2Chm7Alt1(Domain)),
                            PulseAlternative.Alt2 => L2Chm7Alt2,
                            PulseAlternative.Alt3 => L2Chm7Alt3,
                            PulseAlternative.Alt4 => L2Chm7Alt4,
                            PulseAlternative.Alt5 => L2Chm7Alt5,
                            _ => null
                        },
                        5 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm5Default,
                            PulseAlternative.Alt1 => L2Chm5Alt1,
                            PulseAlternative.Alt2 => L2Chm5Alt2,
                            PulseAlternative.Alt3 => L2Chm5Alt3,
                            _ => null
                        },
                        3 => Alternative switch
                        {
                            PulseAlternative.Default => L2Chm3Default,
                            PulseAlternative.Alt1 => L2Chm3Alt1,
                            PulseAlternative.Alt2 => L2Chm3Alt2,
                            _ => null
                        },
                        _ => null,
                    },
                    _ => null,
                },
                3 => PulseType switch
                {
                    PulseTypeName.SHE => PulseCount switch
                    {
                        21 => Alternative switch
                        {
                            PulseAlternative.Default => L3She21Default,
                            _ => null,
                        },
                        19 => Alternative switch
                        {
                            PulseAlternative.Default => L3She19Default,
                            _ => null,
                        },
                        17 => Alternative switch
                        {
                            PulseAlternative.Default => L3She17Default,
                            _ => null,
                        },
                        15 => Alternative switch
                        {
                            PulseAlternative.Default => L3She15Default,
                            _ => null,
                        },
                        13 => Alternative switch
                        {
                            PulseAlternative.Default => L3She13Default,
                            _ => null,
                        },
                        11 => Alternative switch
                        {
                            PulseAlternative.Default => L3She11Default,
                            _ => null,
                        },
                        9 => Alternative switch
                        {
                            PulseAlternative.Default => L3She9Default,
                            _ => null,
                        },
                        7 => Alternative switch
                        {
                            PulseAlternative.Default => L3She7Default,
                            _ => null,
                        },
                        5 => Alternative switch
                        {
                            PulseAlternative.Default => L3She5Default,
                            _ => null,
                        },
                        3 => Alternative switch
                        {
                            PulseAlternative.Default => L3She3Default,
                            PulseAlternative.Alt1 => L3She3Alt1,
                            _ => null,
                        },
                        1 => Alternative switch
                        {
                            PulseAlternative.Default => L3She1Default,
                            _ => null,
                        },
                        _ => null,
                    },
                    PulseTypeName.CHM => PulseCount switch
                    {
                        21 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm21Default,
                            PulseAlternative.Alt1 => L3Chm21Alt1,
                            PulseAlternative.Alt2 => L3Chm21Alt2,
                            PulseAlternative.Alt3 => L3Chm21Alt3,
                            PulseAlternative.Alt4 => L3Chm21Alt4,
                            PulseAlternative.Alt5 => L3Chm21Alt5,
                            PulseAlternative.Alt6 => L3Chm21Alt6,
                            PulseAlternative.Alt7 => L3Chm21Alt7,
                            PulseAlternative.Alt8 => L3Chm21Alt8,
                            PulseAlternative.Alt9 => L3Chm21Alt9,
                            PulseAlternative.Alt10 => L3Chm21Alt10,
                            PulseAlternative.Alt11 => L3Chm21Alt11,
                            PulseAlternative.Alt12 => L3Chm21Alt12,
                            PulseAlternative.Alt13 => L3Chm21Alt13,
                            PulseAlternative.Alt14 => L3Chm21Alt14,
                            PulseAlternative.Alt15 => L3Chm21Alt15,
                            PulseAlternative.Alt16 => L3Chm21Alt16,
                            PulseAlternative.Alt17 => L3Chm21Alt17,
                            PulseAlternative.Alt18 => L3Chm21Alt18,
                            PulseAlternative.Alt19 => L3Chm21Alt19,
                            PulseAlternative.Alt20 => L3Chm21Alt20,
                            PulseAlternative.Alt21 => L3Chm21Alt21,
                            PulseAlternative.Alt22 => L3Chm21Alt22,
                            _ => null,
                        },
                        19 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm19Default,
                            PulseAlternative.Alt1 => L3Chm19Alt1,
                            PulseAlternative.Alt2 => L3Chm19Alt2,
                            PulseAlternative.Alt3 => L3Chm19Alt3,
                            PulseAlternative.Alt4 => L3Chm19Alt4,
                            PulseAlternative.Alt5 => L3Chm19Alt5,
                            PulseAlternative.Alt6 => L3Chm19Alt6,
                            PulseAlternative.Alt7 => L3Chm19Alt7,
                            PulseAlternative.Alt8 => L3Chm19Alt8,
                            PulseAlternative.Alt9 => L3Chm19Alt9,
                            PulseAlternative.Alt10 => L3Chm19Alt10,
                            PulseAlternative.Alt11 => L3Chm19Alt11,
                            PulseAlternative.Alt12 => L3Chm19Alt12,
                            PulseAlternative.Alt13 => L3Chm19Alt13,
                            PulseAlternative.Alt14 => L3Chm19Alt14,
                            PulseAlternative.Alt15 => L3Chm19Alt15,
                            PulseAlternative.Alt16 => L3Chm19Alt16,
                            PulseAlternative.Alt17 => L3Chm19Alt17,
                            PulseAlternative.Alt18 => L3Chm19Alt18,
                            PulseAlternative.Alt19 => L3Chm19Alt19,
                            PulseAlternative.Alt20 => L3Chm19Alt20,
                            PulseAlternative.Alt21 => L3Chm19Alt21,
                            PulseAlternative.Alt22 => L3Chm19Alt22,
                            PulseAlternative.Alt23 => L3Chm19Alt23,
                            PulseAlternative.Alt24 => L3Chm19Alt24,
                            PulseAlternative.Alt25 => L3Chm19Alt25,
                            _ => null,
                        },
                        17 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm17Default,
                            PulseAlternative.Alt1 => L3Chm17Alt1,
                            PulseAlternative.Alt2 => L3Chm17Alt2,
                            PulseAlternative.Alt3 => L3Chm17Alt3,
                            PulseAlternative.Alt4 => L3Chm17Alt4,
                            PulseAlternative.Alt5 => L3Chm17Alt5,
                            PulseAlternative.Alt6 => L3Chm17Alt6,
                            PulseAlternative.Alt7 => L3Chm17Alt7,
                            PulseAlternative.Alt8 => L3Chm17Alt8,
                            PulseAlternative.Alt9 => L3Chm17Alt9,
                            PulseAlternative.Alt10 => L3Chm17Alt10,
                            PulseAlternative.Alt11 => L3Chm17Alt11,
                            PulseAlternative.Alt12 => L3Chm17Alt12,
                            PulseAlternative.Alt13 => L3Chm17Alt13,
                            PulseAlternative.Alt14 => L3Chm17Alt14,
                            PulseAlternative.Alt15 => L3Chm17Alt15,
                            PulseAlternative.Alt16 => L3Chm17Alt16,
                            PulseAlternative.Alt17 => L3Chm17Alt17,
                            PulseAlternative.Alt18 => L3Chm17Alt18,
                            PulseAlternative.Alt19 => L3Chm17Alt19,
                            _ => null
                        },
                        15 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm15Default,
                            PulseAlternative.Alt1 => L3Chm15Alt1,
                            PulseAlternative.Alt2 => L3Chm15Alt2,
                            PulseAlternative.Alt3 => L3Chm15Alt3,
                            PulseAlternative.Alt4 => L3Chm15Alt4,
                            PulseAlternative.Alt5 => L3Chm15Alt5,
                            PulseAlternative.Alt6 => L3Chm15Alt6,
                            PulseAlternative.Alt7 => L3Chm15Alt7,
                            PulseAlternative.Alt8 => L3Chm15Alt8,
                            PulseAlternative.Alt9 => L3Chm15Alt9,
                            PulseAlternative.Alt10 => L3Chm15Alt10,
                            PulseAlternative.Alt11 => L3Chm15Alt11,
                            PulseAlternative.Alt12 => L3Chm15Alt12,
                            PulseAlternative.Alt13 => L3Chm15Alt13,
                            PulseAlternative.Alt14 => L3Chm15Alt14,
                            PulseAlternative.Alt15 => L3Chm15Alt15,
                            PulseAlternative.Alt16 => L3Chm15Alt16,
                            PulseAlternative.Alt17 => L3Chm15Alt17,
                            _ => null
                        },
                        13 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm13Default,
                            PulseAlternative.Alt1 => L3Chm13Alt1,
                            PulseAlternative.Alt2 => L3Chm13Alt2,
                            PulseAlternative.Alt3 => L3Chm13Alt3,
                            PulseAlternative.Alt4 => L3Chm13Alt4,
                            PulseAlternative.Alt5 => L3Chm13Alt5,
                            PulseAlternative.Alt6 => L3Chm13Alt6,
                            PulseAlternative.Alt7 => L3Chm13Alt7,
                            PulseAlternative.Alt8 => L3Chm13Alt8,
                            PulseAlternative.Alt9 => L3Chm13Alt9,
                            PulseAlternative.Alt10 => L3Chm13Alt10,
                            PulseAlternative.Alt11 => L3Chm13Alt11,
                            PulseAlternative.Alt12 => L3Chm13Alt12,
                            PulseAlternative.Alt13 => L3Chm13Alt13,
                            PulseAlternative.Alt14 => L3Chm13Alt14,
                            _ => null
                        },
                        11 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm11Default,
                            PulseAlternative.Alt1 => L3Chm11Alt1,
                            PulseAlternative.Alt2 => L3Chm11Alt2,
                            PulseAlternative.Alt3 => L3Chm11Alt3,
                            PulseAlternative.Alt4 => L3Chm11Alt4,
                            PulseAlternative.Alt5 => L3Chm11Alt5,
                            PulseAlternative.Alt6 => L3Chm11Alt6,
                            PulseAlternative.Alt7 => L3Chm11Alt7,
                            PulseAlternative.Alt8 => L3Chm11Alt8,
                            PulseAlternative.Alt9 => L3Chm11Alt9,
                            PulseAlternative.Alt10 => L3Chm11Alt10,
                            _ => null
                        },
                        9 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm9Default,
                            PulseAlternative.Alt1 => L3Chm9Alt1,
                            PulseAlternative.Alt2 => L3Chm9Alt2,
                            PulseAlternative.Alt3 => L3Chm9Alt3,
                            PulseAlternative.Alt4 => L3Chm9Alt4,
                            PulseAlternative.Alt5 => L3Chm9Alt5,
                            PulseAlternative.Alt6 => L3Chm9Alt6,
                            PulseAlternative.Alt7 => L3Chm9Alt7,
                            _ => null
                        },
                        7 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm7Default,
                            PulseAlternative.Alt1 => L3Chm7Alt1,
                            PulseAlternative.Alt2 => L3Chm7Alt2,
                            PulseAlternative.Alt3 => L3Chm7Alt3,
                            PulseAlternative.Alt4 => L3Chm7Alt4,
                            PulseAlternative.Alt5 => L3Chm7Alt5,
                            PulseAlternative.Alt6 => L3Chm7Alt6,
                            _ => null
                        },
                        5 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm5Default,
                            PulseAlternative.Alt1 => L3Chm5Alt1,
                            PulseAlternative.Alt2 => L3Chm5Alt2,
                            PulseAlternative.Alt3 => L3Chm5Alt3,
                            PulseAlternative.Alt4 => L3Chm5Alt4,
                            _ => null
                        },
                        3 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm3Default,
                            PulseAlternative.Alt1 => L3Chm3Alt1,
                            PulseAlternative.Alt2 => L3Chm3Alt2,
                            _ => null
                        },
                        1 => Alternative switch
                        {
                            PulseAlternative.Default => L3Chm1Default,
                            _ => null
                        },
                        _ => null,
                    },
                    _ => null,
                },
                _ => null,
            };
        }

        // Helper to wrap a preset with a current-amplitude wrapper when needed
        private static CustomPwm? WrapSelectedPreset((CustomPwm? Preset, double CurrentAmplitude) sel)
        {
            if (sel.Preset == null) return null;
            return new CurrentAmplitudeCustomPwm(sel.Preset, sel.CurrentAmplitude);
        }

        // Wrapper that enforces a specific amplitude when GetPwm is called
        private class CurrentAmplitudeCustomPwm : CustomPwm
        {
            private readonly CustomPwm _inner;
            private readonly double _currentAmp;

            public CurrentAmplitudeCustomPwm(CustomPwm inner, double currentAmp)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _currentAmp = currentAmp;
            }

            public override int GetPwm(double M, double X)
            {
                return _inner.GetPwm(_currentAmp, X);
            }
        }

        // Helper to safely get PWM from a preset (null -> 0)
        private static int EvalPreset(CustomPwm? p, double amp, double x)
        {
            if (p == null) return 0;
            return p.GetPwm(amp, x);
        }

        // New API: evaluate and return PWM value (int) directly, applying amplitude limits where necessary.
        // Falls back to existing GetCustomPwm when no special limiting is required.
        public static int GetCustomPwmValue(int Level, PulseTypeName PulseType, int PulseCount, PulseAlternative Alternative,
            Model.Struct.Domain Domain, int Phase, double InitialPhase)
        {
            double sin_freq = Domain.GetBaseWaveFrequency();
            double Amplitude = Domain.ElectricalState.BaseWaveAmplitude.Value;
            (double SineX, _) = Calculation.Common.GetBaseWaveParameter(Domain, Phase, InitialPhase);
            double SineXVal = SineX;

            // Helper to safely get PWM from a preset (null -> 0) is defined at type scope

            if (Level == 2 && PulseType == PulseTypeName.CHM)
            {
                var sel = (PulseCount, Alternative) switch
                {
                    (15, PulseAlternative.Alt1) => SelectL2Chm15Alt1(Domain),
                    (11, PulseAlternative.Alt1) => SelectL2Chm11Alt1(Domain),
                    (11, PulseAlternative.Alt3) => SelectL2Chm11Alt3(Domain),
                    (9, PulseAlternative.Alt1) => SelectL2Chm9Alt1(Domain),
                    (7, PulseAlternative.Alt1) => SelectL2Chm7Alt1(Domain),
                    _ => (Preset: (CustomPwm?)null, CurrentAmplitude: 0.0)
                };

                // sel may be null; EvalPreset handles null by returning 0
                return EvalPreset(sel.Preset, sel.CurrentAmplitude, SineXVal);
            }

            // Default behavior: use existing GetCustomPwm and evaluate its GetPwm
            CustomPwm? preset = GetCustomPwm(Level, PulseType, PulseCount, Alternative, Domain, Phase, InitialPhase);
            if (preset == null) return 0;
            return preset.GetPwm(Amplitude, SineXVal);
        }

        private static CustomPwm L2Chm3Default { get; set; } = new();
        private static CustomPwm L2Chm3Alt1 { get; set; } = new();
        private static CustomPwm L2Chm3Alt2 { get; set; } = new();

        private static CustomPwm L2Chm5Default { get; set; } = new();
        private static CustomPwm L2Chm5Alt1 { get; set; } = new();
        private static CustomPwm L2Chm5Alt2 { get; set; } = new();
        private static CustomPwm L2Chm5Alt3 { get; set; } = new();

        private static CustomPwm L2Chm7Default { get; set; } = new();
        private static CustomPwm L2Chm7Alt1 { get; set; } = new();
        private static CustomPwm L2Chm7Alt2 { get; set; } = new();
        private static CustomPwm L2Chm7Alt3 { get; set; } = new();
        private static CustomPwm L2Chm7Alt4 { get; set; } = new();
        private static CustomPwm L2Chm7Alt5 { get; set; } = new();

        private static CustomPwm L2Chm9Default { get; set; } = new();
        private static CustomPwm L2Chm9Alt1 { get; set; } = new();
        private static CustomPwm L2Chm9Alt2 { get; set; } = new();
        private static CustomPwm L2Chm9Alt3 { get; set; } = new();
        private static CustomPwm L2Chm9Alt4 { get; set; } = new();
        private static CustomPwm L2Chm9Alt5 { get; set; } = new();
        private static CustomPwm L2Chm9Alt6 { get; set; } = new();
        private static CustomPwm L2Chm9Alt7 { get; set; } = new();
        private static CustomPwm L2Chm9Alt8 { get; set; } = new();

        private static CustomPwm L2Chm11Default { get; set; } = new();
        private static CustomPwm L2Chm11Alt1 { get; set; } = new();
        private static CustomPwm L2Chm11Alt2 { get; set; } = new();
        private static CustomPwm L2Chm11Alt3 { get; set; } = new();
        private static CustomPwm L2Chm11Alt4 { get; set; } = new();
        private static CustomPwm L2Chm11Alt5 { get; set; } = new();
        private static CustomPwm L2Chm11Alt6 { get; set; } = new();
        private static CustomPwm L2Chm11Alt7 { get; set; } = new();
        private static CustomPwm L2Chm11Alt8 { get; set; } = new();
        private static CustomPwm L2Chm11Alt9 { get; set; } = new();
        private static CustomPwm L2Chm11Alt10 { get; set; } = new();
        private static CustomPwm L2Chm11Alt11 { get; set; } = new();

        private static CustomPwm L2Chm13Default { get; set; } = new();
        private static CustomPwm L2Chm13Alt1 { get; set; } = new();
        private static CustomPwm L2Chm13Alt2 { get; set; } = new();
        private static CustomPwm L2Chm13Alt3 { get; set; } = new();
        private static CustomPwm L2Chm13Alt4 { get; set; } = new();
        private static CustomPwm L2Chm13Alt5 { get; set; } = new();
        private static CustomPwm L2Chm13Alt6 { get; set; } = new();
        private static CustomPwm L2Chm13Alt7 { get; set; } = new();
        private static CustomPwm L2Chm13Alt8 { get; set; } = new();
        private static CustomPwm L2Chm13Alt9 { get; set; } = new();
        private static CustomPwm L2Chm13Alt10 { get; set; } = new();
        private static CustomPwm L2Chm13Alt11 { get; set; } = new();
        private static CustomPwm L2Chm13Alt12 { get; set; } = new();
        private static CustomPwm L2Chm13Alt13 { get; set; } = new();

        private static CustomPwm L2Chm15Default { get; set; } = new();
        private static CustomPwm L2Chm15Alt1 { get; set; } = new();
        private static CustomPwm L2Chm15Alt2 { get; set; } = new();
        private static CustomPwm L2Chm15Alt3 { get; set; } = new();
        private static CustomPwm L2Chm15Alt4 { get; set; } = new();
        private static CustomPwm L2Chm15Alt5 { get; set; } = new();
        private static CustomPwm L2Chm15Alt6 { get; set; } = new();
        private static CustomPwm L2Chm15Alt7 { get; set; } = new();
        private static CustomPwm L2Chm15Alt8 { get; set; } = new();
        private static CustomPwm L2Chm15Alt9 { get; set; } = new();
        private static CustomPwm L2Chm15Alt10 { get; set; } = new();
        private static CustomPwm L2Chm15Alt11 { get; set; } = new();
        private static CustomPwm L2Chm15Alt12 { get; set; } = new();
        private static CustomPwm L2Chm15Alt13 { get; set; } = new();
        private static CustomPwm L2Chm15Alt14 { get; set; } = new();
        private static CustomPwm L2Chm15Alt15 { get; set; } = new();
        private static CustomPwm L2Chm15Alt16 { get; set; } = new();
        private static CustomPwm L2Chm15Alt17 { get; set; } = new();
        private static CustomPwm L2Chm15Alt18 { get; set; } = new();
        private static CustomPwm L2Chm15Alt19 { get; set; } = new();
        private static CustomPwm L2Chm15Alt20 { get; set; } = new();
        private static CustomPwm L2Chm15Alt21 { get; set; } = new();
        private static CustomPwm L2Chm15Alt22 { get; set; } = new();
        private static CustomPwm L2Chm15Alt23 { get; set; } = new();

        private static (CustomPwm? Preset, double CurrentAmplitude) SelectL2Chm15Alt1(Model.Struct.Domain Domain)
        {
            double sin_freq = Domain.GetBaseWaveFrequency();
            double real_freq = Domain.GetRealBaseWaveFrequency();
            double Amplitude = Domain.ElectricalState.BaseWaveAmplitude.Value;
            bool chm9k = false;
            double w5w3 = chm9k ? GetChangingValue(55, 1.243, 80, 1.215, sin_freq, true) : GetChangingValue(45, 1.245, 80, 1.222, sin_freq, true);
            // Return the chosen preset and the current amplitude to apply
            if (real_freq < 390.0 / 15.0)
            {
                if (Amplitude < 0.07) return (L2Chm7Default, Amplitude);
                if (Amplitude < 0.11) return (L2Chm11Alt11, Amplitude);
                if (Amplitude < 0.31) return (L2Chm15Alt15, Amplitude);
                if (Amplitude < 1.23) return (L2Chm15Default, Amplitude);
                if (Amplitude < 1.245) return (L2Chm11Alt2, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (real_freq < 394.0 / 13.0)
            {
                if (Amplitude < 0.07) return (L2Chm5Default, Amplitude);
                if (Amplitude < 0.11) return (L2Chm9Alt8, Amplitude);
                if (Amplitude < 1.23) return (L2Chm13Default, Amplitude);
                if (Amplitude < 1.245) return (L2Chm9Alt2, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (real_freq < 397.0 / 11.0 && !chm9k)
            {
                if (Amplitude < 0.07) return (L2Chm5Default, Amplitude);
                if (Amplitude < 0.11) return (L2Chm7Alt5, Amplitude);
                if (Amplitude < 0.93) return (L2Chm11Alt9, Amplitude);
                if (Amplitude < 1.05) return (L2Chm11Alt7, Amplitude);
                if (Amplitude < 1.15) return (L2Chm11Alt3, Amplitude);
                if (Amplitude < 1.21) return (L2Chm11Alt1, Amplitude);
                if (Amplitude < 1.23) return (L2Chm11Alt2, Amplitude);
                if (Amplitude < 1.245) return (L2Chm7Alt2, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (real_freq < 400.0 / 9.0 && !chm9k)
            {
                if (Amplitude < 0.07) return (L2Chm3Default, Amplitude);
                if (Amplitude < 0.11) return (L2Chm7Alt5, Amplitude);
                if (Amplitude < 0.70) return (L2Chm9Alt8, Amplitude);
                if (Amplitude < 1.17) return (L2Chm9Default, Amplitude);
                if (Amplitude < 1.19) return (L2Chm9Alt2, Amplitude);
                if (Amplitude < w5w3) return (L2Chm5Alt1, Amplitude);
                if (Amplitude < 1.245) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (real_freq < 400.0 / 11.0 && chm9k)
            {
                if (Amplitude < 0.63) return (L2Chm11Alt9, Amplitude);
                return (L2Chm11Default, Amplitude);
            }
            if (real_freq < 400.0 / 9.0 && chm9k)
            {
                if (Amplitude < 0.05) return (L2Chm7Alt3, Amplitude);
                if (Amplitude < 0.73) return (L2Chm9Alt8, Amplitude);
                if (Amplitude < 0.89) return (L2Chm9Alt5, Amplitude);
                if (Amplitude < 1.17) return (L2Chm9Default, Amplitude);
                if (Amplitude < 1.19) return (L2Chm9Alt2, Amplitude);
                if (Amplitude < w5w3) return (L2Chm5Alt1, Amplitude);
                if (Amplitude < 1.245) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (real_freq < 400.0 / 7.0)
            {
                if (Amplitude < 0.07) return (L2Chm3Default, Amplitude);
                if (Amplitude < 0.11) return (L2Chm5Alt3, Amplitude);
                if (Amplitude < 0.86) return (L2Chm7Alt5, Amplitude);
                if (Amplitude < 1.10) return (L2Chm7Alt3, Amplitude);
                if (Amplitude < 1.14) return (L2Chm7Alt1, Amplitude);
                if (Amplitude < 1.19) return (L2Chm7Alt2, Amplitude);
                if (Amplitude < w5w3) return (L2Chm5Alt1, Amplitude);
                if (Amplitude < 1.245) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (real_freq < 400.0 / 5.0)
            {
                if (Amplitude < 0.10) return (L2Chm3Default, Amplitude);
                if (Amplitude < 0.96) return (L2Chm5Alt3, Amplitude);
                if (Amplitude < 1.12) return (L2Chm5Alt2, Amplitude);
                if (Amplitude < w5w3) return (L2Chm5Alt1, Amplitude);
                if (Amplitude < 1.245) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, GetChangingValue_Sin(GetChangingValue_Sin(0.8, 79.97, 35, 79.6, Math.Abs(Param.SmoothedFrequencyChangeRate), true), 1.245, 80, 1.27, real_freq, true));
            }
            if (real_freq < 400.0)
            {
                if (Amplitude < 1.195) return (L2Chm3Default, Amplitude);
                if (Amplitude < 1.265) return (L2Chm3Alt1, Amplitude);
                return (L2She3Alt1, 1.274);
            }

            return (L2She3Alt1, 1.274);
        }
        private static (CustomPwm? Preset, double CurrentAmplitude) SelectL2Chm11Alt1(Model.Struct.Domain Domain)
        {
            double sin_freq = Domain.GetBaseWaveFrequency();
            double Amplitude = Domain.ElectricalState.BaseWaveAmplitude.Value;
            bool accel = Domain.GetVoltageMultiply() >= 0;

            if (sin_freq < 500.0 / 11.0)
            {
                if (Amplitude < 0.93) return (L2Chm11Alt9, Amplitude);
                if (Amplitude < 1.06) return (L2Chm11Alt7, Amplitude);
                if (Amplitude < 1.10) return (L2Chm11Alt3, Amplitude);
                if (Amplitude < 1.245) return (L2Chm11Default, Amplitude);
                return (L2Chm11Default, 1.245);
            }
            if (sin_freq < 500.0 / 9.0)
            {
                if (Amplitude < 0.73) return (L2Chm9Alt8, Amplitude);
                if (Amplitude < 1.245) return (L2Chm9Default, Amplitude);
                return (L2Chm9Default, 1.245);
            }
            if (sin_freq < 500.0 / 7.0 && accel)
            {
                if (Amplitude < 1.245) return (L2Chm7Default, Amplitude);
                return (L2Chm7Default, 1.245);
            }
            if (sin_freq < 500.0 / 7.0 && !accel)
            {
                if (Amplitude < 1.245) return (L2Chm7Default, Amplitude);
                return (L2Chm3Default, 1.245);
            }
            if (sin_freq < 500.0 / 5.0 - 0.16 && accel)
            {
                if (Amplitude < 1.245) return (L2Chm5Default, Amplitude);
                return (L2Chm5Default, 1.245);
            }
            if (sin_freq < 500.0 / 5.0 - 0.08)
            {
                if (Amplitude < 1.245) return (L2Chm5Default, Amplitude);
                return (L2Chm3Default, 1.245);
            }
            if (sin_freq < 500.0 / 5.0 && accel)
            {
                if (Amplitude < 1.245) return (L2Chm5Default, Amplitude);
                return (L2Chm3Default, GetChangingValue(99.92, 1.245, 100.0, 1.27, sin_freq, true));
            }
            if (sin_freq < 500.0 / 5.0 && !accel)
            {
                if (Amplitude < 1.245) return (L2Chm5Default, Amplitude);
                return (L2Chm3Default, 1.245);
            }
            if (sin_freq < 500.0 / 5.0)
            {
                if (Amplitude < 1.245) return (L2Chm5Default, Amplitude);
                if (Amplitude < 1.265) return (L2Chm3Default, Amplitude);
                return (L2She3Alt1, 1.274);
            }
            if (sin_freq < 500.0 / 3.0)
            {
                if (Amplitude < 1.195) return (L2Chm3Alt2, Amplitude);
                if (Amplitude < 1.245) return (L2Chm3Alt1, Amplitude);
                return (L2She3Alt1, 1.274);
            }

            return (L2She3Alt1, 1.274);
        }
        private static (CustomPwm? Preset, double CurrentAmplitude) SelectL2Chm11Alt3(Model.Struct.Domain Domain)
        {
            double sin_freq = Domain.GetBaseWaveFrequency();
            double Amplitude = Domain.ElectricalState.BaseWaveAmplitude.Value;
            bool accel = Domain.GetVoltageMultiply() >= 0;
            double a75 = GetChangingValue_Pow(0, 0.02, 18, 0.27, 0.8, Param.SmoothedFrequencyChangeRate, true);
            double a53 = GetChangingValue_Pow(0, 0.01, 18, 0.25, 0.8, Param.SmoothedFrequencyChangeRate, true);

            if (sin_freq < 500.0 / 11.0)
            {
                if (Amplitude < 0.93) return (L2Chm11Alt9, Amplitude);
                if (Amplitude < 1.06) return (L2Chm11Alt7, Amplitude);
                if (Amplitude < 1.10) return (L2Chm11Alt3, Amplitude);
                if (Amplitude < 1.245) return (L2Chm11Default, Amplitude);
                return (L2Chm11Default, 1.245);
            }
            if (sin_freq < 500.0 / 9.0)
            {
                if (Amplitude < 0.73) return (L2Chm9Alt8, Amplitude);
                if (Amplitude < 1.245) return (L2Chm9Default, Amplitude);
                return (L2Chm9Default, 1.245);
            }
            if (sin_freq < 500.0 / 7.0 - a75 && accel)
            {
                if (Amplitude < 1.245) return (L2Chm7Default, Amplitude);
                return (L2Chm7Default, 1.245);
            }
            if (sin_freq < 500.0 / 7.0 - a53 && accel)
            {
                if (Amplitude < 1.245) return (L2Chm7Default, Amplitude);
                return (L2Chm5Default, 1.245);
            }
            if (sin_freq < 500.0 / 7.0 && accel)
            {
                if (Amplitude < 1.245) return (L2Chm7Default, Amplitude);
                return (L2Chm3Default, GetChangingValue(500.0/7.0-a53, 1.245, 500.0/7.0, 1.27, sin_freq, true));
            }
            if (sin_freq < 500.0 / 7.0 && !accel)
            {
                if (Amplitude < 1.245) return (L2Chm7Default, Amplitude);
                return (L2Chm3Default, 1.245);
            }
            if (sin_freq < 500.0 / 5.0)
            {
                if (Amplitude < 1.245) return (L2Chm5Default, Amplitude);
                if (Amplitude < 1.265) return (L2Chm3Default, Amplitude);
                return (L2She3Alt1, 1.274);
            }
            if (sin_freq < 500.0 / 3.0)
            {
                if (Amplitude < 1.265) return (L2Chm3Default, Amplitude);
                return (L2She3Alt1, 1.274);
            }

            return (L2She3Alt1, 1.274);
        }
        private static (CustomPwm? Preset, double CurrentAmplitude) SelectL2Chm9Alt1(Model.Struct.Domain Domain)
        {
            double sin_freq = Domain.GetBaseWaveFrequency();
            double Amplitude = Domain.ElectricalState.BaseWaveAmplitude.Value;
            bool accel = Domain.GetVoltageMultiply() >= 0;

            if (sin_freq < 500.0 / 9.0)
            {
                if (Amplitude < 0.73) return (L2Chm9Alt8, Amplitude);
                if (Amplitude < 1.246) return (L2Chm9Default, Amplitude);
                return (L2Chm5Default, 1.246);
            }
            if (sin_freq < 500.0 / 7.0 && accel)
            {
                if (Amplitude >= 1.246 && sin_freq < 60.5) return (L2Chm5Default, 1.246);
                return (L2Chm7Default, Amplitude > 1.246 ? 1.246 : Amplitude);
            }
            if (sin_freq < 500.0 / 7.0 && !accel)
            {
                if (Amplitude < 1.246) return (L2Chm7Default, Amplitude);
                return (L2Chm3Default, 1.246);
            }
            if (sin_freq < 86.8 && accel)
            {
                if (Amplitude < 1.246) return (L2Chm5Default, Amplitude);
                return (L2Chm5Default, 1.246);
            }
            if (sin_freq < 86.9 && accel)
            {
                if (Amplitude < 1.246) return (L2Chm5Default, Amplitude);
                return (L2Chm3Default, 1.246);
            }
            if (sin_freq < 87.0 && accel)
            {
                if (Amplitude < 1.246) return (L2Chm5Default, Amplitude);
                return (L2Chm3Default, GetChangingValue(86.9, 1.246, 87, 1.27, sin_freq, true));
            }
            if (sin_freq < 87.0 && !accel)
            {
                if (Amplitude < 1.246) return (L2Chm5Default, Amplitude);
                return (L2Chm3Default, 1.246);
            }
            if (sin_freq < 500.0 / 5.0)
            {
                if (Amplitude < 1.246) return (L2Chm5Default, Amplitude);
                if (Amplitude < 1.265) return (L2Chm3Default, Amplitude);
                return (L2She3Alt1, 1.274);
            }
            if (sin_freq < 500.0 / 3.0)
            {
                if (Amplitude < 1.195) return (L2Chm3Alt2, Amplitude);
                if (Amplitude < 1.246) return (L2Chm3Alt1, Amplitude);
                return (L2She3Alt1, 1.274);
            }

            return (L2She3Alt1, 1.274);
        }
        private static (CustomPwm? Preset, double CurrentAmplitude) SelectL2Chm7Alt1(Model.Struct.Domain Domain)
        {
            double sin_freq = Domain.GetBaseWaveFrequency();
            double Amplitude = Domain.ElectricalState.BaseWaveAmplitude.Value;
            double mf2 = 435;
            double a75 = GetChangingValue_Pow(mf2 / 9d, 1.208, mf2 / 7d, 1.204, 0.9, sin_freq, true);
            double a7_13 = GetChangingValue_Pow(51, 1.110, 60, 1.060, 0.9, sin_freq, true);
            double a53 = GetChangingValue_Pow(mf2 / 7d, 1.240, mf2 / 5d, 1.210, 1.3, sin_freq, true);
            double f8187 = GetChangingValue(1.245, 87, 1.8, 81, Amplitude, true);
            double f8791 = GetChangingValue(1.245, 91, 1.8, 87, Amplitude, true);
            double f9094 = GetChangingValue(1.245, 94, 1.8, 90, Amplitude, true);
            double f1 = GetChangingValue(f8187, 1.245, f8791, 1.265, sin_freq, true);

            if (sin_freq < mf2 / 9.0)
            {
                if (Amplitude < 0.880) return (L2Chm9Alt5, Amplitude);
                if (Amplitude < 1.030) return (L2Chm9Alt6, Amplitude);
                if (Amplitude < 1.110) return (L2Chm9Alt3, Amplitude);
                if (Amplitude <  a75 ) return (L2Chm9Alt2, Amplitude);
                if (Amplitude <  a53 ) return (L2Chm7Alt2, Amplitude);
                if (Amplitude < 1.245) return (L2Chm5Alt1, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (sin_freq < mf2 / 7.0)
            {
                if (Amplitude < 0.100) return (L2Chm5Alt3, Amplitude);
                if (Amplitude < 0.880) return (L2Chm7Alt5, Amplitude);
                if (Amplitude < a7_13) return (L2Chm7Alt3, Amplitude);
                if (Amplitude <  a75 ) return (L2Chm7Alt2, Amplitude);
                if (Amplitude <  a53 ) return (L2Chm5Alt1, Amplitude);
                if (Amplitude < 1.245) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (sin_freq < f8187)
            {
                if (Amplitude < 0.100) return (L2Chm3Alt2, Amplitude);
                if (Amplitude < 1.087) return (L2Chm5Alt3, Amplitude);
                if (Amplitude <  a53 ) return (L2Chm5Alt1, Amplitude);
                if (Amplitude < 1.245) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, 1.245);
            }
            if (sin_freq < f8791)
            {
                if (Amplitude < 1.195) return (L2Chm3Alt2, Amplitude);
                if (Amplitude <   f1 ) return (L2Chm3Alt1, Amplitude);
                if (Amplitude < 1.265) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, f1 > 1.265 ? 1.265 : f1);
            }
            if (sin_freq < f9094)
            {
                if (Amplitude < 1.195) return (L2Chm3Alt2, Amplitude);
                if (Amplitude < f1) return (L2Chm3Alt1, Amplitude);
                return (L2Chm3Alt1, 1.265);
            }
            if (sin_freq < 114514)
            {
                if (Amplitude < 1.195) return (L2Chm3Alt2, Amplitude);
                if (Amplitude < 1.265) return (L2Chm3Alt1, Amplitude);
                return (L2She3Alt1, 1.274);
            }

            return (L2She3Alt1, 1.274);
        }

        private static CustomPwm L2Chm17Default { get; set; } = new();
        private static CustomPwm L2Chm17Alt1 { get; set; } = new();
        private static CustomPwm L2Chm17Alt2 { get; set; } = new();
        private static CustomPwm L2Chm17Alt3 { get; set; } = new();
        private static CustomPwm L2Chm17Alt4 { get; set; } = new();
        private static CustomPwm L2Chm17Alt5 { get; set; } = new();
        private static CustomPwm L2Chm17Alt6 { get; set; } = new();
        private static CustomPwm L2Chm17Alt7 { get; set; } = new();
        private static CustomPwm L2Chm17Alt8 { get; set; } = new();
        private static CustomPwm L2Chm17Alt9 { get; set; } = new();
        private static CustomPwm L2Chm17Alt10 { get; set; } = new();
        private static CustomPwm L2Chm17Alt11 { get; set; } = new();

        private static CustomPwm L2Chm19Default { get; set; } = new();
        private static CustomPwm L2Chm19Alt1 { get; set; } = new();
        private static CustomPwm L2Chm19Alt2 { get; set; } = new();
        private static CustomPwm L2Chm19Alt3 { get; set; } = new();
        private static CustomPwm L2Chm19Alt4 { get; set; } = new();
        private static CustomPwm L2Chm19Alt5 { get; set; } = new();
        private static CustomPwm L2Chm19Alt6 { get; set; } = new();
        private static CustomPwm L2Chm19Alt7 { get; set; } = new();
        private static CustomPwm L2Chm19Alt8 { get; set; } = new();
        private static CustomPwm L2Chm19Alt9 { get; set; } = new();
        private static CustomPwm L2Chm19Alt10 { get; set; } = new();
        private static CustomPwm L2Chm19Alt11 { get; set; } = new();

        private static CustomPwm L2Chm21Default { get; set; } = new();
        private static CustomPwm L2Chm21Alt1 { get; set; } = new();
        private static CustomPwm L2Chm21Alt2 { get; set; } = new();
        private static CustomPwm L2Chm21Alt3 { get; set; } = new();
        private static CustomPwm L2Chm21Alt4 { get; set; } = new();
        private static CustomPwm L2Chm21Alt5 { get; set; } = new();
        private static CustomPwm L2Chm21Alt6 { get; set; } = new();
        private static CustomPwm L2Chm21Alt7 { get; set; } = new();
        private static CustomPwm L2Chm21Alt8 { get; set; } = new();
        private static CustomPwm L2Chm21Alt9 { get; set; } = new();
        private static CustomPwm L2Chm21Alt10 { get; set; } = new();
        private static CustomPwm L2Chm21Alt11 { get; set; } = new();
        private static CustomPwm L2Chm21Alt12 { get; set; } = new();
        private static CustomPwm L2Chm21Alt13 { get; set; } = new();

        private static CustomPwm L2Chm23Default { get; set; } = new();
        private static CustomPwm L2Chm23Alt1 { get; set; } = new();
        private static CustomPwm L2Chm23Alt2 { get; set; } = new();
        private static CustomPwm L2Chm23Alt3 { get; set; } = new();
        private static CustomPwm L2Chm23Alt4 { get; set; } = new();
        private static CustomPwm L2Chm23Alt5 { get; set; } = new();
        private static CustomPwm L2Chm23Alt6 { get; set; } = new();
        private static CustomPwm L2Chm23Alt7 { get; set; } = new();
        private static CustomPwm L2Chm23Alt8 { get; set; } = new();
        private static CustomPwm L2Chm23Alt9 { get; set; } = new();
        private static CustomPwm L2Chm23Alt10 { get; set; } = new();
        private static CustomPwm L2Chm23Alt11 { get; set; } = new();
        private static CustomPwm L2Chm23Alt12 { get; set; } = new();
        private static CustomPwm L2Chm23Alt13 { get; set; } = new();
        private static CustomPwm L2Chm23Alt14 { get; set; } = new();

        private static CustomPwm L2Chm25Default { get; set; } = new();
        private static CustomPwm L2Chm25Alt1 { get; set; } = new();
        private static CustomPwm L2Chm25Alt2 { get; set; } = new();
        private static CustomPwm L2Chm25Alt3 { get; set; } = new();
        private static CustomPwm L2Chm25Alt4 { get; set; } = new();
        private static CustomPwm L2Chm25Alt5 { get; set; } = new();
        private static CustomPwm L2Chm25Alt6 { get; set; } = new();
        private static CustomPwm L2Chm25Alt7 { get; set; } = new();
        private static CustomPwm L2Chm25Alt8 { get; set; } = new();
        private static CustomPwm L2Chm25Alt9 { get; set; } = new();
        private static CustomPwm L2Chm25Alt10 { get; set; } = new();
        private static CustomPwm L2Chm25Alt11 { get; set; } = new();
        private static CustomPwm L2Chm25Alt12 { get; set; } = new();
        private static CustomPwm L2Chm25Alt13 { get; set; } = new();
        private static CustomPwm L2Chm25Alt14 { get; set; } = new();
        private static CustomPwm L2Chm25Alt15 { get; set; } = new();
        private static CustomPwm L2Chm25Alt16 { get; set; } = new();
        private static CustomPwm L2Chm25Alt17 { get; set; } = new();
        private static CustomPwm L2Chm25Alt18 { get; set; } = new();
        private static CustomPwm L2Chm25Alt19 { get; set; } = new();
        private static CustomPwm L2Chm25Alt20 { get; set; } = new();

        private static CustomPwm L3Chm1Default { get; set; } = new();

        private static CustomPwm L3Chm3Default { get; set; } = new();
        private static CustomPwm L3Chm3Alt1 { get; set; } = new();
        private static CustomPwm L3Chm3Alt2 { get; set; } = new();

        private static CustomPwm L3Chm5Default { get; set; } = new();
        private static CustomPwm L3Chm5Alt1 { get; set; } = new();
        private static CustomPwm L3Chm5Alt2 { get; set; } = new();
        private static CustomPwm L3Chm5Alt3 { get; set; } = new();
        private static CustomPwm L3Chm5Alt4 { get; set; } = new();

        private static CustomPwm L3Chm7Default { get; set; } = new();
        private static CustomPwm L3Chm7Alt1 { get; set; } = new();
        private static CustomPwm L3Chm7Alt2 { get; set; } = new();
        private static CustomPwm L3Chm7Alt3 { get; set; } = new();
        private static CustomPwm L3Chm7Alt4 { get; set; } = new();
        private static CustomPwm L3Chm7Alt5 { get; set; } = new();
        private static CustomPwm L3Chm7Alt6 { get; set; } = new();

        private static CustomPwm L3Chm9Default { get; set; } = new();
        private static CustomPwm L3Chm9Alt1 { get; set; } = new();
        private static CustomPwm L3Chm9Alt2 { get; set; } = new();
        private static CustomPwm L3Chm9Alt3 { get; set; } = new();
        private static CustomPwm L3Chm9Alt4 { get; set; } = new();
        private static CustomPwm L3Chm9Alt5 { get; set; } = new();
        private static CustomPwm L3Chm9Alt6 { get; set; } = new();
        private static CustomPwm L3Chm9Alt7 { get; set; } = new();

        private static CustomPwm L3Chm11Default { get; set; } = new();
        private static CustomPwm L3Chm11Alt1 { get; set; } = new();
        private static CustomPwm L3Chm11Alt2 { get; set; } = new();
        private static CustomPwm L3Chm11Alt3 { get; set; } = new();
        private static CustomPwm L3Chm11Alt4 { get; set; } = new();
        private static CustomPwm L3Chm11Alt5 { get; set; } = new();
        private static CustomPwm L3Chm11Alt6 { get; set; } = new();
        private static CustomPwm L3Chm11Alt7 { get; set; } = new();
        private static CustomPwm L3Chm11Alt8 { get; set; } = new();
        private static CustomPwm L3Chm11Alt9 { get; set; } = new();
        private static CustomPwm L3Chm11Alt10 { get; set; } = new();

        private static CustomPwm L3Chm13Default { get; set; } = new();
        private static CustomPwm L3Chm13Alt1 { get; set; } = new();
        private static CustomPwm L3Chm13Alt2 { get; set; } = new();
        private static CustomPwm L3Chm13Alt3 { get; set; } = new();
        private static CustomPwm L3Chm13Alt4 { get; set; } = new();
        private static CustomPwm L3Chm13Alt5 { get; set; } = new();
        private static CustomPwm L3Chm13Alt6 { get; set; } = new();
        private static CustomPwm L3Chm13Alt7 { get; set; } = new();
        private static CustomPwm L3Chm13Alt8 { get; set; } = new();
        private static CustomPwm L3Chm13Alt9 { get; set; } = new();
        private static CustomPwm L3Chm13Alt10 { get; set; } = new();
        private static CustomPwm L3Chm13Alt11 { get; set; } = new();
        private static CustomPwm L3Chm13Alt12 { get; set; } = new();
        private static CustomPwm L3Chm13Alt13 { get; set; } = new();
        private static CustomPwm L3Chm13Alt14 { get; set; } = new();

        private static CustomPwm L3Chm15Default { get; set; } = new();
        private static CustomPwm L3Chm15Alt1 { get; set; } = new();
        private static CustomPwm L3Chm15Alt2 { get; set; } = new();
        private static CustomPwm L3Chm15Alt3 { get; set; } = new();
        private static CustomPwm L3Chm15Alt4 { get; set; } = new();
        private static CustomPwm L3Chm15Alt5 { get; set; } = new();
        private static CustomPwm L3Chm15Alt6 { get; set; } = new();
        private static CustomPwm L3Chm15Alt7 { get; set; } = new();
        private static CustomPwm L3Chm15Alt8 { get; set; } = new();
        private static CustomPwm L3Chm15Alt9 { get; set; } = new();
        private static CustomPwm L3Chm15Alt10 { get; set; } = new();
        private static CustomPwm L3Chm15Alt11 { get; set; } = new();
        private static CustomPwm L3Chm15Alt12 { get; set; } = new();
        private static CustomPwm L3Chm15Alt13 { get; set; } = new();
        private static CustomPwm L3Chm15Alt14 { get; set; } = new();
        private static CustomPwm L3Chm15Alt15 { get; set; } = new();
        private static CustomPwm L3Chm15Alt16 { get; set; } = new();
        private static CustomPwm L3Chm15Alt17 { get; set; } = new();

        private static CustomPwm L3Chm17Default { get; set; } = new();
        private static CustomPwm L3Chm17Alt1 { get; set; } = new();
        private static CustomPwm L3Chm17Alt2 { get; set; } = new();
        private static CustomPwm L3Chm17Alt3 { get; set; } = new();
        private static CustomPwm L3Chm17Alt4 { get; set; } = new();
        private static CustomPwm L3Chm17Alt5 { get; set; } = new();
        private static CustomPwm L3Chm17Alt6 { get; set; } = new();
        private static CustomPwm L3Chm17Alt7 { get; set; } = new();
        private static CustomPwm L3Chm17Alt8 { get; set; } = new();
        private static CustomPwm L3Chm17Alt9 { get; set; } = new();
        private static CustomPwm L3Chm17Alt10 { get; set; } = new();
        private static CustomPwm L3Chm17Alt11 { get; set; } = new();
        private static CustomPwm L3Chm17Alt12 { get; set; } = new();
        private static CustomPwm L3Chm17Alt13 { get; set; } = new();
        private static CustomPwm L3Chm17Alt14 { get; set; } = new();
        private static CustomPwm L3Chm17Alt15 { get; set; } = new();
        private static CustomPwm L3Chm17Alt16 { get; set; } = new();
        private static CustomPwm L3Chm17Alt17 { get; set; } = new();
        private static CustomPwm L3Chm17Alt18 { get; set; } = new();
        private static CustomPwm L3Chm17Alt19 { get; set; } = new();

        private static CustomPwm L3Chm19Default { get; set; } = new();
        private static CustomPwm L3Chm19Alt1 { get; set; } = new();
        private static CustomPwm L3Chm19Alt2 { get; set; } = new();
        private static CustomPwm L3Chm19Alt3 { get; set; } = new();
        private static CustomPwm L3Chm19Alt4 { get; set; } = new();
        private static CustomPwm L3Chm19Alt5 { get; set; } = new();
        private static CustomPwm L3Chm19Alt6 { get; set; } = new();
        private static CustomPwm L3Chm19Alt7 { get; set; } = new();
        private static CustomPwm L3Chm19Alt8 { get; set; } = new();
        private static CustomPwm L3Chm19Alt9 { get; set; } = new();
        private static CustomPwm L3Chm19Alt10 { get; set; } = new();
        private static CustomPwm L3Chm19Alt11 { get; set; } = new();
        private static CustomPwm L3Chm19Alt12 { get; set; } = new();
        private static CustomPwm L3Chm19Alt13 { get; set; } = new();
        private static CustomPwm L3Chm19Alt14 { get; set; } = new();
        private static CustomPwm L3Chm19Alt15 { get; set; } = new();
        private static CustomPwm L3Chm19Alt16 { get; set; } = new();
        private static CustomPwm L3Chm19Alt17 { get; set; } = new();
        private static CustomPwm L3Chm19Alt18 { get; set; } = new();
        private static CustomPwm L3Chm19Alt19 { get; set; } = new();
        private static CustomPwm L3Chm19Alt20 { get; set; } = new();
        private static CustomPwm L3Chm19Alt21 { get; set; } = new();
        private static CustomPwm L3Chm19Alt22 { get; set; } = new();
        private static CustomPwm L3Chm19Alt23 { get; set; } = new();
        private static CustomPwm L3Chm19Alt24 { get; set; } = new();
        private static CustomPwm L3Chm19Alt25 { get; set; } = new();

        private static CustomPwm L3Chm21Default { get; set; } = new();
        private static CustomPwm L3Chm21Alt1 { get; set; } = new();
        private static CustomPwm L3Chm21Alt2 { get; set; } = new();
        private static CustomPwm L3Chm21Alt3 { get; set; } = new();
        private static CustomPwm L3Chm21Alt4 { get; set; } = new();
        private static CustomPwm L3Chm21Alt5 { get; set; } = new();
        private static CustomPwm L3Chm21Alt6 { get; set; } = new();
        private static CustomPwm L3Chm21Alt7 { get; set; } = new();
        private static CustomPwm L3Chm21Alt8 { get; set; } = new();
        private static CustomPwm L3Chm21Alt9 { get; set; } = new();
        private static CustomPwm L3Chm21Alt10 { get; set; } = new();
        private static CustomPwm L3Chm21Alt11 { get; set; } = new();
        private static CustomPwm L3Chm21Alt12 { get; set; } = new();
        private static CustomPwm L3Chm21Alt13 { get; set; } = new();
        private static CustomPwm L3Chm21Alt14 { get; set; } = new();
        private static CustomPwm L3Chm21Alt15 { get; set; } = new();
        private static CustomPwm L3Chm21Alt16 { get; set; } = new();
        private static CustomPwm L3Chm21Alt17 { get; set; } = new();
        private static CustomPwm L3Chm21Alt18 { get; set; } = new();
        private static CustomPwm L3Chm21Alt19 { get; set; } = new();
        private static CustomPwm L3Chm21Alt20 { get; set; } = new();
        private static CustomPwm L3Chm21Alt21 { get; set; } = new();
        private static CustomPwm L3Chm21Alt22 { get; set; } = new();

        private static CustomPwm L2She3Default { get; set; } = new();
        private static CustomPwm L2She3Alt1 { get; set; } = new();
        private static CustomPwm L2She5Default { get; set; } = new();
        private static CustomPwm L2She5Alt1 { get; set; } = new();
        private static CustomPwm L2She5Alt2 { get; set; } = new();
        private static CustomPwm L2She7Default { get; set; } = new();
        private static CustomPwm L2She7Alt1 { get; set; } = new();
        private static CustomPwm L2She9Default { get; set; } = new();
        private static CustomPwm L2She9Alt1 { get; set; } = new();
        private static CustomPwm L2She9Alt2 { get; set; } = new();
        private static CustomPwm L2She9Alt3 { get; set; } = new();
        private static CustomPwm L2She11Default { get; set; } = new();
        private static CustomPwm L2She11Alt1 { get; set; } = new();
        private static CustomPwm L2She11Alt2 { get; set; } = new();
        private static CustomPwm L2She11Alt3 { get; set; } = new();
        private static CustomPwm L2She13Default { get; set; } = new();
        private static CustomPwm L2She13Alt1 { get; set; } = new();
        private static CustomPwm L2She13Alt2 { get; set; } = new();
        private static CustomPwm L2She13Alt3 { get; set; } = new();
        private static CustomPwm L2She15Default { get; set; } = new();
        private static CustomPwm L2She15Alt1 { get; set; } = new();
        private static CustomPwm L2She15Alt2 { get; set; } = new();
        private static CustomPwm L2She15Alt3 { get; set; } = new();
        private static CustomPwm L2She17Default { get; set; } = new();
        private static CustomPwm L2She17Alt1 { get; set; } = new();
        private static CustomPwm L2She17Alt2 { get; set; } = new();
        private static CustomPwm L2She17Alt3 { get; set; } = new();
        private static CustomPwm L2She17Alt4 { get; set; } = new();
        private static CustomPwm L2She17Alt5 { get; set; } = new();
        private static CustomPwm L2She17Alt6 { get; set; } = new();
        private static CustomPwm L2She19Default { get; set; } = new();
        private static CustomPwm L2She19Alt1 { get; set; } = new();
        private static CustomPwm L2She19Alt2 { get; set; } = new();
        private static CustomPwm L2She19Alt3 { get; set; } = new();
        private static CustomPwm L2She19Alt4 { get; set; } = new();
        private static CustomPwm L2She19Alt5 { get; set; } = new();
        private static CustomPwm L2She19Alt6 { get; set; } = new();
        private static CustomPwm L2She21Default { get; set; } = new();
        private static CustomPwm L2She21Alt1 { get; set; } = new();
        private static CustomPwm L2She21Alt2 { get; set; } = new();
        private static CustomPwm L2She21Alt3 { get; set; } = new();
        private static CustomPwm L2She21Alt4 { get; set; } = new();
        private static CustomPwm L2She21Alt5 { get; set; } = new();
        private static CustomPwm L2She21Alt6 { get; set; } = new();

        private static CustomPwm L3She1Default { get; set; } = new();
        private static CustomPwm L3She3Default { get; set; } = new();
        private static CustomPwm L3She3Alt1 { get; set; } = new();
        private static CustomPwm L3She5Default { get; set; } = new();
        private static CustomPwm L3She7Default { get; set; } = new();
        private static CustomPwm L3She9Default { get; set; } = new();
        private static CustomPwm L3She11Default { get; set; } = new();
        private static CustomPwm L3She13Default { get; set; } = new();
        private static CustomPwm L3She15Default { get; set; } = new();
        private static CustomPwm L3She17Default { get; set; } = new();
        private static CustomPwm L3She19Default { get; set; } = new();
        private static CustomPwm L3She21Default { get; set; } = new();

    }
}
