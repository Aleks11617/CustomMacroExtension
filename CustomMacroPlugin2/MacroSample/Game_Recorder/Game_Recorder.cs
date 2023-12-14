﻿using CustomMacroBase;
using CustomMacroBase.Helper;
using CustomMacroBase.Helper.Attributes;
using CustomMacroPlugin2.MacroSample.Game_Recorder.Packet.Base;
using CustomMacroPlugin2.MacroSample.Game_Recorder.Packet.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace CustomMacroPlugin2.MacroSample.Game_Recorder
{
    public partial class Game_Recorder
    {
        class InnerModel : NotificationObject
        {
            private double _DefDurationValue = 0;
            public double DefDurationValue
            {
                get => _DefDurationValue;
                set
                {
                    if (_DefDurationValue != value)
                    {
                        _DefDurationValue = Math.Floor(value);
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        InnerModel model = new();
    }

    [SortIndex(-9999)]
    public partial class Game_Recorder : MacroBase
    {
        const bool hide_it = true;
        readonly Dictionary<string, bool> debug_flag = new()
        {
            {"SendToProCon", hide_it},
            {"DefDuration slider", false},
        };

        public override void Init()
        {
            MainGate.Text = "Recorder";

            MainGate.Add(CreateGateBase("AllowRecording"));//[0]
            MainGate.Add(CreateGateBase("AllowPlayback (Touch to start, Release to stop)"));//[1]
            MainGate[1].Add(CreateGateBase("AutoRepeat"));//[1][0]

            MainGate.Add(CreateGateBase("Send to ProCon Simulator", false, hideself: debug_flag["SendToProCon"]));//[2]
            MainGate.AddEx(() => CreateSlider(0, 1000, model, nameof(model.DefDurationValue), 1, sliderTextPrefix: "DefaultDuration: ", defalutValue: 500, sliderTextSuffix: "ms", hideself: debug_flag["DefDuration slider"]));
            MainGate.AddEx(() => { return new cRecorder() { Width = 320, Height = 240, Margin = new Thickness(0, 4, 0, 4) }; });

            Mediator.Instance.Register(RecorderMessageType.Instance.StartRecordedAction, _ =>
            {
                Task.Run(async () =>
                {
                    tryStart = true;
                    await Task.Delay(500);
                    tryStart = false;
                });
            });
            Mediator.Instance.Register(RecorderMessageType.Instance.StopRecordedAction, _ =>
            {
                Task.Run(async () =>
                {
                    tryStop = true;
                    await Task.Delay(500);
                    tryStop = false;
                });
            });
        }

        public override void UpdateState()
        {
            if (MainGate.Enable is false) { return; }

            if (MainGate[0].Enable) { Macro_Record(); }
            if (MainGate[1].Enable) { Macro_Playback(); }

            if (MainGate[2].Enable) { ProConSendReport(32); }
        }
    }

    public partial class Game_Recorder
    {
        RecorderController RM => RecorderController.Instance;

        bool tryStart = false;
        bool tryStop = false;
        bool runByBtn = false;
        bool tryLoop => MainGate[1][0].Enable;

        bool start_flag => RealDS4.Touch0IsActive || tryStart;
        bool stop_flag => runByBtn ? tryStop : !RealDS4.Touch0IsActive;
        bool repeat_flag => tryLoop;

        private void Macro_Record()
        {
            RM.Record(RealDS4, VirtualDS4, () => (int)model.DefDurationValue);
        }

        private void Macro_Playback()
        {
            if (RM is null) { return; }

            if (tryStart) { runByBtn = true; }
            if (RealDS4.Touch0IsActive) { runByBtn = false; }

            RM.Start_Condition = start_flag;
            RM.Stop_Condition = stop_flag;
            RM.Repeat_Condition = repeat_flag;
            RM.Playback(VirtualDS4);
        }
    }
}
