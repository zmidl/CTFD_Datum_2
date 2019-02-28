﻿using LiveCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveCharts.Geared;
using System.Windows.Media;
using System.Windows;
using LiveCharts.Wpf;
using CTFD.Model.Base;
using LiveCharts.Defaults;
using CTFD.Global.Common;

namespace CTFD.Model.RuntimeData
{
    public class Charts : Notify
    {
        public SeriesCollection RealtimeCurve { get; private set; }

        private SeriesCollection realtimeAmplificationCurve { get; set; }

        private SeriesCollection realtimeMeltingCurve { get; set; }

        private SeriesCollection realtimeTemperautreCurve { get; set; }

        private int ampXEnd = 100;

        private int meltingXStart = 0;

        private int meltingXEnd = 102;

        private int tempXEnd = 100;

        public string RealtimeXTitle { get; private set; }

        public string RealtimeYTitle { get; private set; }

        public string AnalysisXTitle { get; private set; }

        public string AnalysisYTitel { get; private set; }

        public Func<double, string> RealtimeYFomatter { get; private set; }

        public Func<double, string> RealtimeXFomatter { get; private set; }

        public int? RealtimeXMinValue { get; private set; }

        public int? RealtimeXMaxValue { get; private set; }

        public int? RealtimeXStep { get; private set; }

        public Func<double, string> AnalysisXFormatter { get; private set; }

        public int? AnalysisXStep { get; private set; } = null;

        public int? AnalysisMaxValue { get; private set; } = null;

        public string AnalysisXLabel { get; private set; }

        public SeriesCollection FinalCurve { get; private set; }

        private SeriesCollection finalAmplificationCurve;

        private SeriesCollection finalStandardMeltingCurve;

        private SeriesCollection finalDerivationMeltingCurve;

        private bool isAmplificationCurveFinished;

        private bool isStandarMeltingCurveFinished;

        private bool isDerivationMeltingCurveFinished;

        public Charts()
        {
            this.InitializeSeries();
            this.ChangeAnalysisViewSeries(0);
        }

        public void RaiseRealtimeXAxis(int amplificationTemperature, int ampXEnd, int experimentDuaring)
        {
            this.meltingXStart = amplificationTemperature / 10;
            this.meltingXEnd = (102 - meltingXStart) / 2 + 1;
            this.ampXEnd = ampXEnd;
            this.tempXEnd = experimentDuaring;
        }

        public void InitializeSeries()
        {
            this.isAmplificationCurveFinished = false;
            this.isStandarMeltingCurveFinished = false;
            this.isDerivationMeltingCurveFinished = false;

            this.realtimeAmplificationCurve = this.CreateSeries<int>(32);
            this.realtimeMeltingCurve = this.CreateSeries<int>(32);
            this.realtimeTemperautreCurve = this.CreateSeries2<ObservablePoint>(3);

            this.finalAmplificationCurve = this.CreateSeries<int>(32);
            this.finalStandardMeltingCurve = this.CreateSeries<int>(32);
            this.finalDerivationMeltingCurve = this.CreateSeries<int>(32);
        }

        private SeriesCollection CreateSeries<T>(int seriesCount, int smoothness = 3)
        {
            var result = new SeriesCollection();
            var lines = new GLineSeries[seriesCount];
            for (int i = 0; i < seriesCount; i++) lines[i] = new GLineSeries { Fill = Brushes.Transparent, StrokeThickness = 1, LineSmoothness = smoothness, Values = new ChartValues<T>().AsGearedValues().WithQuality(Quality.Highest), PointGeometrySize = 0 };
            result.AddRange(lines);
            return result;
        }

        private SeriesCollection CreateSeries2<T>(int seriesCount, int smoothness = 3)
        {
            var result = new SeriesCollection();
            var lines = new GLineSeries[seriesCount];
            for (int i = 0; i < seriesCount; i++) lines[i] = new GLineSeries { Fill = Brushes.Transparent, StrokeThickness = 1, LineSmoothness = smoothness, Values = new ChartValues<T>().AsGearedValues().WithQuality(Quality.Highest), PointGeometrySize = 0 };
            result.AddRange(lines);
            return result;
        }

        public void AddRealtimeAmplificationValue(int[] values)
        {
            for (int i = 0; i < 32; i++) this.realtimeAmplificationCurve[i].Values.Add(values[i]);
        }

        public void AddRealtimeMeltingValue(int[] values)
        {
            for (int i = 0; i < 32; i++) this.realtimeMeltingCurve[i].Values.Add(values[i]);
        }

        public void AddRealtimeTemperaturePoint(Token token, ObservablePoint point)
        {
            var index = 0;
            switch (token)
            {
                case Token.UpperTemperature: { index = 0; break; }
                case Token.InnerRingTemperature: { index = 1; break; }
                case Token.OuterRingTemperature: { index = 2; break; }
            }
            this.realtimeTemperautreCurve[index].Values.Add(point);
        }

