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

namespace MiniSampler
{
    /// <summary>
    /// Interaktionslogik für AudioSetup.xaml
    /// </summary>
    public partial class AudioSetup : Window
    {

        private SamplePlayer samplePlayer;
        private int initialDev = -2;
        public int selectedNewDev = -2;

        public AudioSetup(SamplePlayer _samplePlayer, int dev)
        {
            samplePlayer = _samplePlayer;
            InitializeComponent();
            comboAudioDev.Items.Add("Wave Mapper (default)");
            foreach (string name in samplePlayer.devNames)
            {
                comboAudioDev.Items.Add(name);
            }
            int initialDev = samplePlayer.openDev;
            if (samplePlayer.openDev == -2)
            {
                // None selected
                comboAudioDev.SelectedIndex = 0; // Default
            } else
            {
                comboAudioDev.SelectedIndex = samplePlayer.openDev + 1;
            }
            buttonCancel.Click += ButtonCancel_Click;
            buttonOk.Click += ButtonOk_Click;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            // Ok
            if (comboAudioDev.SelectedIndex != initialDev+1)
            {
                // New Dev Selected
                selectedNewDev = comboAudioDev.SelectedIndex - 1;
                DialogResult = true;
                Close();
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            // Cancel
            DialogResult = false;
            Close();
        }


    }
}
