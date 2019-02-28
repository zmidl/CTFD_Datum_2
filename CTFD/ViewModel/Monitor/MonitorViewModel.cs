﻿using CTFD.Global;
using CTFD.Global.Common;
using CTFD.Model.Base;
using CTFD.Model.RuntimeData;
using CTFD.ViewModel.Base;
using CTFD.ViewModel.CommandAction;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CTFD.ViewModel.Monitor
{
    public class MonitorViewModel : Base.ViewModel, ISample
    {
        private Thickness margin;
        public Thickness Margin
        {
            get { return this.margin; }
            set
            {
                this.margin = value;
                this.RaisePropertyChanged(nameof(this.Margin));
            }
        }

        private string holeName;

        private string detection;

        private SerialPort qrCodeReceiver;

        public BackgroundTimer CoolDownTimer { get; set; }

        private int experimentViewHeight = 637;
        public int ExperimentViewHeight
        {
            get => this.experimentViewHeight;
            set
            {
                this.experimentViewHeight = value;
                this.RaisePropertyChanged(nameof(this.ExperimentViewHeight));
            }
        }

        public Experiment Experiment => General.WorkingData.Configuration.Experiment;

        public int SelectedSampleIndex { get; set; }

        public string HoleName
        {
            get => this.holeName;
            set
            {
                this.holeName = value;
                foreach (var item in this.Experiment.Samples.Where(o => o.IsSelected == true)) ((Sample)item).SetHoleName(value);
            }
        }

        public string Detection
        {
            get => this.detection;
            set
            {
                this.detection = value;
                foreach (var item in this.Experiment.Samples.Where(o => o.IsSelected == true)) ((Sample)item).SetDetection(value);
            }
        }

        public int AT
        {
            get => this.Experiment.Parameter.AmplificationTemperature / 10;
            set
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    if (result > 0 && result < 130)
                    {
                        this.Experiment.Parameter.AmplificationTemperature = result * 10;
                        this.RaisePropertyChanged(nameof(this.AT));
                    }
                }
            }
        }

        public int AD
        {
            get => this.Experiment.Parameter.AmplificationDuration / 60;
            set
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    if (result > 0 && result < 130)
                    {
                        this.Experiment.Parameter.AmplificationDuration = value * 60;
                        this.RaisePropertyChanged(nameof(this.AD));
                    }
                }
            }
        }

        public int DT
        {
            get => this.Experiment.Parameter.LysisTemperature / 10;
            set
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    if (result > 0 && result < 130)
                    {
                        this.Experiment.Parameter.LysisTemperature = result * 10;
                        this.RaisePropertyChanged(nameof(this.AT));
                    }
                }
            }
        }

        public int DD
        {
            get => this.Experiment.Parameter.LysisDuration / 60;
            set
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    if (result > 0 && result < 130)
                    {
                        this.Experiment.Parameter.LysisDuration = value * 60;
                        this.RaisePropertyChanged(nameof(this.DD));
                    }
                }
            }
        }

        public string[] MinorSource { get; set; }

        private object startButtonContent;

        public Sample CurrentSample { get; set; } = new Sample();

        public int StepIndex { get; set; } = 0;

        public bool IsStartView => this.GetViewStatus(0);

        public bool IsBasicSetupView => this.GetViewStatus(1);

        public bool IsSampleSetupView => this.GetViewStatus(2);

        public bool IsExprimentView => this.GetViewStatus(3);

        public bool IsAnalysisView => this.GetViewStatus(4);

        public bool IsReportView => this.GetViewStatus(5);

        private bool GetViewStatus(int index)=>this.StepIndex == index ? true : false;
        
        public object StartButtonContent
        {
            get => this.startButtonContent;
            set
            {
                this.startButtonContent = value;
                this.RaisePropertyChanged(nameof(this.StartButtonContent));
            }
        }

        public bool IsMinorEnabled { get; set; }

        public int MajorSelection { get; set; }

        public int MinorSelection { get; set; }

        public RelayCommand MajorSelectionChanged => new RelayCommand(() =>
        {
            this.IsMinorEnabled = true;
            switch (this.MajorSelection)
            {
                case 0:
                {
                    this.Experiment.ChangeSeriesVisibility(this.Experiment.Samples, true);
                    this.MinorSource = new string[0];
                    this.IsMinorEnabled = false;
                    break;
                }
                case 1:
                {
                    this.MinorSource = this.Experiment.Samples.Select(o => o.Detection).Distinct().ToArray();
                    break;
                }
                case 2:
                {
                    this.MinorSource = this.Experiment.Samples.Select(o => o.Name).Distinct().ToArray();
                    break;
                }
                case 3:
                {
                    this.MinorSource = new string[] { "阴性", "阳性" };
                    break;
                }
                default: break;
            }
            this.RaisePropertyChanged(nameof(this.MinorSource));
            this.RaisePropertyChanged(nameof(this.IsMinorEnabled));
            this.MinorSelection = 0;
        });

        public RelayCommand MinorSelectionChanged => new RelayCommand(() =>
        {
            switch (this.MajorSelection)
            {
                case 0:
                {
                    this.Experiment.ChangeSeriesVisibility(this.Experiment.Samples, true);
                    break;
                }
                case 1:
                {
                    var minorItem = this.MinorSource[this.MinorSelection];
                    this.Experiment.ChangeSeriesVisibility(this.Experiment.Samples.Where(o => o.Detection == minorItem).ToArray(), true);
                    break;
                }
                case 2:
                {
                    var minorItem = string.Empty;
                    if (this.MinorSelection >= 0) minorItem = this.MinorSource[this.MinorSelection];
                    if (minorItem != null)
                    {
                        var id = this.Experiment.Samples.Where(o => o.Name == minorItem).ToArray();
                        this.Experiment.ChangeSeriesVisibility(id, true);
                    }
                    break;
                }
            }
        });

        public RelayCommand<int> SwitchViewExperiment => new RelayCommand<int>((viewIndex) => this.SwitchExperimentView(viewIndex));

        public RelayCommand RaiseSelectedSamples => new RelayCommand(() => General.RaiseGlobalHandler(GlobalEvent.RaiseSelectedSamplesFromTable));

        public RelayCommand OpenTemplate => new RelayCommand(() =>
        {

            //var openFileDialog = new System.Windows.Forms.OpenFileDialog { Filter = "Exp（*.xls）|*.xls", FilterIndex = 1, RestoreDirectory = true };
            //if (openFileDialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
            //{
            //    this.Device = new Device();
            //    this.Device.InitializeSamples();
            //    this.Device.InitializeChart("");
            //    this.RaisePropertyChanged(nameof(this.Device));
            //    var names = General.ReadExcel(openFileDialog.FileName, "原始数据").AsEnumerable().Select(r => r["A1荧光强度"]).Distinct().ToArray();
            //    this.Device.Samples[0].TestAddingPoint(names);
            //}



            //if (MessageBox.Show("是否覆盖当前已配置的信息", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            //{
            //    var openFileDialog = new System.Windows.Forms.OpenFileDialog { Filter = "Exp（*.sc）|*.sc", FilterIndex = 1, RestoreDirectory = true };
            //    if (openFileDialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
            //    {
            //        using (var streamReader = new StreamReader(openFileDialog.FileName))
            //        {
            //            var data = new StringBuilder();
            //            string line = string.Empty;
            //            while ((line = streamReader.ReadLine()) != null) data.Append(line);
            //            this.Device = General.JsonDeserializeFromString<Device>(data.ToString());
            //        }
            //        this.LoadTemplateData();
            //    }
            //}
        });

        public RelayCommand SaveTemplate => new RelayCommand(() =>
        {

            //for (int i = 0; i < 32; i++)
            //{
            //    this.Device?.Samples[i].AddDissolvingPoint(i * new Random().Next(0, 5));
            //}
            //this.TestChartPerformance();

            //var saveFileDialog = new System.Windows.Forms.SaveFileDialog { Filter = "Exp（*.sc）|*.sc", FilterIndex = 1, RestoreDirectory = true };
            //saveFileDialog.FileName = DateTime.Now.ToString("yyyy_MM_dd");
            //if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    try
            //    {
            //        using (var streamWriter = new StreamWriter(new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
            //        {
            //            streamWriter.WriteLine(General.JsonSerializeToString(General.WorkingData.Device));
            //        }
            //        MessageBox.Show("the Job file has been saved");
            //    }
            //    catch (Exception e)
            //    {
            //        MessageBox.Show(e.Message);
            //    }
            //}
        });

        public RelayCommand EditSample => new RelayCommand((o) =>
        {
            if (this.CurrentSample != null) this.EditCurreentSample(this.CurrentSample, false);
        });

        public RelayCommand OpenFluorescenceChart => new RelayCommand(() =>
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog { Filter = "Csv（*.csv）|*.csv", FilterIndex = 1, RestoreDirectory = true };
            if (openFileDialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
            {
                try
                {
                    var result = new List<string>();
                    StreamReader reader = new StreamReader(openFileDialog.FileName);
                    string line;
                    while ((line = reader.ReadLine()) != null) { result.Add(line); }
                    this.Experiment.AddAmplificationCurve(this.ReadCav(result));
                    General.ShowToast("打开荧光曲线成功");
                }
                catch { }
            }
        });

        public RelayCommand SaveFluorescenceChart => new RelayCommand(() =>
        {
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog { Filter = "Csv（*.csv）|*.csv", FilterIndex = 1, RestoreDirectory = true };
            saveFileDialog.FileName = DateTime.Now.ToString("yyyy_MM_dd");
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    using (var streamWriter = new StreamWriter(new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                    {
                        //streamWriter.WriteLine(this.WriteCsv(this.Experiment.GetAmplificationData().ToList()));
                    }
                    General.ShowToast($"保存荧光曲线成功");
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        });

        public RelayCommand RunOrPause => new RelayCommand(() =>
        {
            switch (General.Status)
            {
                case Status.Stop:
                {
                    Task.Factory.StartNew(() => this.TransmitData(Token.Parameter));
                    General.Status = Status.CoolDown;
                    this.CoolDownTimer.Restart();
                    this.StartButtonContent = General.CoolDown;
                    break;
                }
                case Status.Run:
                {
                    this.TransmitData(Token.End);
                    break;
                }
                case Status.CoolDown:
                {
                    General.Status = Status.Stop;
                    this.StartButtonContent = General.Stop;
                    this.CoolDownTimer.Restart();
                    break;
                }
                default: break;
            }
        });

        public RelayCommand PrintReport => new RelayCommand(async () =>
        {
            var aa = await Func();
            MessageBox.Show(aa.ToString());
        });

        public RelayCommand TransmitParameter => new RelayCommand(() => this.TransmitData(Token.Parameter));

        public RelayCommand ShowManualSettingView => new RelayCommand(() =>
        {
            ((Window)Activator.CreateInstance(typeof(CTFD.View.Monitor.ManualSettingView), new object[1] { this.Experiment })).ShowDialog();
        });

        public RelayCommand TurnPrevious => new RelayCommand(() =>
        {
            this.StepIndex--;
            this.SwitchExperimentView(this.StepIndex);
        });

        public RelayCommand TurnNext => new RelayCommand(() =>
        {
            this.StepIndex++;
            this.SwitchExperimentView(this.StepIndex);
        });

        public RelayCommand Rollback => new RelayCommand(() =>
        {
            this.Experiment.Rollback();
            this.RaisePropertyChanged(nameof(this.AD));
            this.RaisePropertyChanged(nameof(this.AT));
            this.RaisePropertyChanged(nameof(this.DD));
            this.RaisePropertyChanged(nameof(this.DT));
            this.holeName = string.Empty;
            this.RaisePropertyChanged(nameof(this.HoleName));
            this.detection = string.Empty;
            this.RaisePropertyChanged(nameof(this.Detection));
            //(ISample)
        });

        public RelayCommand LostFocus => new RelayCommand(() =>
        {
            General.WriteSetup();
            this.Experiment.ChangeExperimentViewSeries(0);
        });

        public async ValueTask<int> Func()
        {
            await Task.Delay(3000);
            return 100;
        }

        public MonitorViewModel()
        {
            this.InitializeTcpClient();
            this.InitializeSerialPort();
            this.Experiment.Initialize();
            this.CoolDownTimer = new BackgroundTimer(new DateTime(1, 1, 1, 0, 0, 10), "ss", -1);
            this.CoolDownTimer.Stopped += CoolDownTimer_Stopped;
        }

        private string WriteCsv(List<int[]> data)
        {
            var result = new StringBuilder();
            var firstRow = new StringBuilder();
            firstRow.Append("数据,");
            var timeStamp = new DateTime(1, 1, 1, 0, 0, 0);
            for (int i = 0; i < data[0].Length; i++)
            {
                timeStamp = timeStamp.AddSeconds(30);
                firstRow.AppendFormat($"{timeStamp.ToString("HH:mm:ss")},");
            }
            result.AppendLine(firstRow.ToString());
            for (int i = 0; i < data.Count; i++)
            {
                var row = new StringBuilder();
                row.Append($"{this.Experiment.Samples[i].HoleName},");
                foreach (var item in data[i]) row.AppendFormat($"{item},");
                result.AppendLine(row.ToString());
            }
            return result.ToString();
        }

        private List<int[]> ReadCav(List<string> data)
        {
            data.RemoveAt(0);
            var result = new List<int[]>(data.Count);
            var values = new List<int>();
            foreach (var item in data)
            {
                foreach (var item2 in item.Split(','))
                {
                    if (int.TryParse(item2, out int value))
                    {
                        values.Add(value);
                    }
                }
                if (values.Count > 0) result.Add(values.ToArray());
                values.Clear();
            }
            return result;
        }

        private void CoolDownTimer_Stopped(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.TransmitData(Token.Start);
            });
        }

        private void InitializeSerialPort()
        {
            var portName = string.Empty;
            try { portName = SerialPort.GetPortNames().First(); }
            catch { }
            if (string.IsNullOrEmpty(portName)) General.ShowToast("扫码枪异常");
            else
            {
                this.qrCodeReceiver = new SerialPort { Encoding = Encoding.Default, PortName = portName };
                this.qrCodeReceiver.DataReceived += this.QrCodeReceiverDataReceived;
                this.qrCodeReceiver.Open();
            }
        }

        private void SwitchExperimentView(int viewIndex)
        {
            if (viewIndex < 0) viewIndex = 0;
            else if (viewIndex > 5) viewIndex = 5;
            this.StepIndex = viewIndex;
            this.RaisePropertyChanged(nameof(this.StepIndex));
            this.RaisePropertyChanged(nameof(this.IsStartView));
            this.RaisePropertyChanged(nameof(this.IsBasicSetupView));
            this.RaisePropertyChanged(nameof(this.IsSampleSetupView));
            this.RaisePropertyChanged(nameof(this.IsExprimentView));
            this.RaisePropertyChanged(nameof(this.IsAnalysisView));
            this.RaisePropertyChanged(nameof(this.IsReportView));

            this.ExperimentViewHeight = this.StepIndex > 0 ? 580 : 637;

            switch (viewIndex)
            {
                case 0:
                {
                    break;
                }
                case 1:
                {
                    break;
                }
                case 2:
                {

                    break;
                }
                case 3:
                {
                    this.Experiment.RaiseRealtimeXAxis(this.Experiment.Parameter.AmplificationTemperature, this.Experiment.Parameter.GetTimeAxis(), this.Experiment.CalculateRemainningTime());
                    break;
                }
                case 4:
                {

                    break;
                }
            }
        }

        public void DisposeSerialPort()
        {
            try
            {
                this.qrCodeReceiver.DataReceived -= this.QrCodeReceiverDataReceived;
                this.qrCodeReceiver.DiscardInBuffer();
                this.qrCodeReceiver.DiscardOutBuffer();
                this.qrCodeReceiver.Dispose();
                this.qrCodeReceiver.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void LoadTemplateData()
        {
            var aa = new byte[] { 0x00 };
            var imageSizeBytes = aa.Skip(12).Take(4).ToArray();
            var jobSizeBytes = aa.Skip(16).Take(4).ToArray();

            int imageSize = System.BitConverter.ToInt32(imageSizeBytes, 0);
            int jobSize = System.BitConverter.ToInt32(jobSizeBytes, 0);

            var jobStartIndex = 31 + imageSize;
            var jbo = aa.Skip(jobStartIndex).Take(jobSize).ToArray();
        }

        public void ChangeExperimentStatus(int status)
        {
            this.StartButtonContent = status == 0 ? General.Stop : General.Run;
        }

        //public static IList<Object[]> ReadDataFromCSV(string filePathName)
        //{
        //    List<Object[]> ls = new List<Object[]>();
        //    StreamReader fileReader = new StreamReader(filePathName, Encoding.Default);
        //    string line = "";
        //    while (line != null)
        //    {
        //        line = fileReader.ReadLine();
        //        if (String.IsNullOrEmpty(line))
        //            continue;
        //        String[] array = line.Split(';');
        //        ls.Add(array);
        //    }
        //    fileReader.Close();
        //    return ls;
        //}

        private void TransmitData(Token token)
        {
            byte id = 1;
            var result = new List<byte> { (byte)token, id };
            if (this.Experiment != null)
            {
                switch (token)
                {
                    case Token.Parameter: { byte[] data = General.JsonSerialize(this.Experiment.Parameter); if (data != null) result.AddRange(data); break; }
                    case Token.Query: { byte[] data = General.JsonSerialize(General.WorkingData.Query); if (data != null) result.AddRange(data); break; }
                    default: break;
                }
            }
            General.TcpClient.SendMsg(result.ToArray());
        }

        private void InitializeTcpClient()
        {
            if (General.TcpClient != null) General.TcpClient.Close();
            General.TcpClient = new Communication.SocketClient(General.WorkingData.Configuration.CurrentTcpServerIPAddress, General.WorkingData.Configuration.TcpServerPort, true);
            General.TcpClient.ReceiveHandle += this.DataReceived;
            General.TcpClient.StartConnecte();
        }

        private void SwitchStatus(Token token, byte data)
        {
            if (data == 0x00)
            {
                if (token == Token.Start)
                {
                    this.StartButtonContent = General.Run;
                    General.Status = Status.Run;
                    General.ShowToast("实验开始");
                    this.Experiment.ReStartTimer();
                    this.Experiment.ResetExperiment();
                }
                else
                {
                    this.StartButtonContent = General.Stop;
                    General.Status = Status.Stop;
                    General.ShowToast("实验停止");
                    General.WriteSetup();
                    this.Experiment.StopTimer();

                }
            }
        }

        private void EditCurreentSample(Sample sample, bool isFromBarcode)
        {
            var detections = this.Experiment.Samples.Select(o => o.Detection).Distinct().ToArray();
            if (this.SelectedSampleIndex == -1) this.SelectedSampleIndex = 0;
            var start = this.SelectedSampleIndex / detections.Length * detections.Length;
            var range = start + detections.Length;
            for (int i = start; i < range; i++)
            {
                this.Experiment.Samples[i].Name = sample.Name;
                this.Experiment.Samples[i].Patient.Name = sample.Patient.Name;
                this.Experiment.Samples[i].Patient.Sex = sample.Patient.Sex;
                this.Experiment.Samples[i].Patient.Age = sample.Patient.Age;
                this.Experiment.Samples[i].Patient.CaseId = sample.Patient.CaseId;
                this.Experiment.Samples[i].Patient.BedId = sample.Patient.BedId;
                this.Experiment.Samples[i].Patient.OutPatientId = sample.Patient.OutPatientId;
            }
            if (isFromBarcode) this.SelectedSampleIndex = range;
            else this.SelectedSampleIndex = start;
            if (this.SelectedSampleIndex > 31) this.SelectedSampleIndex = 0;
            this.RaisePropertyChanged(nameof(this.SelectedSampleIndex));
        }

        private void QrCodeReceiverDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var codeString = this.qrCodeReceiver.ReadExisting().TrimEnd();
            if (long.TryParse(codeString, out long barCode))
            {
                this.EditCurreentSample(new Sample(), true);
            }
            else
            {
                //var project = default(Project);
                //try
                //{
                //    project = General.JsonDeserializeFromString<Project>(codeString);
                //    App.Current.Dispatcher.InvokeAsync(() => this.Experiment.ResetParameter(project));
                //    General.ShowToast("√ 扫码实验信息成功");
                //}
                //catch
                //{
                //    General.ShowToast("× 扫码失败请尝试重新扫码");
                //}
            }
        }

        private void DataReceived(Communication.SocketClient client, byte[] message)
        {
            App.Current.Dispatcher.InvokeAsync(() =>
            {
                var token = (Token)message[0];
                var id = message[1];
                var value = message.Skip(2).ToArray();
                switch (token)
                {
                    case Token.Start:
                    case Token.End: { this.SwitchStatus(token, value[0]); break; }
                    case Token.RealtimeFluorescenceCurve:
                    {
                        int[] curveData = General.JsonDeserialize<int[]>(value);
                        if (curveData != null) this.Experiment.AddRealtimeAmplificationValue(curveData);
                        break;
                    }
                    case Token.UpperTemperature:
                    case Token.InnerRingTemperature:
                    case Token.OuterRingTemperature:
                    {
                        var data = BitConverter.ToInt16(value, 0);
                        this.Experiment.SetTemperature(token, data);
                        this.Experiment.AddTemperatureValue(token, data);
                        //General.Log.Info($"{token}--{data}");
                        break;
                    }
                    case Token.Rpm: { this.Experiment.SetRpm(BitConverter.ToInt16(value, 0)); break; }
                    case Token.Step: { this.Experiment.SetStep(BitConverter.ToInt16(value, 0)); break; }
                    case Token.Query: { break; }
                    case Token.AmplificationCurve:
                    {
                        var curveData = General.JsonDeserialize<List<int[]>>(value);
                        if (curveData != null) this.Experiment.AddAmplificationCurve(curveData);
                        break;
                    }
                    case Token.RealtimeMeltingCurve:
                    {
                        int[] curveData = General.JsonDeserialize<int[]>(value);
                        if (curveData != null) this.Experiment.AddRealtimeMeltingValue(curveData);
                        break;
                    }
                    case Token.DerivationMeltingCurve:
                    {
                        var curveData = General.JsonDeserialize<List<int[]>>(value);
                        if (curveData != null) this.Experiment.AddDerivationMeltingCurves(curveData);
                        this.Experiment.AddStandardMeltingCurve();
                        break;
                    }
                    case Token.CtValue:
                    {
                       var ctResult = General.JsonDeserialize<string[]>(value);
                        for (int i = 0; i < this.Experiment.Samples.Length; i++) this.Experiment.Samples[i].CtResult = ctResult[i];
                        break;
                    }
                    default: break;
                }
            });
        }

        DateTime t;
        int index1 = 0;
        public void Test()
        {
            if (index1 == 0) t = DateTime.Now;
            else General.ShowToast((DateTime.Now - t).Minutes.ToString());
            index1++;
        }

        void ISample.ResetSelection()
        {
            foreach (var item in this.Experiment.Samples) item.IsSelected = false;
        }
    }
}