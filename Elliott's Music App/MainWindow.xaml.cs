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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Microsoft.DirectX.DirectSound;
using System.Windows.Interop;
using System.Windows.Markup;
using Microsoft.DirectX.AudioVideoPlayback;
using System.Windows.Threading;

namespace Elliott_s_Music_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        DispatcherTimer layerTimer;
        bool layerIsDragging = false;
        bool layerFading = false;
        private double layerCrossElapsed = 0;

        DispatcherTimer stemTimer;
        bool stemIsDragging = false;
        bool stemFading = false;
        private double stemCrossElapsed = 0;

        TimeSpan crossTime = new TimeSpan(0,0,0,0,2500);
 

        private Device dev;
        private BufferDescription bf;
        
        private SecondaryBuffer l1;
        private SecondaryBuffer l2;
        private SecondaryBuffer l3;
        private SecondaryBuffer l4;
        private SecondaryBuffer currentLayer;
        private SecondaryBuffer oldLayer;

        private SecondaryBuffer s1;
        private SecondaryBuffer s2;
        private SecondaryBuffer s3;
        private SecondaryBuffer s4;

        bool aSelect = false;
        bool bSelect = false;
        bool cSelect = false;
        bool dSelect = false;

        bool aFadeIn = false;
        bool bFadeIn = false;
        bool cFadeIn = false;
        bool dFadeIn = false;

        bool aFadeOut = false;
        bool bFadeOut = false;
        bool cFadeOut = false;
        bool dFadeOut = false;

        bool layerPlaying = false;
        bool stemPlaying = false;

        bool minSelect = false;
        bool tenSelect = false;
        bool danSelect = false;
        bool actSelect = false;

        ImageSource play = new BitmapImage(new Uri("Assets/play.png", UriKind.Relative));
        ImageSource pause = new BitmapImage(new Uri("Assets/pause.png", UriKind.Relative));
        ImageBrush iPlay;
        ImageBrush iPause;

        SolidColorBrush WhiteFill = new SolidColorBrush(Colors.White);
        SolidColorBrush BlackFill = new SolidColorBrush(Color.FromRgb(34,34,34));

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (XamlParseException ex)
            {
                System.Windows.MessageBox.Show(ex.InnerException.ToString());
            }
            dev = new Device();
            WindowInteropHelper winHelp = new WindowInteropHelper(this);
            winHelp.EnsureHandle();
            dev.SetCooperativeLevel(winHelp.Handle, CooperativeLevel.Priority);
            bf = new BufferDescription();
            bf.ControlVolume = true;

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);

            iPlay = new ImageBrush(play);
            iPause = new ImageBrush(pause);

            layerTimer = new DispatcherTimer();
            layerTimer.Interval = TimeSpan.FromMilliseconds(5);
            layerTimer.Tick += new EventHandler(layer_Tick);
            layerSeek.Maximum = 0;

            stemTimer = new DispatcherTimer();
            stemTimer.Interval = TimeSpan.FromMilliseconds(5);
            stemTimer.Tick += new EventHandler(stem_Tick);
            stemSeek.Maximum = 0;

        }

        private void stem_Tick(object sender, EventArgs e)
        {
            if (!stemIsDragging)
            {
                if(s1!=null)stemSeek.Value = s1.PlayPosition * 1.0f / ((s1.Format.BitsPerSample / 8) * s1.Format.SamplesPerSecond * s1.Format.Channels);
                else if (s2 != null) stemSeek.Value = s2.PlayPosition * 1.0f / ((s2.Format.BitsPerSample / 8) * s2.Format.SamplesPerSecond * s2.Format.Channels);
                else if (s3 != null) stemSeek.Value = s3.PlayPosition * 1.0f / ((s3.Format.BitsPerSample / 8) * s3.Format.SamplesPerSecond * s3.Format.Channels);
                else if (s4 != null) stemSeek.Value = s4.PlayPosition * 1.0f / ((s4.Format.BitsPerSample / 8) * s4.Format.SamplesPerSecond * s4.Format.Channels);
            }
            if (aFadeIn || aFadeOut)
            {
                StemFade(s1,ref aFadeIn, ref aFadeOut);
            }
            if (bFadeIn || bFadeOut)
            {
                StemFade(s2,ref bFadeIn, ref bFadeOut);
            }
            if (cFadeIn || cFadeOut)
            {
                StemFade(s3,ref cFadeIn, ref cFadeOut);
            }
            if (dFadeIn || dFadeOut)
            {
                StemFade(s4,ref dFadeIn, ref dFadeOut);
            }
        }

        private void Layer_Click(object sender, RoutedEventArgs e)
        {
            if (stemPlaying) Stem_Click(sender, e);
            if (!layerPlaying)
            {
                LayerButton.Background = iPause;
                playLayer();
            }
            else
            {
                LayerButton.Background = iPlay;
                stopLayer();
            }
            layerPlaying = !layerPlaying;
        }

        private void stopLayer()
        {
            if(l1!= null) l1.Stop();
            if(l2!=null) l2.Stop();
            if(l3!=null) l3.Stop();
            if(l4!=null) l4.Stop();
        }

        private void playLayer()
        {
            if (!layerTimer.IsEnabled) layerTimer.Start();
            currentLayer.Play(0, BufferPlayFlags.Looping);
        }

        private void Stem_Click(object sender, RoutedEventArgs e)
        {
            if (layerPlaying) Layer_Click(sender, e);
            if (!stemPlaying)
            {
                StemButton.Background = iPause;
                playStem();
            }
            else
            {
                StemButton.Background = iPlay;
                stopStem();
            }
            stemPlaying = !stemPlaying;
        }

        private void stopStem()
        {
            if (s1 != null) s1.Stop();
            if (s2 != null) s2.Stop();
            if (s3 != null) s3.Stop();
            if (s4 != null) s4.Stop();
        }

        private void playStem()
        {
            if (!stemTimer.IsEnabled) stemTimer.Start();
            if (s1 != null) s1.Play(0, BufferPlayFlags.Looping);
            if (s2 != null) s2.Play(0, BufferPlayFlags.Looping);
            if (s3 != null) s3.Play(0, BufferPlayFlags.Looping);
            if (s4 != null) s4.Play(0, BufferPlayFlags.Looping);
        }

        private void Minimal_Click(object sender, RoutedEventArgs e)
        { 
            if (!minSelect)
            {
                Minimal.Background = WhiteFill;
                Minimal.Foreground = BlackFill;
                minSelect = true;
                if (layerPlaying)
                {
                        LayerFade(l1);
                        if (tenSelect)
                        {
                            Tension_Click(sender, e);
                        }
                        if (danSelect)
                        {
                            Danger_Click(sender, e);
                        }
                        if (actSelect)
                        {
                            Action_Click(sender, e);
                        }
                }
                else
                {
                    currentLayer = l1;
                }
                LayerButton.IsEnabled = true;
            }
            else if(sender != Minimal)
            {
                Minimal.Background = BlackFill;
                Minimal.Foreground = WhiteFill;
                minSelect = false;
            }
        }

        private void Tension_Click(object sender, RoutedEventArgs e)
        {
            if (!tenSelect)
            {
                Tension.Background = WhiteFill;
                Tension.Foreground = BlackFill;
                tenSelect = true;
                if (layerPlaying)
                {
                    LayerFade(l2);
                    if (minSelect)
                    {
                        Minimal_Click(sender, e);
                    }
                    if (danSelect)
                    {
                        Danger_Click(sender, e);
                    }
                    if (actSelect)
                    {
                        Action_Click(sender, e);
                    }
                }
                else
                {
                    currentLayer = l2;
                }
                LayerButton.IsEnabled = true;
            }
            else if (sender != Tension)
            {
                Tension.Background = BlackFill;
                Tension.Foreground = WhiteFill;
                tenSelect = false;
            }
        }

        private void Danger_Click(object sender, RoutedEventArgs e)
        {
            if (!danSelect)
            {
                Danger.Background = WhiteFill;
                Danger.Foreground = BlackFill;
                danSelect = true;
                if (layerPlaying)
                {
                    LayerFade(l3);
                    if (tenSelect)
                    {
                        Tension_Click(sender, e);
                    }
                    if (minSelect)
                    {
                        Minimal_Click(sender, e);
                    }
                    if (actSelect)
                    {
                        Action_Click(sender, e);
                    }
                }
                else
                {
                    currentLayer = l3;
                }
                LayerButton.IsEnabled = true;
            }
            else if (sender != Danger)
            {
                Danger.Background = BlackFill;
                Danger.Foreground = WhiteFill;
                danSelect = false;
            }
        }

        private void Action_Click(object sender, RoutedEventArgs e)
        {
            if (!actSelect)
            {
                Action.Background = WhiteFill;
                Action.Foreground = BlackFill;
                actSelect = true;
                if (layerPlaying)
                {
                    LayerFade(l4);
                    if (tenSelect)
                    {
                        Tension_Click(sender, e);
                    }
                    if (danSelect)
                    {
                        Danger_Click(sender, e);
                    }
                    if (minSelect)
                    {
                        Minimal_Click(sender, e);
                    }
                }
                else
                {
                    currentLayer = l4;
                }
                LayerButton.IsEnabled = true;
            }
            else if (sender != Action)
            {
                Action.Background = BlackFill;
                Action.Foreground = WhiteFill;
                actSelect = false;
            }
        }

        private void Load_Minimal_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "1_LAYER"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog(); 

            // Process open file dialog box results 
            if (result == true) 
            { 
                // Open document 
                string filename = dlg.FileName;
                Audio a1 = Audio.FromFile(filename,false);
                if(layerSeek.Maximum < a1.Duration) layerSeek.Maximum = a1.Duration;
                l1 = new SecondaryBuffer(filename, bf, dev);
                Minimal.IsEnabled = true;
            }
        }

        private void Load_Tension_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "2_LAYER"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Audio a2 = Audio.FromFile(filename, false);
                if (layerSeek.Maximum < a2.Duration) layerSeek.Maximum = a2.Duration;
                l2 = new SecondaryBuffer(filename, bf, dev);
                //Load wav from filename;
                Tension.IsEnabled = true;
            }
        }

        private void Load_Danger_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "3_LAYER"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Audio a3 = Audio.FromFile(filename, false);
                if (layerSeek.Maximum < a3.Duration) layerSeek.Maximum = a3.Duration;
                l3 = new SecondaryBuffer(filename, bf, dev);
                //Load wav from filename;
                Danger.IsEnabled = true;
            }
        }

        private void Load_Action_click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "4_LAYER"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Audio a4 = Audio.FromFile(filename, false);
                if (layerSeek.Maximum < a4.Duration) layerSeek.Maximum = a4.Duration;
                l4 = new SecondaryBuffer(filename, bf, dev);
                //Load wav from filename;
                Action.IsEnabled = true;
            }
        }

        void layer_Tick (object sender, EventArgs e)
        { 
            if(!layerIsDragging){
                layerSeek.Value = currentLayer.PlayPosition * 1.0f/((currentLayer.Format.BitsPerSample/8) * currentLayer.Format.SamplesPerSecond * currentLayer.Format.Channels);
            }
            if (layerFading)
            {
                layerCrossElapsed += layerTimer.Interval.TotalMilliseconds;
                if (layerCrossElapsed < crossTime.TotalMilliseconds)
                {

                    double vol = (1000 * Math.Log10(layerCrossElapsed / crossTime.TotalMilliseconds));
                    if (vol < -10000 || double.IsNaN(vol)) vol = -10000;
                    if (vol > 0) vol = 0;
                    currentLayer.Volume = (int)vol;
                    vol = (1000 * Math.Log10(1-(layerCrossElapsed / crossTime.TotalMilliseconds)));
                    if (vol < -10000 || double.IsNaN(vol)) vol = -10000;
                    if (vol > 0) vol = 0;
                    oldLayer.Volume = (int)vol;
                }
                else
                {
                    oldLayer.Volume = -10000;
                    currentLayer.Volume = 0;
                    layerFading = false;
                    oldLayer.Stop();
                    layerCrossElapsed = 0;
                }
            }
        }

        private void layerSeek_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            layerIsDragging = true;
        }

        private void layerSeek_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            layerIsDragging = false;
            currentLayer.SetCurrentPosition((int)(layerSeek.Value) * currentLayer.Format.SamplesPerSecond * currentLayer.Format.Channels * (currentLayer.Format.BitsPerSample/8));
        }


        private void LayerFade(SecondaryBuffer l1)
        {
            l1.SetCurrentPosition(currentLayer.PlayPosition);
            l1.Volume = -10000;
            l1.Play(0, BufferPlayFlags.Looping);
            layerFading = true;
            oldLayer = currentLayer;
            currentLayer = l1;
        }

        private void StemFade(SecondaryBuffer s1, ref bool fadeIn, ref bool fadeOut)
        {
            stemCrossElapsed += stemTimer.Interval.TotalMilliseconds;
            if (stemCrossElapsed < crossTime.TotalMilliseconds)
            {
                double vol;
                if (fadeIn)
                {
                    vol = (2000 * Math.Log10(stemCrossElapsed / crossTime.TotalMilliseconds));
                    if (vol < -10000 || double.IsNaN(vol)) vol = -10000;
                    if (vol > 0) vol = 0;
                    s1.Volume = (int)vol;
                }
                else if(fadeOut)
                {
                    vol = (2000 * Math.Log10(1 - (stemCrossElapsed / crossTime.TotalMilliseconds)));
                    if (vol < -10000 || double.IsNaN(vol)) vol = -10000;
                    if (vol > 0) vol = 0;
                    s1.Volume = (int)vol;
                }
            }
            else
            {
                stemCrossElapsed = 0;
                if (fadeIn)
                {
                    s1.Volume = 0;
                    fadeIn = false;
                }
                else if (fadeOut)
                {
                    s1.Volume = -10000;
                    fadeOut = false;
                }
            }

        }

        private void crossVar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                crossTime = TimeSpan.FromMilliseconds(double.Parse(crossVar.Text));
            }
        }

        private void Load_A_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "A_STEM"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Audio a1 = Audio.FromFile(filename, false);
                if (stemSeek.Maximum < a1.Duration) stemSeek.Maximum = a1.Duration;
                s1 = new SecondaryBuffer(filename, bf, dev);
                A.IsEnabled = true;
                StemButton.IsEnabled = true;
                A.Background = WhiteFill;
                A.Foreground = BlackFill;
                aSelect = true;
            }
        }

        private void Load_B_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "B_STEM"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Audio a1 = Audio.FromFile(filename, false);
                if (stemSeek.Maximum < a1.Duration) stemSeek.Maximum = a1.Duration;
                s2 = new SecondaryBuffer(filename, bf, dev);
                B.IsEnabled = true;
                StemButton.IsEnabled = true;
                B.Background = WhiteFill;
                B.Foreground = BlackFill;
                bSelect = true;
            }
        }

        private void Load_C_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "C_STEM"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Audio a1 = Audio.FromFile(filename, false);
                if (stemSeek.Maximum < a1.Duration) stemSeek.Maximum = a1.Duration;
                s3 = new SecondaryBuffer(filename, bf, dev);
                C.IsEnabled = true;
                StemButton.IsEnabled = true;
                C.Background = WhiteFill;
                C.Foreground = BlackFill;
                cSelect = true;
            }
        }

        private void Load_D_click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "D_STEM"; // Default file name 
            dlg.DefaultExt = ".wav"; // Default file extension 
            dlg.Filter = "Audio Files (.wav)|*.wav"; // Filter files by extension 

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                Audio a1 = Audio.FromFile(filename, false);
                if (stemSeek.Maximum < a1.Duration) stemSeek.Maximum = a1.Duration;
                s4 = new SecondaryBuffer(filename, bf, dev);
                D.IsEnabled = true;
                StemButton.IsEnabled = true;
                D.Background = WhiteFill;
                D.Foreground = BlackFill;
                dSelect = true;
            }
        }

        private void A_Click(object sender, RoutedEventArgs e)
        {
            if (!aSelect)
            {
                A.Background = WhiteFill;
                A.Foreground = BlackFill;
                A.Content = "Mute A";
                aSelect = true;
                if (stemPlaying)
                {
                    aFadeIn = true;
                }
            }
            else
            {
                A.Background = BlackFill;
                A.Foreground = WhiteFill;
                A.Content = "Unmute A";
                aSelect = false;
                if (stemPlaying)
                {
                    aFadeOut = true;
                }
            }
        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            if (!bSelect)
            {
                B.Background = WhiteFill;
                B.Foreground = BlackFill;
                B.Content = "Mute B";
                bSelect = true;
                if (stemPlaying)
                {

                    bFadeIn = true;
                }
            }
            else
            {
                B.Background = BlackFill;
                B.Foreground = WhiteFill;
                B.Content = "Unmute B";
                bSelect = false;
                if (stemPlaying)
                {
                    bFadeOut = true;
                }
            }
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            if (!cSelect)
            {
                C.Background = WhiteFill;
                C.Foreground = BlackFill;
                C.Content = "Mute C";
                cSelect = true;
                if (stemPlaying)
                {
                    cFadeIn = true;
                }
            }
            else
            {
                C.Background = BlackFill;
                C.Foreground = WhiteFill;
                C.Content = "Unmute C";
                cSelect = false;
                if (stemPlaying)
                {
                    cFadeOut = true;
                }
            }
        }

        private void D_Click(object sender, RoutedEventArgs e)
        {
            if (!dSelect)
            {
                D.Background = WhiteFill;
                D.Foreground = BlackFill;
                D.Content = "Mute D";
                dSelect = true;
                if (stemPlaying)
                {
                    dFadeIn = true;
                }
            }
            else
            {
                D.Background = BlackFill;
                D.Foreground = WhiteFill;
                D.Content = Content = "Unmute D";
                dSelect = false;
                if (stemPlaying)
                {
                    dFadeOut = true;
                }
            }
        }

        private void stemSeek_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            stemIsDragging = true;
        }

        private void stemSeek_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            stemIsDragging = false;
            if(s1!=null)s1.SetCurrentPosition((int)(stemSeek.Value) * s1.Format.SamplesPerSecond * s1.Format.Channels * (s1.Format.BitsPerSample / 8));
            if(s2!=null)s2.SetCurrentPosition((int)(stemSeek.Value) * s2.Format.SamplesPerSecond * s2.Format.Channels * (s2.Format.BitsPerSample / 8));
            if(s3!=null)s3.SetCurrentPosition((int)(stemSeek.Value) * s3.Format.SamplesPerSecond * s3.Format.Channels * (s3.Format.BitsPerSample / 8));
            if(s4!=null)s4.SetCurrentPosition((int)(stemSeek.Value) * s4.Format.SamplesPerSecond * s4.Format.Channels * (s4.Format.BitsPerSample / 8));
        }
    }
}
