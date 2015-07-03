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
using Elliotts_Music_App;

namespace Elliott_s_Music_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer layerTimer;
        bool layerIsDragging = false; // ^^

        DispatcherTimer stemTimer; // See above but for stems
        bool stemIsDragging = false; // ^^

        public static TimeSpan crossTime = new TimeSpan(0,0,0,0,2500); // Variable to hold the total time for the crossfade (can be changed with the text in the GUI). Default is 2.5 seconds

        private Device dev; // Device handler for audio ouptut
        private BufferDescription bf; // Describes the SecondaryBuffers to allow volume control during playback

        private Layer l1; // Buffer to hold the data for Layer 1 (minimal)
        private Layer l2; // Buffer to hold the data for Layer 2 (Tension)
        private Layer l3; // Buffer to hold the data for Layer 3 (Danger)
        private Layer l4; // Buffer to hold the data for Layer 4 (Action)
        private Layer currentLayer;

        private Stem s1;  // Buffer to hold the data for Stem A
        private Stem s2; // Buffer to hold the data for Stem B
        private Stem s3; // Buffer to hold the data for Stem C
        private Stem s4; // Buffer to hold the data for Stem D

        bool layerPlaying = false; // If a layer is currently playing 
        bool stemPlaying = false; // If all the stems are currently playing

        ImageSource play = new BitmapImage(new Uri("Assets/play.png", UriKind.Relative)); // ImageSource for changing between play and pause images
        ImageSource pause = new BitmapImage(new Uri("Assets/pause.png", UriKind.Relative));
        ImageBrush iPlay; // Brushes to change between play and pause images
        ImageBrush iPause;

        public static SolidColorBrush WhiteFill = new SolidColorBrush(Colors.White); // Brushes to change reverse the colors when things are selected and deselected
        public static SolidColorBrush BlackFill = new SolidColorBrush(Color.FromRgb(34,34,34));

        public MainWindow()
        {
            InitializeComponent(); // Initiales XAML for main window

            // NEEEEEEEEEEDS MOVING OUT

            dev = new Device(); // Creates new Audio device 

            WindowInteropHelper winHelp = new WindowInteropHelper(this); // Gets the handler for the window
            winHelp.EnsureHandle(); // ^^

            dev.SetCooperativeLevel(winHelp.Handle, CooperativeLevel.Priority);  // Uses the window handle to allow for multiple secondary buffers 
            bf = new BufferDescription();
            bf.ControlVolume = true; // Sets buffers to be allowed to change volume dynamically using buffer description.

            // END OF NEEEEEEEEEDS MOVING OUT

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant); // Improves rendering quality of play and pause button

            iPlay = new ImageBrush(play); // Initialise play and pause button brushes
            iPause = new ImageBrush(pause);

            layerSeek.Maximum = 0; // Sets slider maximum to 0, changes later when layers are added and the length can be determined

            stemTimer = new DispatcherTimer(); // Intitalises timer for every 5 milliseconds to call stem_tick. DOES NOT START TIMER YET  
            stemTimer.Interval = TimeSpan.FromMilliseconds(30);
            stemTimer.Tick += new EventHandler(stem_Tick);

            layerTimer = new DispatcherTimer();
            layerTimer.Interval = TimeSpan.FromMilliseconds(30);
            layerTimer.Tick += new EventHandler(layer_Tick);

            stemSeek.Maximum = 0;  // Sets slider maximum to 0, changes later when stems are added and the length can be determined

        }

        // Called every 5 mill to update slider and manage stem fading
        private void stem_Tick(object sender, EventArgs e)
        {
            if (!stemIsDragging)
            {
                //Gets the first stem buffer that isnt empty and determines the current position (in seconds) and sets the sliders position to it.
                if (s1 != null) stemSeek.Value = s1.getPosition();
                else if (s2 != null) stemSeek.Value = s2.getPosition();
                else if (s3 != null) stemSeek.Value = s3.getPosition();
                else if (s4 != null) stemSeek.Value = s4.getPosition();
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


        // Plays current layer (starts timer if hasn't started yet)
        private void playLayer()
        {
            if (!layerTimer.IsEnabled) layerTimer.Start();
            if (l1 != null) l1.play();
            if (l2 != null) l2.play();
            if (l3 != null) l3.play();
            if (l4 != null) l4.play();
        }

        // Pauses all layers (maybe should pause the timer if that's possible? )
        private void stopLayer()
        {
            if (layerTimer.IsEnabled) layerTimer.Stop();
            if(l1!= null) l1.stop();
            if(l2!= null) l2.stop();
            if(l3!= null) l3.stop();
            if(l4!= null) l4.stop();
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

        // Plays all loaded stems (all stem buffer plays even when muted (like the website))
        private void playStem()
        {
            if (!stemTimer.IsEnabled) stemTimer.Start();
            if (s1 != null) s1.play();
            if (s2 != null) s2.play();
            if (s3 != null) s3.play();
            if (s4 != null) s4.play();
        }

        // Pauses all stems (maybe same timer thing as layers?)
        private void stopStem()
        {
            if (stemTimer.IsEnabled) stemTimer.Stop();
            if (s1 != null) s1.stop();
            if (s2 != null) s2.stop();
            if (s3 != null) s3.stop();
            if (s4 != null) s4.stop();
        }
        


        // Handles the clicking of the minimal button
        private void Minimal_Click(object sender, RoutedEventArgs e)
        {
            l1.select(layerSeek.Value, layerPlaying);
            if (l2 != null)l2.deselect(layerPlaying);
            if (l3 != null) l3.deselect(layerPlaying);
            if (l4 != null) l4.deselect(layerPlaying);
            currentLayer = l1;
            LayerButton.IsEnabled = true; // Only allow layer play button to be clickable when a layer is selected
        }

        //See Minimal_Click
        private void Tension_Click(object sender, RoutedEventArgs e)
        {
            l2.select(layerSeek.Value, layerPlaying);
            if (l1 != null) l1.deselect(layerPlaying);
            if (l3 != null) l3.deselect(layerPlaying);
            if (l4 != null)l4.deselect(layerPlaying);
            currentLayer = l2;
            LayerButton.IsEnabled = true; // Only allow layer play button to be clickable when a layer is selected
        }

        // See Minimal_click
        private void Danger_Click(object sender, RoutedEventArgs e)
        {
            l3.select(layerSeek.Value, layerPlaying);
            if (l1 != null) l1.deselect(layerPlaying);
            if (l2 != null) l2.deselect(layerPlaying);
            if (l4 != null) l4.deselect(layerPlaying);
            currentLayer = l3;
            LayerButton.IsEnabled = true; // Only allow layer play button to be clickable when a layer is selected
        }

        // See Minimal_click
        private void Action_Click(object sender, RoutedEventArgs e)
        {
            l4.select(layerSeek.Value, layerPlaying);
            if (l1 != null) l1.deselect(layerPlaying);
            if (l2 != null) l2.deselect(layerPlaying);
            if (l3 != null) l3.deselect(layerPlaying);
            currentLayer = l4;
            LayerButton.IsEnabled = true; // Only allow layer play button to be clickable when a layer is selected
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
                l1 = new Layer(filename, bf, dev, ref Minimal);
                if(layerSeek.Maximum < l1.Length) layerSeek.Maximum = l1.Length;
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
                l2 = new Layer(filename, bf, dev, ref Tension);
                if (layerSeek.Maximum < l2.Length) layerSeek.Maximum = l2.Length;
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
                l3 = new Layer(filename, bf, dev, ref Danger);
                if (layerSeek.Maximum < l3.Length) layerSeek.Maximum = l3.Length;
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
                l4 = new Layer(filename, bf, dev, ref Action);
                if (layerSeek.Maximum < l4.Length) layerSeek.Maximum = l4.Length;
                //Load wav from filename;
                Action.IsEnabled = true;
            }
        }

        //Handles layer slider updating
        void layer_Tick (object sender, EventArgs e)
        {
            if(!layerIsDragging){
                layerSeek.Value = currentLayer.getPosition();
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
            stopLayer();
            currentLayer.play(layerSeek.Value);
            layerTimer.Start();
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
                s1 = new Stem(filename, bf, dev, ref A);
                if (stemSeek.Maximum < s1.Length) stemSeek.Maximum = s1.Length;
                A.IsEnabled = true;
                StemButton.IsEnabled = true;
                A.Background = WhiteFill;
                A.Foreground = BlackFill;
                if (stemPlaying)
                {
                    s1.play();
                }
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
                s2 = new Stem(filename, bf, dev, ref B);
                if (stemSeek.Maximum < s2.Length) stemSeek.Maximum = s2.Length;
                B.IsEnabled = true;
                StemButton.IsEnabled = true;
                B.Background = WhiteFill;
                B.Foreground = BlackFill;
                if (stemPlaying)
                {
                    s2.play();
                }
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
                s3 = new Stem(filename, bf, dev, ref C);
                if (stemSeek.Maximum < s3.Length) stemSeek.Maximum = s3.Length;
                C.IsEnabled = true;
                StemButton.IsEnabled = true;
                C.Background = WhiteFill;
                C.Foreground = BlackFill;
                if (stemPlaying)
                {
                    s3.play();
                }
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
                s4 = new Stem(filename, bf, dev, ref D);
                if (stemSeek.Maximum < s4.Length) stemSeek.Maximum = s4.Length;
                D.IsEnabled = true;
                StemButton.IsEnabled = true;
                D.Background = WhiteFill;
                D.Foreground = BlackFill;
                if (stemPlaying)
                {
                    s4.play();
                }
            }
        }

        // Handles toggling of stem A, and enables boolean for fade (All stems are all playing while pause is false, they are just silent, fading or max.)
        private void A_Click(object sender, RoutedEventArgs e)
        {
            s1.select(stemSeek.Value, stemPlaying);
        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            s2.select(stemSeek.Value, stemPlaying);
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            s3.select(stemSeek.Value, stemPlaying);
        }

        private void D_Click(object sender, RoutedEventArgs e)
        {
            s4.select(stemSeek.Value, stemPlaying);
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
            stopStem();
            if (s1 != null) s1.play(stemSeek.Value);
            if (s2 != null) s2.play(stemSeek.Value);
            if (s3 != null) s3.play(stemSeek.Value);
            if (s4 != null) s4.play(stemSeek.Value);
            stemTimer.Start();
        }
    }
}
