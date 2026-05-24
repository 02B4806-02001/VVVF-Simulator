using System;

namespace VvvfSimulator.Generation.Audio
{
    public class SimpleFilter
    {
        private double _alphaLow;
        private double _alphaHigh;
        private double _prevLowPass;
        private double _prevHighPass;
        private double _prevInput;

        public SimpleFilter(double alphaLow = 1.0, double alphaHigh = 0.0)
        {
            _alphaLow = alphaLow;
            _alphaHigh = alphaHigh;
            _prevLowPass = 0;
            _prevHighPass = 0;
            _prevInput = 0;
        }

        public void SetAlphaLow(double alphaLow)
        {
            _alphaLow = alphaLow;
        }

        public void SetAlphaHigh(double alphaHigh)
        {
            _alphaHigh = alphaHigh;
        }

        public void SetLowPassCutoff(double fCutoff, double dt)
        {
            if (fCutoff <= 0 || double.IsInfinity(fCutoff))
            {
                _alphaLow = 1.0;
                return;
            }
            double rc = 1.0 / (2.0 * Math.PI * fCutoff);
            _alphaLow = dt / (rc + dt);
        }

        public void SetHighPassCutoff(double fCutoff, double dt)
        {
            if (fCutoff <= 0 || double.IsInfinity(fCutoff))
            {
                _alphaHigh = 0.0;
                return;
            }
            double rc = 1.0 / (2.0 * Math.PI * fCutoff);
            _alphaHigh = rc / (rc + dt);
        }

        public double Process(double input)
        {
            // Low-pass filter
            double lowPass = _prevLowPass + _alphaLow * (input - _prevLowPass);
            _prevLowPass = lowPass;

            // High-pass filter
            // If alphaHigh is 0, we want to pass the lowPass signal directly (no high-pass filtering)
            // Standard high-pass filter: y[i] = alpha * (y[i-1] + x[i] - x[i-1])
            // To make alphaHigh=0 mean "no filtering", we can map it so that alphaHigh=0 -> alpha=1
            // Or simply bypass it if alphaHigh is 0.
            
            double highPass;
            if (_alphaHigh == 0.0)
            {
                highPass = lowPass;
            }
            else
            {
                // Assuming alphaHigh is between 0 and 1, where closer to 1 means lower cutoff frequency
                // Let's use a standard formula where alphaHigh is the filter coefficient
                highPass = _alphaHigh * (_prevHighPass + lowPass - _prevInput);
            }
            
            _prevHighPass = highPass;
            _prevInput = lowPass;

            return highPass;
        }
    }
}
