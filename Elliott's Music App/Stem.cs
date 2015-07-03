using Elliott_s_Music_App;
using Microsoft.DirectX.AudioVideoPlayback;
using Microsoft.DirectX.DirectSound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Elliotts_Music_App
{
    class Stem
    {

        private double stemCrossElapsed = MainWindow.crossTime.TotalMilliseconds;

        DispatcherTimer stemTimer;
        private bool fadeIn = false;
        private bool fadeOut = false;

        private bool isSelected;

        private double length;

        public double Length
        {
            get { return length; }
            set { length = value; }
        }
        private int bytesPerSecond;

        public SecondaryBuffer buf;

        Button button;

        public Stem(String filename, BufferDescription bf, Device dev, ref Button button)
        {
            buf = new SecondaryBuffer(filename, bf, dev);
            this.button = button;

            stemTimer = new DispatcherTimer(); // Intitalises timer for every 5 milliseconds to call stem_tick. DOES NOT START TIMER YET
            stemTimer.Interval = TimeSpan.FromMilliseconds(100);
            stemTimer.Tick += new EventHandler(stem_Tick);



            bytesPerSecond = buf.Format.Channels * buf.Format.SamplesPerSecond * (buf.Format.BitsPerSample / 8);


            length = (new Audio(filename, false).Duration) * bytesPerSecond;

            buf.SetCurrentPosition(0);

            isSelected = true;
        }

        public void select(double playPosition, bool isPlaying){
            if (!isSelected)
            {
                buf.SetCurrentPosition((int)Math.Round(playPosition));
                isSelected = true;
                if (isPlaying)
                {
                    if (fadeOut) fadeOut = false;
                    else
                    {
                        buf.Volume = -10000;
                        buf.Play(0, BufferPlayFlags.Looping);
                        stemTimer.Start();
                    }
                    fadeIn = true;

                }
                else
                {
                    stemCrossElapsed = MainWindow.crossTime.TotalMilliseconds;
                }
                button.Background = MainWindow.WhiteFill;
                button.Foreground = MainWindow.BlackFill;
                button.Content = "Mute " + button.Name;
            }
            else
            {
                    isSelected = false;
                    if (isPlaying)
                    {
                        if (fadeIn) fadeIn = false;
                        else
                        {
                            buf.Volume = 0;
                            stemTimer.Start();
                        }
                        fadeOut = true;
                    }
                    else
                    {
                        stemCrossElapsed = 0;
                    }
                    button.Background = MainWindow.BlackFill;
                    button.Foreground = MainWindow.WhiteFill;
                    button.Content = "Unmute " + button.Name;
            }
        }

        public void play()
        {
            if (isSelected)
            {
                buf.Volume = 0;
            }
            buf.Play(0, BufferPlayFlags.Looping);
        }

        public void play(double playPosition)
        {
            if (isSelected)
            {
                buf.SetCurrentPosition((int)(playPosition));
                buf.Volume = 0;
                buf.Play(0, BufferPlayFlags.Looping);
            }
        }

        public void stop()
        {
            buf.Stop();
            buf.Volume = -10000;
            fadeIn = false;
            fadeOut = false;
            stemTimer.Stop();
        }

        private void stem_Tick(object sender, EventArgs e)
        {
            Console.WriteLine(stemCrossElapsed);
            if (fadeIn)
            {
                double vol;
                stemCrossElapsed += stemTimer.Interval.TotalMilliseconds;
                if (buf.Volume != 0 || stemCrossElapsed < MainWindow.crossTime.TotalMilliseconds)
                {
                    //Fade in
                    vol = (10000 * Math.Log10(stemCrossElapsed / MainWindow.crossTime.TotalMilliseconds));
                    if (vol < -10000 || double.IsNaN(vol)) vol = -10000;
                    if (vol > 0) vol = 0;
                    buf.Volume = (int)vol;
                }else{ //Fade in complete
                    buf.Volume = 0;
                    stemCrossElapsed = MainWindow.crossTime.TotalMilliseconds;
                    fadeIn = false;
                    stemTimer.Stop();
                }
            }
            else if (fadeOut)
            {
                double vol;
                stemCrossElapsed -= stemTimer.Interval.TotalMilliseconds;
                if (buf.Volume != -10000 || stemCrossElapsed > 0)
                {
                    //Fade out
                    vol = (10000 * Math.Log10(stemCrossElapsed / MainWindow.crossTime.TotalMilliseconds));
                    if (vol < -10000 || double.IsNaN(vol)) vol = -10000;
                    if (vol > 0) vol = 0;
                    buf.Volume = (int)vol;
                }else{ // Fade out complete
                    buf.Volume = -10000;
                    stemCrossElapsed = 0;
                    fadeOut = false;
                    stemTimer.Stop();
                }
            }
        }



        public double getPosition()
        {
            return ((double)buf.PlayPosition);
        }

        public bool FadeIn
        {
            get { return fadeIn; }
            set { fadeIn = value; }
        }

        public bool FadeOut
        {
            get { return fadeOut; }
            set { fadeOut = value; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; }
        }
    }
}
