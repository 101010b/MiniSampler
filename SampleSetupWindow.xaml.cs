using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static MiniSampler.MainWindow;

namespace MiniSampler
{
    /// <summary>
    /// Interaktionslogik für SampleSetupWindow.xaml
    /// </summary>
    public partial class SampleSetupWindow : Window
    {

        private SamplePlayer samplePlayer;
        public string filename;
        public string name;
        public SampleData data = null;
        public Key hotkey;
        public MainWindow.SampleItem.PlayMode mode;

        public SampleSetupWindow(SamplePlayer _samplePlayer, SampleData _data, string _filename, string _name, Key _hotkey, MainWindow.SampleItem.PlayMode _mode)
        {
            InitializeComponent();
            samplePlayer = _samplePlayer;
            data = _data;
            filename = _filename;
            name = _name;
            hotkey = _hotkey;
            mode = _mode;

            int hkidx = 0;
            comboHotkey.Items.Add("none");
            for (int i=0;i<MainWindow.SampleItem.hotkeys.Length;i++)
            {
                string name = Enum.GetName(typeof(Key), MainWindow.SampleItem.hotkeys[i]);
                comboHotkey.Items.Add(name);
                if (hotkey == MainWindow.SampleItem.hotkeys[i])
                    hkidx = i+1;
            }
            comboHotkey.SelectedIndex = hkidx;

            comboMode.Items.Add("Click and forget");
            comboMode.Items.Add("Play while held");
            comboMode.Items.Add("Click On/Off");
            comboMode.SelectedIndex = (int)mode;

            if (data == null) 
            {
                textName.Text = "";
                textFilename.Text = "[NO FILE LOADED]";
                textLength.Text = "[NONE]";

                slideVolume.Value = 75.0;
                slideFadeIn.Value = 0;
                slideFadeOut.Value = 0;

                waveView.updateWaveData(null);
            } else
            {
                textName.Text = name;
                textFilename.Text = filename;
                textLength.Text = string.Format("{0} sps = {1:F3} s", data.samples,
                    (double)data.samples / samplePlayer.sampleRate);
                checkLoop.IsChecked = data.loop;

                slideVolume.Value = data.volume;
                slideFadeIn.Value = data.fadeInTime;
                slideFadeOut.Value = data.fadeOutTime;
                waveView.updateWaveData(data);
                if (data.loop) 
                    waveView.loop = true;
                waveView.loopstart = data.loopstart;
                waveView.loopstop = data.loopstop;
            }

            buttonBrowse.Click += ButtonBrowse_Click;
            buttonOk.Click += ButtonOk_Click;
            buttonCancel.Click += ButtonCancel_Click;
            buttonRemove.Click += ButtonRemove_Click;
            checkLoop.Click += CheckLoop_Checked;
            buttonPlay.Click += ButtonPlay_Click;

        }

        private int playid = -1;

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            if (data == null) return;
            if (playid >= 0)
            {
                samplePlayer.playoff(playid);
                playid = -1;
                buttonPlay.Background = Brushes.LightGray;
                buttonPlay.InvalidateVisual();
            }
            else
            {
                if (checkLoop.IsChecked == true)
                {
                    int lstart = waveView.loopstart;
                    int lstop = waveView.loopstop;
                    if (lstart < 0) lstart = 0;
                    if (lstop >= data.samples) lstop = data.samples - 1;
                    if (lstop <= lstart) lstop = lstart + 1;
                    data.loopstart = lstart;
                    data.loopstop = lstop;
                    data.loop = true;
                }
                else
                {
                    data.loop = false;
                }
                data.volume = (float)slideVolume.Value;
                data.fadeInTime = (float)slideFadeIn.Value;
                data.fadeOutTime = (float)slideFadeOut.Value;
                playid = samplePlayer.playon(data);
                buttonPlay.Background = Brushes.LightGreen;
                buttonPlay.InvalidateVisual();
            }
        }

        private void CheckLoop_Checked(object sender, RoutedEventArgs e)
        {
            bool lp = checkLoop.IsChecked == true;
            if (data != null)
            {
                if (lp == data.loop) return;
                data.loop = lp;
                waveView.loop = lp;
            }
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            name = "[EMPTY]";
            filename = "";
            data = null;
            mode = SampleItem.PlayMode.PlayWhileHeld;
            hotkey = Key.None;
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            name = textName.Text;
            if (comboHotkey.SelectedIndex > 0)
                hotkey = MainWindow.SampleItem.hotkeys[comboHotkey.SelectedIndex - 1];
            else
                hotkey = Key.None;
            mode = (MainWindow.SampleItem.PlayMode)comboMode.SelectedIndex;
            if (data != null)
            {
                data.loop = checkLoop.IsChecked == true;
                if (data.loop)
                {
                    data.loopstart = waveView.loopstart;
                    data.loopstop = waveView.loopstop;
                }
                data.volume = (float)slideVolume.Value;
                data.fadeInTime = (float)slideFadeIn.Value;
                data.fadeOutTime = (float)slideFadeOut.Value;
            }
            Close();
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fo = new OpenFileDialog();
            fo.Filter = "Wav Files (*.wav)|*.wav|MP3-Files (*.mp3)|*.mp3";
            if (fo.ShowDialog() == true)
            {
                SampleData newData = new SampleData(samplePlayer, fo.FileName);
                if (newData.isValid())
                {
                    filename = fo.FileName;
                    data = newData;
                    name = System.IO.Path.GetFileNameWithoutExtension(filename);
                    textFilename.Text = filename;
                    textName.Text = name;
                    textLength.Text = string.Format("{0} sps = {1:F3} s", data.samples,
                        (double)data.samples / samplePlayer.sampleRate);
                    waveView.updateWaveData(data);
                }
                else
                {
                    data = null;
                    textFilename.Text = "[NO FILE LOADED]";
                    textName.Text = "[NONE]";
                    textLength.Text = "-";
                    waveView.updateWaveData(null);
                }
            }
        }
    }
}