        public void AddAmplificationCurve(List<int[]> values)
        {
            this.isAmplificationCurveFinished = true;
            for (int i = 0; i < 32; i++) ((LiveCharts.Helpers.NoisyCollection<int>)this.finalAmplificationCurve[i].Values).AddRange(values[i]);
        }

        public void AddStandardMeltingCurve()
        {
            this.isStandarMeltingCurveFinished = true;
            this.finalStandardMeltingCurve = this.realtimeMeltingCurve;
        }

        public void AddDerivationMeltingCurves(List<int[]> values)
        {
            this.isDerivationMeltingCurveFinished = true;
            for (int i = 0; i < 32; i++) ((LiveCharts.Helpers.NoisyCollection<int>)this.finalDerivationMeltingCurve[i].Values).AddRange(values[i]);
        }

        public void ChangeSeriesVisibility(int sampleIndex, bool isCurveDisplayed)
        {
            if (this.isAmplificationCurveFinished) ((GLineSeries)this.finalAmplificationCurve[sampleIndex]).Visibility = isCurveDisplayed ? Visibility.Visible : Visibility.Hidden;
            
            if (this.isStandarMeltingCurveFinished) ((GLineSeries)this.finalStandardMeltingCurve[sampleIndex]).Visibility = isCurveDisplayed ? Visibility.Visible : Visibility.Hidden;
            
            if (this.isDerivationMeltingCurveFinished) ((GLineSeries)this.finalDerivationMeltingCurve[sampleIndex]).Visibility = isCurveDisplayed ? Visibility.Visible : Visibility.Hidden;
            
        }

        public void ChangeRealtimeCurve(int curveIndex)
        {
            switch (curveIndex)
            {
                case 0:
                {
                    this.RealtimeCurve = this.realtimeAmplificationCurve;
                    this.RealtimeXTitle = "实时扩增时间（分钟）";
                    this.RealtimeYTitle = "荧光值";
                    this.RealtimeXFomatter = (value) => $"{((int)(value / 2)).ToString("00")}";
                    this.RealtimeYFomatter = (value) => value.ToString("N0");
                    this.RealtimeXMaxValue = this.ampXEnd;
                    this.RealtimeXStep = this.RealtimeXMaxValue >= 50 ? 8 : 4;
                    break;
                }
                case 1:
                {
                    this.RealtimeCurve = this.realtimeMeltingCurve;
                    this.RealtimeXTitle = "实时熔解温度（分钟）";
                    this.RealtimeYTitle = "荧光值";
                    this.RealtimeXFomatter = (value) => (this.meltingXStart + (value*2)).ToString();
                    this.RealtimeYFomatter = (value) => value.ToString("N0");
                    this.RealtimeXMaxValue = this.meltingXEnd;
                    this.RealtimeXStep = null;
                    break;
                }
                case 2:
                {
                    this.RealtimeCurve = this.realtimeTemperautreCurve;
                    this.RealtimeXTitle = "温度变化时间（分钟）";
                    this.RealtimeYTitle = "温度（℃）";
                    this.RealtimeXFomatter = null;
                    this.RealtimeYFomatter = (value) => $"{(value / 10).ToString("00.0")}";
                    this.RealtimeXMaxValue = this.tempXEnd;
                    this.RealtimeXStep = 10;
                    break;
                }
            }
            this.RaisePropertyChanged(nameof(this.RealtimeCurve));
            this.RaisePropertyChanged(nameof(this.RealtimeXTitle));
            this.RaisePropertyChanged(nameof(this.RealtimeYTitle));
            this.RaisePropertyChanged(nameof(this.RealtimeYFomatter));
            this.RaisePropertyChanged(nameof(this.RealtimeXFomatter));
            this.RaisePropertyChanged(nameof(this.RealtimeXMinValue));
            this.RaisePropertyChanged(nameof(this.RealtimeXMaxValue));
            this.RaisePropertyChanged(nameof(this.RealtimeXStep));
        }

        public void ChangeAnalysisViewSeries(int index)
        {
            switch (index)
            {
                case 0:
                {
                    this.FinalCurve = this.finalAmplificationCurve;
                    this.AnalysisXFormatter = (value) => $"{((int)value / 2).ToString("00")}";
                    this.AnalysisXLabel = "运行时间（单位：分钟）";
                    this.AnalysisXStep = 4;
                    break;
                }
                case 1:
                case 2:
                {
                    if (index == 1) this.FinalCurve = this.finalStandardMeltingCurve;
                    else if (index == 2) this.FinalCurve = this.finalDerivationMeltingCurve;
                    this.AnalysisXStep = null;
                    this.AnalysisXFormatter = null;
                    this.AnalysisXLabel = "温度变化（单位：次数）";
                    break;
                }
                default: break;
            }

            this.RaisePropertyChanged(nameof(this.FinalCurve));
            this.RaisePropertyChanged(nameof(this.AnalysisXLabel));
            this.RaisePropertyChanged(nameof(this.AnalysisMaxValue));
            this.RaisePropertyChanged(nameof(this.AnalysisXStep));
            this.RaisePropertyChanged(nameof(this.AnalysisXFormatter));

        }
    }
}
