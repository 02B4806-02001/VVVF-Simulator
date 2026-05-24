using System;
using System.Collections.Generic;
using VvvfSimulator.GUI.Create.Settings;
using VvvfSimulator.Vvvf;
using VvvfSimulator.Vvvf.Model;
using VvvfSimulator.GUI.Simulator.RealTime.Controller.Design2;
using static VvvfSimulator.Vvvf.MyMath;
using static VvvfSimulator.Data.Vvvf.Struct;
using static VvvfSimulator.Data.Vvvf.Struct.JerkSettings;
using static VvvfSimulator.Data.Vvvf.Struct.PulseControl;
using static VvvfSimulator.Data.Vvvf.Struct.PulseControl.Pulse;
using static VvvfSimulator.Vvvf.Model.Struct;
using Jerk = VvvfSimulator.Data.Vvvf.Struct.JerkSettings.Jerk;

namespace VvvfSimulator.Data.Vvvf
{
    public class Analyze
    {
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
            double final = y1 + (y2 - y1) * Math.Pow((x - x1) / (x2 - x1), a);
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
            double final = -0.5 * ((y2 - y1) * Math.Cos(M_PI * (x - x1) / (x2 - x1)) - (y1 + y2));
            if (limit == true)
            {
                if (x < x1) return y1;
                else if (x > x2) return y2;
                else return final;
            }
            else return final;
        }
        private static double GetMovingValue(FunctionValue Data, double Current)
        {
            double val = 1000;
            if (Data.Type == FunctionValue.FunctionType.Proportional)
                val = GetChangingValue(
                    Data.Start,
                    Data.StartValue,
                    Data.End,
                    Data.EndValue,
                    Current,
                    false
                );
            else if (Data.Type == FunctionValue.FunctionType.Pow2_Exponential)
                val = (Math.Pow(2, Math.Pow((Current - Data.Start) / (Data.End - Data.Start), Data.Degree)) - 1) * (Data.EndValue - Data.StartValue) + Data.StartValue;
            else if (Data.Type == FunctionValue.FunctionType.Inv_Proportional)
            {
                double x = GetChangingValue(
                    Data.Start,
                    1 / Data.StartValue,
                    Data.End,
                    1 / Data.EndValue,
                    Current,
                    false
                );

                double c = -Data.CurveRate;
                double k = Data.EndValue;
                double l = Data.StartValue;
                double a = 1 / ((1 / l) - (1 / k)) * (1 / (l - c) - 1 / (k - c));
                double b = 1 / (1 - (1 / l) * k) * (1 / (l - c) - (1 / l) * k / (k - c));

                val = 1 / (a * x + b) + c;
            }
            else if (Data.Type == FunctionValue.FunctionType.Sine)
            {
                double x = (MyMath.M_PI_2 - Math.Asin(Data.StartValue / Data.EndValue)) / (Data.End - Data.Start) * (Current - Data.Start) + Math.Asin(Data.StartValue / Data.EndValue);
                val = Math.Sin(x) * Data.EndValue;
            }

            return val;

        }
        public static int GetPulseWithSwitchAngle(
            double alpha1,
            double alpha2,
            double alpha3,
            double alpha4,
            double alpha5,
            double alpha6,
            double alpha7,
            double alpha8,
            double alpha9,
            int flag,
            double time, double SineAngleFrequency, double InitialPhase)
        {
            static bool GetPulseWithSwitchAngle__Optim(double[] alpha, bool polary, double x)
            {
                double theta = Math.Abs(Functions.Saw(x)) * M_PI / 2;
                bool _switch;
                for (int i = 0; i < alpha.Length; i++)
                {
                    if (theta <= alpha[i])
                    {
                        _switch = i % 2 == 0;
                        _switch = polary ? !_switch : _switch;
                        _switch = x % M_2PI < M_PI ? _switch : !_switch;
                        return _switch;
                    }
                }
                _switch = alpha.Length % 2 == 0;
                _switch = polary ? !_switch : _switch;
                _switch = x % M_2PI < M_PI ? _switch : !_switch;
                return _switch;
            }
            double theta = (InitialPhase + time * SineAngleFrequency) - (double)((int)((InitialPhase + time * SineAngleFrequency) * M_1_2PI) * M_2PI);
            return GetPulseWithSwitchAngle__Optim([alpha1, alpha2, alpha3, alpha4, alpha5, alpha6, alpha7, alpha8, alpha9], flag != 65, theta) ? 2 : 0;
        }
        private static double GetAmplitude(AmplitudeValue.Parameter Param, double Current)
        {
            double Amplitude = 0;
            double _acc = Design2.Param.FrequencyChangeRate;
            if (Param.Mode == AmplitudeValue.Parameter.ValueMode.Table)
            {
                int TargetIndex = 0;
                for (int i = 0; i < Param.AmplitudeTable.Length; i++)
                {
                    if (Param.AmplitudeTable[i].Frequency > Current) break;
                    TargetIndex = i;
                }

                if (Param.AmplitudeTableInterpolation && (TargetIndex + 1 < Param.AmplitudeTable.Length))
                {
                    (double FrequencyStart, double AmplitudeStart) = Param.AmplitudeTable[TargetIndex];
                    (double FrequencyEnd, double AmplitudeEnd) = Param.AmplitudeTable[TargetIndex + 1];
                    double amp = (AmplitudeEnd - AmplitudeStart) / (FrequencyEnd - FrequencyStart) * (Current - FrequencyStart) + AmplitudeStart;
                    return amp * Design2.Param.Amplitude;
                }
                else
                    return Param.AmplitudeTable[TargetIndex].Amplitude * Design2.Param.Amplitude;
            }

            if (Param.EndAmplitude == Param.StartAmplitude) Amplitude = Param.StartAmplitude;
            else if (Param.Mode == AmplitudeValue.Parameter.ValueMode.Linear)
            {
                if (!Param.DisableRangeLimit)
                {
                    if (Current < Param.StartFrequency) Current = Param.StartFrequency;
                    if (Current > Param.EndFrequency) Current = Param.EndFrequency;
                }
                //double power_
                double amp = (Param.EndAmplitude - Param.StartAmplitude) / (Param.EndFrequency - Param.StartFrequency) * (Current - Param.StartFrequency) + Param.StartAmplitude;
                //double power = fix * _acc * amp > 0 ? fix * acc * amp : decel * fix * (acc-0.01*Current-0.8) * amp;
                //double _power = decel * fix * acc * amp - 0.006 * Current < 0 ? 0 : decel * fix * acc * amp - 0.006 * Current;
                //double _dtc = (amp-(power-(amp-power)*0.2+(power-(amp-power))))/((_acc>0?58:58)-25)*(Current-25)+power+0.002*Current;
                //double dtc = _dtc > amp ? amp : _dtc < power ? power : _dtc;
                double a = _acc > 0 ? Current / 56 + 0.03 : Current / 64 + 0.03;
                double b = _acc > 0 ? Current / 48 + 0.03 : Current / 55 + 0.03;
                double b1a = GetChangingValue(75, 1.165, 79, 1.215, Current, true);
                double b1b = GetChangingValue(75, 1.165, 78, 1.215, Current, true);
                double b2a = GetChangingValue(100, 1.215, 104, 1.27, Current, true);
                double b2b = GetChangingValue(101, 1.215, 104, 1.27, Current, true);
                double b3 = GetChangingValue(75, 0.011, 100, 0.0093, Current, true) * Current;
                double b4 = GetChangingValue(101, 0.0093, 104, 0.013, Current, true) * Current;
                double c = GetChangingValue(60, 0.017, 100, 0.012, Current, true) * Current;
                //Amplitude = -2.3 < _acc && _acc < 0.01 ? 0 : amp; //Default
                Amplitude = amp * Design2.Param.Amplitude; //Default
                //Amplitude = GetChangingValue(0, 0.03, 60, 1.245, Current, false) * Design2.Param.Amplitude; //Default2
                //Amplitude = Math.Abs(Design2.Param.VoltageMultiply) < 0.06 ? 0 : amp; //Default
                //Amplitude = GetChangingValue_Pow(0, 0.05, 70, 1.26, 1.5, Current, false) * Design2.Param.Control.GetAmp(); //BBD TC1110
                //Amplitude = (1.25 - 0.05) / (58 - 0) * (Current - 0) + 0;
                //Amplitude = -1.4 < _acc && _acc < 0 ? 0 : _acc <= -1.4 ? _power : power;
                //Amplitude = -(0.02*Current+1.8) < _acc && _acc < 0 ? 0 : power + 0.001 * Current; //Power
                //Amplitude = -0.0 < _acc && _acc < -0 ? 0 : power + 0.00 * Current; //Power
                /*{
                    if (Current <= (Design2.Param.FrequencyChangeRate > 0 ? 75 : 72))
                    {
                        if (GetYWTamp(Amplitude, Current) > GetYWTamp(a, Current)) Amplitude = GetYWTamp(a, Current);
                        else if (GetYWTamp(Amplitude, Current) < b3) Amplitude = GetYWTamp(b3, Current);
                        else Amplitude = GetYWTamp(Amplitude, Current);
                    }

                    else if (Current <= (Design2.Param.FrequencyChangeRate > 0 ? 100 : 101))
                    {
                        if (GetYWTamp(Amplitude, Current) > (Design2.Param.FrequencyChangeRate >= 0 ? b1a : b1b)) Amplitude = GetYWTamp((Design2.Param.FrequencyChangeRate >= 0 ? b1a : b1b), Current);
                        else if (GetYWTamp(Amplitude, Current) < b3) Amplitude = GetYWTamp(b3, Current);
                        else Amplitude = GetYWTamp(Amplitude, Current);
                    }

                    else
                    {
                        if (GetYWTamp(Amplitude, Current) > (Design2.Param.FrequencyChangeRate >= 0 ? b2a : b2b)) Amplitude = GetYWTamp((Design2.Param.FrequencyChangeRate >= 0 ? b2a : b2b), Current);
                        else if (GetYWTamp(Amplitude, Current) < b4) Amplitude = GetYWTamp(b4, Current);
                        else Amplitude = GetYWTamp(Amplitude, Current);
                    }
                }*/
                /*{
                    if (Amplitude > b) Amplitude = b;
                    if (Amplitude < c) Amplitude = c;
                }*/
                //Amplitude = dtc;
            }
            else if (Param.Mode == AmplitudeValue.Parameter.ValueMode.InverseProportional)
            {
                if (!Param.DisableRangeLimit)
                {
                    if (Current < Param.StartFrequency) Current = Param.StartFrequency;
                    if (Current > Param.EndFrequency) Current = Param.EndFrequency;
                }

                double x = (1.0 / Param.EndAmplitude - 1.0 / Param.StartAmplitude) / (Param.EndFrequency - Param.StartFrequency) * (Current - Param.StartFrequency) + 1.0 / Param.StartAmplitude;

                double c = -Param.CurveChangeRate;
                double k = Param.EndAmplitude;
                double l = Param.StartAmplitude;
                double a = 1 / (1 / l - 1 / k) * (1 / (l - c) - 1 / (k - c));
                double b = 1 / (1 - 1 / l * k) * (1 / (l - c) - 1 / l * k / (k - c));

                double amp = 1 / (a * x + b) + c;
                Amplitude = amp * Design2.Param.Amplitude;
            }
            else if (Param.Mode == AmplitudeValue.Parameter.ValueMode.Exponential)
            {
                if (!Param.DisableRangeLimit)
                {
                    if (Current > Param.EndFrequency) Current = Param.EndFrequency;
                }

                double t = 1 / Param.EndFrequency * Math.Log(Param.EndAmplitude + 1);

                Amplitude = Math.Pow(Math.E, t * Current) - 1;
            }
            else if (Param.Mode == AmplitudeValue.Parameter.ValueMode.LinearPolynomial)
            {
                if (!Param.DisableRangeLimit)
                {
                    if (Current > Param.EndFrequency) Current = Param.EndFrequency;
                }
                Amplitude = Math.Pow(Current, Param.Polynomial) / Math.Pow(Param.EndFrequency, Param.Polynomial) * Param.EndAmplitude;
            }
            else if (Param.Mode == AmplitudeValue.Parameter.ValueMode.Sine)
            {
                if (!Param.DisableRangeLimit)
                {
                    if (Current > Param.EndFrequency) Current = Param.EndFrequency;
                }

                double x = Math.PI * Current / (2.0 * Param.EndFrequency);

                Amplitude = Math.Sin(x) * Param.EndAmplitude;
            }

            if (Param.CutOffAmplitude > Amplitude) Amplitude = 0;
            if (Param.MaxAmplitude != -1 && Param.MaxAmplitude < Amplitude) Amplitude = Param.MaxAmplitude;

            return Amplitude;
        }
        private static bool IsMatching(Domain Control, PulseControl ysd)
        {
            bool enable_free_run_condition = Control.IsFreeRun() && ((!Control.IsPowerOff() && ysd.EnableFreeRunOn) || (Control.IsPowerOff() && ysd.EnableFreeRunOff));
            bool enable_normal_condition = ysd.EnableNormal && !Control.IsFreeRun();
            if (!(enable_free_run_condition || enable_normal_condition)) return false;

            bool Condition1 = ysd.ControlFrequencyFrom <= Control.GetControlFrequency();
            bool Condition2 = ysd.RotateFrequencyFrom == -1 || ysd.RotateFrequencyFrom <= Control.GetBaseWaveFrequency();
            bool Condition3 = ysd.RotateFrequencyBelow == -1 || ysd.RotateFrequencyBelow > Control.GetBaseWaveFrequency();

            if (!Condition2) return false;
            if (!Condition3) return false;

            if (!Control.IsFreeRun() && Condition1) return true;
            if (!Control.IsFreeRun() && !Condition1) return false;

            if (Condition1) return true;

            if (
                (ysd.StuckFreeRunOn && Control.IsFreeRun() && !Control.IsPowerOff()) ||
                (ysd.StuckFreeRunOff && Control.IsFreeRun() && Control.IsPowerOff())
            )
            {
                if (Control.GetBaseWaveFrequency() > ysd.ControlFrequencyFrom) return true;
                return false;
            }

            return false;

        }
        public static void Calculate(Domain Domain, Struct Data)
        {
            // Minimum Frequency Solve
            double minBaseFrequency;
            if (Domain.IsBraking()) minBaseFrequency = Data.MinimumFrequency.Braking;
            else minBaseFrequency = Data.MinimumFrequency.Accelerating;
            if (0 < Domain.GetControlFrequency() && Domain.GetControlFrequency() < minBaseFrequency && !Domain.IsFreeRun()) Domain.SetControlFrequency(minBaseFrequency);
            Domain.SetBaseWaveTimeChangeAllowed(!(Domain.GetBaseWaveFrequency() < minBaseFrequency && Domain.GetControlFrequency() > 0));
            double solvedElectricBaseWaveFrequency = Domain.IsBaseWaveTimeChangeAllowed() ? Domain.GetBaseWaveFrequency() : minBaseFrequency;

            // Pattern Solve
            int solveIndex = -1;
            List<PulseControl> patternList = [.. Domain.IsBraking() ? Data.BrakingPattern : Data.AcceleratePattern];
            patternList.Sort((a, b) => b.ControlFrequencyFrom.CompareTo(a.ControlFrequencyFrom));
            for (int index = 0; index < patternList.Count; index++)
            {
                PulseControl ysd = patternList[index];
                if (!IsMatching(Domain, ysd)) continue;
                solveIndex = index;
                break;
            }
            if (solveIndex == -1)
            {
                if (Domain.IsFreeRun())
                {
                    if (Domain.IsPowerOff())
                        Domain.SetControlFrequency(0);
                    else
                        Domain.SetControlFrequency(Domain.GetBaseWaveFrequency());
                }
                Domain.ElectricalState = new(Data.Level, solvedElectricBaseWaveFrequency);
                return;
            }

            // Target Pattern
            PulseControl solvePattern = patternList[solveIndex];
            Pulse solvePulse = solvePattern.PulseMode;

            // Solve Async Carrier Frequency
            ElectricalParameter.CarrierParameter? solvedCarrierFrequency = null;
            if (solvePulse.PulseType == PulseTypeName.ASYNC)
            {
                ElectricalParameter.CarrierParameter.RandomFrequency randomFrequency = new(
                    solvePattern.AsyncModulationData.RandomData.Range.Mode switch
                    {
                        AsyncControl.RandomModulation.Parameter.ValueMode.Moving => GetMovingValue(solvePattern.AsyncModulationData.RandomData.Range.MovingValue, Domain.GetControlFrequency()),
                        _ => solvePattern.AsyncModulationData.RandomData.Range.Constant,
                    },
                    solvePattern.AsyncModulationData.RandomData.Interval.Mode switch
                    {
                        AsyncControl.RandomModulation.Parameter.ValueMode.Moving => GetMovingValue(solvePattern.AsyncModulationData.RandomData.Interval.MovingValue, Domain.GetControlFrequency()),
                        _ => solvePattern.AsyncModulationData.RandomData.Interval.Constant,
                    }
                );

                ElectricalParameter.CarrierParameter.VibratoFrequency SolveVibratoParameter() { 
                    double SolveVibratoValue(AsyncControl.CarrierFrequency.VibratoValue.Parameter ValueData)
                    {
                        return ValueData.Mode switch
                        {
                            AsyncControl.CarrierFrequency.VibratoValue.Parameter.ValueMode.Moving => GetMovingValue(ValueData.MovingValue, Domain.GetControlFrequency()),
                            _ => ValueData.Constant
                        };
                    }
                    return new ElectricalParameter.CarrierParameter.VibratoFrequency(
                        SolveVibratoValue(solvePattern.AsyncModulationData.CarrierWaveData.VibratoData.Highest),
                        SolveVibratoValue(solvePattern.AsyncModulationData.CarrierWaveData.VibratoData.Lowest),
                        SolveVibratoValue(solvePattern.AsyncModulationData.CarrierWaveData.VibratoData.Interval)
                    );
                }

                ElectricalParameter.CarrierParameter.ConstantFrequency SolveTableParameter()
                {
                    List<AsyncControl.CarrierFrequency.TableValue.Parameter> Table = [.. solvePattern.AsyncModulationData.CarrierWaveData.CarrierFrequencyTable.Table];
                    Table.Sort((a, b) => Math.Sign(b.ControlFrequencyFrom - a.ControlFrequencyFrom));
                    int target = 0;
                    for (int i = 0; i < Table.Count; i++)
                    {
                        var carrier = Table[i];
                        bool flag1 = carrier.FreeRunStuckAtHere && (Domain.GetBaseWaveFrequency() >= carrier.ControlFrequencyFrom) && Domain.IsFreeRun();
                        bool flag2 = Domain.GetControlFrequency() > carrier.ControlFrequencyFrom;
                        if (!flag1 && !flag2) continue;
                        target = i;
                        break;
                    }
                    return new ElectricalParameter.CarrierParameter.ConstantFrequency(Table[target].CarrierFrequency);
                }

                object baseFrequency = solvePattern.AsyncModulationData.CarrierWaveData.Mode switch
                {
                    AsyncControl.CarrierFrequency.ValueMode.Vibrato => SolveVibratoParameter(),
                    AsyncControl.CarrierFrequency.ValueMode.Table => SolveTableParameter(),
                    AsyncControl.CarrierFrequency.ValueMode.Moving => new ElectricalParameter.CarrierParameter.ConstantFrequency(GetMovingValue(solvePattern.AsyncModulationData.CarrierWaveData.MovingValue, Domain.GetControlFrequency())),
                    _ => new ElectricalParameter.CarrierParameter.ConstantFrequency(solvePattern.AsyncModulationData.CarrierWaveData.Constant),
                };

                solvedCarrierFrequency = new ElectricalParameter.CarrierParameter(randomFrequency, baseFrequency);
            }

            // Solve Amplitude
            double solvedAmplitude = 0;
            if (Domain.IsFreeRun())
            {

                Jerk baseFrequencyInfo = Domain.IsBraking() ? Data.JerkSetting.Braking : Data.JerkSetting.Accelerating;
                AmplitudeValue.Parameter Param = (Domain.IsPowerOff() ? solvePattern.Amplitude.PowerOff : solvePattern.Amplitude.PowerOn).Clone();
                double MaxControlFrequency = !Domain.IsPowerOff() ? baseFrequencyInfo.On.MaxControlFrequency : baseFrequencyInfo.Off.MaxControlFrequency;

                if (Param.EndFrequency == -1)
                {
                    if (solvePattern.Amplitude.Default.DisableRangeLimit) Param.EndFrequency = Domain.GetBaseWaveFrequency();
                    else
                    {
                        Param.EndFrequency = (Domain.GetBaseWaveFrequency() > MaxControlFrequency) ? MaxControlFrequency : Domain.GetBaseWaveFrequency();
                        Param.EndFrequency = (Param.EndFrequency > solvePattern.Amplitude.Default.EndFrequency) ? solvePattern.Amplitude.Default.EndFrequency : Param.EndFrequency;
                    }
                }

                if (Param.EndAmplitude == -1) Param.EndAmplitude = GetAmplitude(solvePattern.Amplitude.Default, Domain.GetBaseWaveFrequency());
                if (Param.StartAmplitude == -1) Param.StartAmplitude = GetAmplitude(solvePattern.Amplitude.Default, Domain.GetBaseWaveFrequency());
                solvedAmplitude = GetAmplitude(Param, Domain.GetControlFrequency());
            }
            else
                solvedAmplitude = GetAmplitude(solvePattern.Amplitude.Default, Domain.GetControlFrequency());

            // Solve PulseData
            Dictionary<PulseDataKey, double> solvedPulseData = [];
            PulseDataKey[] PulseDataKeys = Config.GetAvailablePulseDataKey(solvePulse, Data.Level);
            for (int i = 0; i < PulseDataKeys.Length; i++)
            {
                PulseDataValue? Value = solvePulse.PulseData.GetValueOrDefault(PulseDataKeys[i]);
                double Val = Config.GetPulseDataKeyDefaultConstant(PulseDataKeys[i]);
                if (Value != null)
                {
                    Val = Value.Mode switch
                    {
                        PulseDataValue.PulseDataValueMode.Moving => GetMovingValue(Value.MovingValue, Domain.GetControlFrequency()),
                        _ => Value.Constant,
                    };
                }

                solvedPulseData.Add(PulseDataKeys[i], Val);
            }

            if (Domain.IsPowerOff() && solvedAmplitude == 0) Domain.SetControlFrequency(0);

            Domain.ElectricalState = new ElectricalParameter(
                false,
                Domain.ElectricalState.BaseWaveAmplitude == 0 || Domain.GetControlFrequency() == 0,
                Data.Level, 
                solvePattern.Clone(), 
                solvedCarrierFrequency, 
                solvedPulseData, 
                solvedElectricBaseWaveFrequency, 
                solvedAmplitude
            );
        }
    }
}
