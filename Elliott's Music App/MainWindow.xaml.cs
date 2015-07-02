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

        DispatcherTimer layerTimer; // Timer to updates the layer sliders position to the current layers postion and to check/handle the fading between layers every 5 milliseconds
        bool layerIsDragging = false; // Boolean to stop slider position from updating while the slider is being dragged.
        bool layerFading = false; // Boolean to determine whether fading is happening between the old layer and the current layer
        private double layerCrossElapsed = 0; // Time elapsed during current fading to determine the next volume to set the layers at. 

        DispatcherTimer stemTimer; // See above but for stems
        bool stemIsDragging = false; // ^^
        private double stemCrossElapsed = 0; // ^^

        TimeSpan crossTime = new TimeSpan(0,0,0,0,2500); // Variable to hold the total time for the crossfade (can be changed with the text in the GUI). Default is 2.5 seconds
 

        private Device dev; // Device handler for audio ouptut
        private BufferDescription bf; // Describes the SecondaryBuffers to allow volume control during playback

        private SecondaryBuffer l1; // Buffer to hold the data for Layer 1 (minimal)
        private SecondaryBuffer l2; // Buffer to hold the data for Layer 2 (Tension)
        private SecondaryBuffer l3; // Buffer to hold the data for Layer 3 (Danger)
        private SecondaryBuffer l4; // Buffer to hold the data for Layer 4 (Action)
        private SecondaryBuffer currentLayer; // Reference to the current layer playing
        private SecondaryBuffer oldLayer; // Reference to the last layer to allow fading out

        private SecondaryBuffer s1;  // Buffer to hold the data for Stem A
        private SecondaryBuffer s2; // Buffer to hold the data for Stem B
        private SecondaryBuffer s3; // Buffer to hold the data for Stem C
        private SecondaryBuffer s4; // Buffer to hold the data for Stem D

        bool aSelect = false; // Boolean to hold whether stem A is selected (for GUI changing)
        bool bSelect = false;// ^^ B
        bool cSelect = false;// ^^ C
        bool dSelect = false;// ^^ D

        bool aFadeIn = false; // Boolean for storing if stem A is currently fading in
        bool bFadeIn = false;// ^^ B
        bool cFadeIn = false;// ^^ C
        bool dFadeIn = false;// ^^ D

        bool aFadeOut = false;// Boolean for storing if stem A is currently fading out
        bool bFadeOut = false;// ^^ B
        bool cFadeOut = false;// ^^ C
        bool dFadeOut = false;// ^^ D

        bool layerPlaying = false; // If a layer is currently playing 
        bool stemPlaying = false; // If all the stems are currently playing

        bool minSelect = false;  // Boolean to hold whether Minimal is selected (for GUI changing) and also to determine which layer to fade out (previously selected).
        bool tenSelect = false;  // ^^ Tension
        bool danSelect = false; // ^^ Danger
        bool actSelect = false; // ^^ Action

        ImageSource play = new BitmapImage(new Uri("Assets/play.png", UriKind.Relative)); // ImageSource for changing between play and pause images
        ImageSource pause = new BitmapImage(new Uri("Assets/pause.png", UriKind.Relative));
        ImageBrush iPlay; // Brushes to change between play and pause images
        ImageBrush iPause;

        SolidColorBrush WhiteFill = new SolidColorBrush(Colors.White); // Brushes to change reverse the colors when things are selected and deselected
        SolidColorBrush BlackFill = new SolidColorBrush(Color.FromRgb(34,34,34));

        public MainWindow()
        {
            InitializeComponent(); // Initiales XAML for main window


            dev = new Device(); // Creates new Audio device 

            WindowInteropHelper winHelp = new WindowInteropHelper(this); // Gets the handler for the window
            winHelp.EnsureHandle(); // ^^

            dev.SetCooperativeLevel(winHelp.Handle, CooperativeLevel.Priority);  // Uses the window handle to allow for multiple secondary buffers 
            bf = new BufferDescription();
            bf.ControlVolume = true; // Sets buffers to be allowed to change volume dynamically using buffer description.

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant); // Improves rendering quality of play and pause button

            iPlay = new ImageBrush(play); // Initialise play and pause button brushes
            iPause = new ImageBrush(pause);

            layerTimer = new DispatcherTimer(); // Intitalises timer for every 5 milliseconds to call layer_tick. DOES NOT START TIMER YET
            layerTimer.Interval = TimeSpan.FromMilliseconds(5);
            layerTimer.Tick += new EventHandler(layer_Tick);

            layerSeek.Maximum = 0; // Sets slider maximum to 0, changes later when layers are added and the length can be determined

            stemTimer = new DispatcherTimer(); // Intitalises timer for every 5 milliseconds to call stem_tick. DOES NOT START TIMER YET  
            stemTimer.Interval = TimeSpan.FromMilliseconds(5);
            stemTimer.Tick += new EventHandler(stem_Tick);
            stemSeek.Maximum = 0;  // Sets slider maximum to 0, changes later when stems are added and the length can be determined

        }

        // Called every 5 mill to update slider and manage stem fading
        private void stem_Tick(object sender, EventArgs e)
        {
            if (!stemIsDragging)
            {
                //Gets the first stem buffer that isnt empty and determines the current position (in secondds) and sets the sliders position to it.
                if(s1!=null)stemSeek.Value = s1.PlayPosition * 1.0f / ((s1.Format.BitsPerSample / 8) * s1.Format.SamplesPerSecond * s1.Format.Channels);
                else if (s2 != null) stemSeek.Value = s2.PlayPosition * 1.0f / ((s2.Format.BitsPerSample / 8) * s2.Format.SamplesPerSecond * s2.Format.Channels);
                else if (s3 != null) stemSeek.Value = s3.PlayPosition * 1.0f / ((s3.Format.BitsPerSample / 8) * s3.Format.SamplesPerSecond * s3.Format.Channels);
                else if (s4 != null) stemSeek.Value = s4.PlayPosition * 1.0f / ((s4.Format.BitsPerSample / 8) * s4.Format.SamplesPerSecond * s4.Format.Channels);
            }
            // Calles the fading on each of the stems if they are fading
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

        // If the layer play button is clicked this is called. Toggles the playing of the layers and stops stem from playing also.
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

        // Pauses all layers (maybe should pause the timer if that's possible? )
        private void stopLayer()
        {
            if(l1!= null) l1.Stop();
            if(l2!=null) l2.Stop();
            if(l3!=null) l3.Stop();
            if(l4!=null) l4.Stop();
        }

        // Plays current layer (starts timer if hasn't started yet)
        private void playLayer()
        {
            if (!layerTimer.IsEnabled) layerTimer.Start();
            currentLayer.Play(0, BufferPlayFlags.Looping);
        }

        // If the stem play button is clicked this is called. Toggles the playing of the stems and stops layers from playing also.
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

        // Pauses all stems (maybe same timer thing as layers?)
        private void stopStem()
        {
            if (s1 != null) s1.Stop();
            if (s2 != null) s2.Stop();
            if (s3 != null) s3.Stop();
            if (s4 != null) s4.Stop();
        }
        
        // Plays all loaded stems (all stem buffer plays even when muted (like the website))
        private void playStem()
        {
            if (!stemTimer.IsEnabled) stemTimer.Start();
            if (s1 != null) s1.Play(0, BufferPlayFlags.Looping);
            if (s2 != null) s2.Play(0, BufferPlayFlags.Looping);
            if (s3 != null) s3.Play(0, BufferPlayFlags.Looping);
            if (s4 != null) s4.Play(0, BufferPlayFlags.Looping);
        }

        // Handles the clicking of the minimal button
        private void Minimal_Click(object sender, RoutedEventArgs e)
        { 
            if (!minSelect)
            {
                Minimal.Background = WhiteFill;
                Minimal.Foreground = BlackFill;
                minSelect = true;
                if (layerPlaying)
                {
                        //If a layer is currently playing (we know it's not this one because !minselect, then fade this in and fade the currentlayer out
                        LayerFade(l1);

                        // Also, click the other button for us to deslect it and toggle GUI
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
                else // No need to fade if layer switches while paused? (maybe old layer need updating?)
                {
                    currentLayer = l1;
                }
                LayerButton.IsEnabled = true; // Only allow layer play button to be clickable when a layer is selected
            }
            else if(sender != Minimal) // Handles the other layer buttons clicking this button to deselect it as mentioned above 
            {
                Minimal.Background = BlackFill;
                Minimal.Foreground = WhiteFill;
                minSelect = false;
            }
        }

        //See Minimal_Click
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

        // See Minimal_click
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

        // See Minimal_click
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

        //Loads minimal wav. Sets the sliders maximum to maximum layer length
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
        // ^^
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
        // ^^
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
        // ^^
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

        //Handles layer slider updating and fading
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

        //Trivial
        private void layerSeek_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            layerIsDragging = true;
        }

        // Sets current position when slider stops being dragged
        private void layerSeek_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            layerIsDragging = false;
            currentLayer.SetCurrentPosition((int)(layerSeek.Value) * currentLayer.Format.SamplesPerSecond * currentLayer.Format.Channels * (currentLayer.Format.BitsPerSample/8));
        }

        // Starts layer fading/ Playing next layer
        private void LayerFade(SecondaryBuffer l1)
        {
            l1.SetCurrentPosition(currentLayer.PlayPosition);
            l1.Volume = -10000;
            l1.Play(0, BufferPlayFlags.Looping);
            layerFading = true;
            oldLayer = currentLayer;
            currentLayer = l1;
        }
        
        // Handles stem fading
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
            else // Fading has finished, turns volume to 0 (Full) or -10000 (Silent)
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

        // Updates crossfade time when textbox gets enter key (maybe needs better handling for when to update)?
        private void crossVar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                crossTime = TimeSpan.FromMilliseconds(double.Parse(crossVar.Text));
            }
        }

        // Loads stem A wav
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
        // ^^
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

        // Handles toggling of stem A, and enables boolean for fade (All stems are all playing while pause is false, they are just silent, fading or max.)
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


        //Trivial
        private void stemSeek_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            stemIsDragging = true;
        }

        // Sets all stems current position after dragging
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
