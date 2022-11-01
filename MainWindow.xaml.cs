using Microsoft.Win32;
using NAudio.Gui;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static MiniSampler.SamplePlayer;

namespace MiniSampler
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private const int cols = 5;
        private const int rows = 5;
        private SamplePlayer samplePlayer;
        private SampleItem[] sampleItem;

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            for (int i=0;i<sampleItem.Length;i++)
                if (!sampleItem[i].empty && (e.Key == sampleItem[i].hotKey))
                {
                    sampleItem[i].keyDown();
                    return;
                }
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);
            for (int i = 0; i < sampleItem.Length; i++)
                if (!sampleItem[i].empty  && (e.Key == sampleItem[i].hotKey))
                {
                    sampleItem[i].keyUp();
                    return;
                }
        }

        public class SampleItem
        {
            public static Key[] hotkeys = { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12 };

            // High Level Config
            public bool empty = true;
            public string name = "[EMPTY]";
            public string filename = "";
            public enum PlayMode { ClickAndForget, PlayWhileHeld, PlayOnOff };
            public PlayMode playMode = PlayMode.ClickAndForget;
            public Key hotKey = Key.None;

            // Low Level Player
            public SamplePlayer samplePlayer = null;
            public SampleData data = null;
            public SamplePlayButton play = null;

            private int playid = -1;
            private int keyid = -1;

            public SampleItem(SamplePlayer _samplePlayer, int _keyid)
            {
                samplePlayer = _samplePlayer;
                keyid = _keyid;
            }

            public SampleItem(SamplePlayer _samplePlayer, int _keyid, string fn):this(_samplePlayer, _keyid)
            {
                if ((fn == null) || (fn.Length < 1))
                    return;
                filename = fn;
                name = System.IO.Path.GetFileName(fn);
                data = new SampleData(samplePlayer, fn);
                if (data.isValid())
                    empty = false;
            }

            public SampleItem(SamplePlayer _samplePlayer, int _keyid, BinaryReader brd)
            {
                samplePlayer = _samplePlayer;
                keyid = _keyid;
                int version = brd.ReadInt32();
                empty = brd.ReadBoolean();
                filename = brd.ReadString();
                name = brd.ReadString();
                playMode = (PlayMode)brd.ReadInt32();
                hotKey = (Key)brd.ReadInt32();
                playid = -1;
                if (!empty)
                    data = new SampleData(samplePlayer, brd);
            }

            public void store(BinaryWriter bwr)
            {
                bwr.Write((int)0);
                bwr.Write(empty);
                bwr.Write(filename);
                bwr.Write(name);
                bwr.Write((int)playMode);
                bwr.Write((int)hotKey);
                if (!empty)
                    data.store(bwr);
            }

            public void keyDown()
            {
                play.manualKeyDown();
            }

            public void keyUp()
            {
                play.manualKeyUp();
            }

            public SampleItem(SamplePlayer _samplePlayer, int _keyid, bool fromRegistry):this(_samplePlayer, _keyid)
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                        string.Format(@"SOFTWARE\101010b\MiniSampler\BTN_{0}", keyid)))
                    {
                        if (key == null) throw new Exception("Key not found");
                        string _filename = (string)key.GetValue("filename");
                        string _name = (string)key.GetValue("name");
                        bool _loop = (int)key.GetValue("loop") != 0;
                        int _loopstart = (int)key.GetValue("loopstart");
                        int _loopstop = (int)key.GetValue("loopstop");
                        PlayMode _playmode = (PlayMode)key.GetValue("playmode");
                        float _volume = float.Parse((string)key.GetValue("volume"), CultureInfo.InvariantCulture);
                        float _fadein = float.Parse((string)key.GetValue("fadein"), CultureInfo.InvariantCulture);
                        float _fadeout = float.Parse((string)key.GetValue("fadeout"), CultureInfo.InvariantCulture);
                        Key _hotKey = (Key)((int)key.GetValue("hotkey"));

                        // Ok, update the dataset
                        empty = true;
                        filename = _filename;
                        name = _name;
                        // Try to load data
                        data = new SampleData(samplePlayer, filename);
                        if (!data.isValid())
                        {
                            return;
                        }
                        empty = false;
                        data.loop = _loop;
                        data.loopstart = (_loopstart < data.samples) ? _loopstart : 0;
                        data.loopstop = (_loopstop < data.samples) ? _loopstop : data.samples;
                        data.volume = _volume;
                        data.fadeInTime = _fadein;
                        data.fadeOutTime = _fadeout;
                        playMode = _playmode;
                        hotKey = _hotKey;
                        if (play != null)
                            updatePlayButton();
                    }
                }
                catch (Exception ex)
                {
                    empty = true;
                    data = null;
                }
            }

            private void updatePlayButton()
            {
                if (empty)
                {
                    play.empty = true;
                    play.isToggleSwitch = false;
                    play.buttonStayDown = false;
                    play.sampleName = "";
                    play.hotKey = Key.None;
                }
                else
                {
                    play.empty = false;
                    play.sampleName = name;
                    play.hotKey = hotKey;
                    switch (playMode)
                    {
                        case PlayMode.PlayOnOff:
                            play.isToggleSwitch = true;
                            play.buttonStayDown = false;
                            break;
                        case PlayMode.ClickAndForget:
                            play.isToggleSwitch = false;
                            play.buttonStayDown = true;
                            break;
                        case PlayMode.PlayWhileHeld:
                            play.isToggleSwitch = false;
                            play.buttonStayDown = false;
                            break;
                    }
                }
            }


            public void setButton(SamplePlayButton _play)
            {
                play = _play;
                play.downStateChanged += Play_downStateChanged;
                play.configRequest += Play_configRequest;
                updatePlayButton();
            }

            public void storeRegistryData()
            {
                
                string keyname = string.Format(@"SOFTWARE\101010b\MiniSampler\BTN_{0}", keyid);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyname))
                {
                    if (key != null)
                    {
                        // Delete Key first
                        key.Close();
                        Registry.CurrentUser.DeleteSubKey(keyname);
                    }
                }
                if (empty) return;
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                        string.Format(@"SOFTWARE\101010b\MiniSampler\BTN_{0}", keyid)))
                {
                    if (key != null)
                    {
                        key.SetValue("filename", filename);
                        key.SetValue("name", name);
                        key.SetValue("loop", (data.loop) ? 1 : 0);
                        key.SetValue("loopstart", data.loopstart);
                        key.SetValue("loopstop", data.loopstop);
                        key.SetValue("playmode", (int)playMode);
                        key.SetValue("volume", data.volume.ToString(CultureInfo.InvariantCulture));
                        key.SetValue("fadein", data.fadeInTime);
                        key.SetValue("fadeout", data.fadeOutTime);
                        key.SetValue("hotkey", (int)hotKey);
                        key.Close();
                    }
                }
            }


            private void Play_configRequest()
            {
                SampleSetupWindow sw;
                if (empty)
                    sw = new SampleSetupWindow(samplePlayer, null, "", "", Key.None,PlayMode.PlayWhileHeld);
                else
                    sw = new SampleSetupWindow(samplePlayer, data, filename, name, hotKey, playMode);
                if (sw.ShowDialog() == true)
                {
                    if (sw.data != null)
                    {
                        if (sw.data != data)
                        {
                            // New Dataset 
                            data = sw.data;
                        }
                        filename = sw.filename;
                        name = sw.name;
                        playMode = sw.mode;
                        empty = false;
                        hotKey = sw.hotkey;
                        updatePlayButton();
                    } else
                    {
                        empty = true;
                        updatePlayButton();
                    }
                    storeRegistryData();
                }
            }

            private void Play_downStateChanged(bool down)
            {
                if (down)
                {
                    if (empty) return;
                    if (playid < 0)
                        playid = samplePlayer.playon(data);
                }
                else
                {
                    if (empty) return;
                    if (playid >= 0)
                        samplePlayer.playoff(playid);
                    playid = -1;
                }
            }

            public void tick()
            {
                if (playid >= 0)
                {
                    if (!samplePlayer.stillPlaying(playid))
                    {
                        playid = -1;
                        play.release();
                    }
                }
            }

        }

        DispatcherTimer uiUpdateTimer;

        public MainWindow()
        {
            InitializeComponent();

            samplePlayer = new SamplePlayer();

            int defdev = -1;
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\101010b\MiniSampler");
            if (key != null)
            {
                string selectedItem = (string)key.GetValue("AudioDevice");
                for (int i = 0; i < samplePlayer.devNames.Length; i++)
                {
                    if (samplePlayer.devNames[i].Equals(selectedItem, StringComparison.InvariantCultureIgnoreCase))
                        defdev = i;
                }
            }


            sampleItem = new SampleItem[rows*cols];

            for (int i=0;i< rows*cols; i++)
            {
                sampleItem[i] = new SampleItem(samplePlayer, i, true);
                int col = i % cols;
                int row = i / cols;

                SamplePlayButton bplay = new SamplePlayButton();
                bplay.Margin = new Thickness(0, 0, 0, 0);
                gridPlay.Children.Add(bplay);
                Grid.SetRow(bplay, row);
                Grid.SetColumn(bplay, col);

                sampleItem[i].setButton(bplay);
            }

            samplePlayer.open(defdev);

            menuSetupAudio.Click += MenuSetupAudio_Click;
            menuOpenFile.Click += MenuOpenFile_Click;
            menuSaveFile.Click += MenuSaveFile_Click;
            menuExit.Click += MenuExit_Click;
            buttonStopAll.Click += ButtonStopAll_Click;

            uiUpdateTimer = new DispatcherTimer();
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            uiUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            uiUpdateTimer.Start();

            if (samplePlayer.isOpen)
                labelInfoText.Text = "MiniSampler Online";
            else
                labelInfoText.Text = "MiniSampler Offline";

        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void MenuSaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fo = new SaveFileDialog();
            fo.Filter = "Sample Set Files (*.msp)|*.msp|All Files (*.*)|*.*";
            if (fo.ShowDialog() == true)
                storeToFile(fo.FileName);
        }

        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fo = new OpenFileDialog();
            fo.Filter = "Sample Set Files (*.msp)|*.msp|All Files (*.*)|*.*";
            if (fo.ShowDialog() == true)
                loadFromFile(fo.FileName);
        }

        private void MenuSetupAudio_Click(object sender, RoutedEventArgs e)
        {
            AudioSetup aw = new AudioSetup(samplePlayer, samplePlayer.openDev);
            if (aw.ShowDialog() == true)
            {
                int newdev = aw.selectedNewDev;
                if (!samplePlayer.open(newdev))
                {
                    MessageBox.Show("Error", "Cannot Open Audio Device", MessageBoxButton.OK, MessageBoxImage.Error);
                    labelInfoText.Text = "Error opening Audio device";
                    return;
                }
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\101010b\MiniSampler");
                    if (key == null)
                        key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\101010b\MiniSampler");
                    if (key != null)
                    {
                        key.DeleteValue("AudioDevice");
                        key.SetValue("AudioDevice", samplePlayer.devNames[samplePlayer.openDev]);
                    }
                }
                catch (Exception ex)
                {
                    // Failed to store... who cares
                }
                labelInfoText.Text = "Minisample Online";
            }
        }

        private void ButtonStopAll_Click(object sender, RoutedEventArgs e)
        {
            samplePlayer.stopHard();
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            levelDisplay.setLevel(samplePlayer.level1, samplePlayer.level2);
            levelDisplay.setMax(samplePlayer.max1, samplePlayer.max2);
            levelDisplay.InvalidateVisual();
            foreach (SampleItem si in sampleItem)
                si.tick();
        }

        const UInt32 MAGIC = 0x123456AA;

        public void store(BinaryWriter bwr)
        {
            bwr.Write(MAGIC);
            bwr.Write(sampleItem.Length);
            bwr.Write(samplePlayer.sampleRate);
            for (int i=0;i<sampleItem.Length;i++)
                sampleItem[i].store(bwr);
        }

        public void load(BinaryReader brd)
        {
            UInt32 magic = brd.ReadUInt32();
            if (magic != MAGIC)
                throw new Exception("File not a vild File");
            int l = brd.ReadInt32();
            int rt = brd.ReadInt32();
            if (rt != samplePlayer.sampleRate)
                throw new Exception("File not made for this samplerate");
            if (l != cols*rows)
                throw new Exception("File has wrong geometry");
            SampleItem[] newSampleItems = new SampleItem[l];
            for (int i=0;i<l;i++)
                newSampleItems[i] = new SampleItem(samplePlayer, i, brd);
            // Still here --> Update Buttons
            SampleItem[] oldSampleItems = sampleItem;
            sampleItem = newSampleItems;
            for (int i = 0; i < l; i++)
            {
                sampleItem[i].setButton(oldSampleItems[i].play);
                oldSampleItems[i].play = null;
            }
            // Update Registry
            for (int i = 0; i < l; i++)
                sampleItem[i].storeRegistryData();
        }

        public void loadFromFile(string fn) 
        {
            try
            {
                BinaryReader brd = new BinaryReader(File.OpenRead(fn));
                load(brd);
                brd.Close();
            } 
            catch (Exception ex)
            {
                MessageBox.Show("Error", String.Format("Failed to read File {0}.", fn), MessageBoxButton.OK, MessageBoxImage.Error);
                labelInfoText.Text = "";
                return;
            }
            labelInfoText.Text = string.Format("File {0} read", fn);
        }

        public void storeToFile(string fn)
        {
            try
            {
                BinaryWriter bwr = new BinaryWriter(File.OpenWrite(fn));
                store(bwr);
                bwr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", String.Format("Failed to write File {0}.", fn), MessageBoxButton.OK, MessageBoxImage.Error);
                labelInfoText.Text = "";
                return;
            }
            labelInfoText.Text = string.Format("File {0} written", fn);
        }


    }
}
