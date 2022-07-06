using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Drawing.Drawing2D;

namespace Gunolandia
{
    [DesignerCategory("")]
    internal class Window : Form
    {

        enum State { 
            Game, Menu, Pause, Loading, Splash, Choose_Character
        }

        private BufferedGraphics buffer;
        private long counter_frequency;
        private long start_counter;
        private Timer fps_timer = new Timer(300);
        private SpriteSheet main_menu = new SpriteSheet("assets/ecra.png", 528, 254, 4, 100, 2);
        private float display_ms;
        private ulong delta_us;
        private World world = new World();
        private ulong qpc_refresh_period = 0;
        private bool is_sleep_granular = false;
        private AudioBuffer audio_buffer;
        private SourceVoice menu_music_voice;
        private State state = State.Splash;
        private PrivateFontCollection main_pfc = new PrivateFontCollection();
        private PrivateFontCollection minecraft_pfc = new PrivateFontCollection();
        private Font main_font;
        private Font minecraft_font;
        private Layout pause_layout = new Layout(20, Gunolandia.Layout.Type.CenterCenter);
        private Layout choose_character_layout = new Layout(20, Gunolandia.Layout.Type.CenterCenter);
        private Layout main_menu_layout = new Layout(20, Gunolandia.Layout.Type.CenterCenter);
        private Layout game_layout = new Layout(20, Gunolandia.Layout.Type.LeftTop);
        private Player loading_guna = new Player();
        private Player loading_filosofo = new Player();
        private Timer loading_timer = new Timer(8 * 1000);
        private Timer filosofo_timer = new Timer(1500);
        private Timer splash_timer_loading = new Timer(500);
        private float loading_filosofo_pos = 0;
        private float loading_fumo_pos = 0;
        private Bitmap filosofo_texto = Utils.ResizeImage(new Bitmap("assets/filosofo_texto.png"), 3);
        private Bitmap fumo = Utils.ResizeImage(new Bitmap("assets/fumo.png"), 3);
        private Bitmap splash_screen = Utils.ResizeImage(new Bitmap("assets/SplashScreen.png"), 1);
        private Bitmap progress;
        private Bitmap progress_filled;
        private Size progress_size;
        private int progress_margin = 10;
        private float loading_progress = 0;
        private float target_loading_progress = 0;
        private Random rng = new Random();

        enum PlayerChoice { 
            Filosofo, Beto, Guna
        }
        private PlayerChoice player_selected = PlayerChoice.Guna;

        private AudioBuffer CreateAudioBuffer(DataStream data_stream)
        {
            return new AudioBuffer() { Stream = data_stream, AudioBytes = (int)data_stream.Length, Flags = BufferFlags.EndOfStream };
        }

        public Window()
        {
            Text = "Gunolândia";
            SetStyle(ControlStyles.ResizeRedraw, true);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = splash_screen.Size;
            Resize += Window_Resize;
            MouseDown += Window_MouseDown;
            MouseUp += Window_MouseUp;
            KeyDown += Window_KeyDown;

            buffer = BufferedGraphicsManager.Current.Allocate(CreateGraphics(), ClientRectangle);
            Paint += Window_Paint;

            {
                var timing_info = new DWM_TIMING_INFO() { cbSize = (uint)Marshal.SizeOf(typeof(DWM_TIMING_INFO)) };
                var error = DwmGetCompositionTimingInfo(IntPtr.Zero, ref timing_info);
                if (error == 0)
                {
                    Debug.WriteLine(String.Format("[DWMAPI]: {0} Hz, {1} RefreshPeriod", timing_info.rateRefresh.uiNumerator / timing_info.rateRefresh.uiDenominator, timing_info.qpcRefreshPeriod));
                    qpc_refresh_period = timing_info.qpcRefreshPeriod;
                }
            }

            {
                TimeCaps timecaps = new TimeCaps();
                var error = timeGetDevCaps(ref timecaps, (uint)Marshal.SizeOf(typeof(TimeCaps)));
                if (error == 0)
                {
                    Debug.WriteLine(String.Format("[WINMM]: Min Resolution: {0}, Max Resolution {1}", timecaps.wPeriodMin, timecaps.wPeriodMax));
                }
            }

            {
                var error = timeBeginPeriod(1);
                if (error == 0)
                {
                    Debug.WriteLine("[WINMM]: Sleep is granular!");
                    is_sleep_granular = true;
                }
                else
                {
                    Debug.WriteLine("[WINMM]: Sleep is not granular! ;(");
                }
            }

            Application.Idle += new EventHandler(Application_Idle);
            world.Generate();

            var device = new XAudio2();
            var mastering_voice = new MasteringVoice(device);
            mastering_voice.GetVoiceDetails(out var voice_details);
            var channel_num = voice_details.InputChannelCount;
            var sample_rate = voice_details.InputSampleRate;
            Debug.WriteLine($"[XAUDIO2]: Channel Num: {channel_num}, Sample Rate: {sample_rate}");
            using (var stream = new SoundStream(File.OpenRead("assets/pols.wav")))
            {
                var wave_format = new WaveFormat(sample_rate, channel_num);
                menu_music_voice = new SourceVoice(device, stream.Format);
                audio_buffer = CreateAudioBuffer(stream);
                menu_music_voice.SubmitSourceBuffer(audio_buffer, stream.DecodedPacketsInfo);
            }
            
            progress_size = new Size(ClientWidth - 2 * progress_margin, 50);
            progress = Utils.BitmapFromNinePatch(new Bitmap(@"assets/Progress.9.png"), progress_size, 2);
            progress_filled = Utils.BitmapFromNinePatch(new Bitmap(@"assets/ProgressFilled.9.png"), progress_size, 2);

            main_pfc.AddFontFile("assets/Pixellari.ttf");
            main_font = new Font(main_pfc.Families[0], 12);
            minecraft_pfc.AddFontFile("assets/Minecraft.ttf");
            minecraft_font = new Font(minecraft_pfc.Families[0], 12);

            var play_btn = new Button(Button.ClickType.Normal, "Play", main_font);
            main_menu_layout.Add(play_btn, (x) => {
                if (x > 0) state = State.Game;
            });
            var settings_btn = new Button(Button.ClickType.Normal, "Settings", main_font);
            main_menu_layout.Add(settings_btn, (x) => {
                if (x > 0) MessageBox.Show("Ainda não há settings, desculpe pelo incomodo! Carrege em Resume");
            });
            var choose_character_btn = new Button(Button.ClickType.Normal, "Choose Character", main_font);
            main_menu_layout.Add(choose_character_btn, (x) => {
                if (x > 0) state = State.Choose_Character;
            });

            main_menu_layout.CalculateRects(ClientWidth / 2, ClientHeight / 2);

            var resume_btn = new Button(Button.ClickType.Normal, "Resume", main_font);
            pause_layout.Add(resume_btn, (x) =>
            {
                if (x > 0) state = State.Game;
            });
            pause_layout.Add(settings_btn, (x) => {
                if (x > 0) MessageBox.Show("Ainda não há settings, desculpe pelo incomodo! Carrege em Resume");
            });

            pause_layout.CalculateRects(ClientWidth / 2, ClientHeight / 2);

            var mute_btn = new Button(Button.ClickType.Toggle, "Mute", main_font);
            game_layout.Add(mute_btn, (x) =>
            {
                if (x == 2)
                {
                    menu_music_voice.Stop();
                }
                else if (x == 1)
                {
                    menu_music_voice.Start();
                }
            });
            game_layout.CalculateRects(50, 50);

            var choose_filosofo_btn = new Button(Button.ClickType.Normal, "Choose Filosofo", main_font);
            var choose_beto_btn = new Button(Button.ClickType.Normal, "Choose Beto", main_font);
            var choose_guna_btn = new Button(Button.ClickType.Normal, "Choose Guna", main_font);
            choose_character_layout.Add(choose_filosofo_btn, (x) => {
                player_selected = PlayerChoice.Filosofo;
            });
            choose_character_layout.Add(choose_beto_btn, (x) => {
                player_selected = PlayerChoice.Beto;
            });
            choose_character_layout.Add(choose_guna_btn, (x) => {
                player_selected = PlayerChoice.Guna;
            });
            choose_character_layout.CalculateRects(ClientWidth / 2, ClientHeight / 2);

            loading_filosofo.bitmap = new Bitmap("assets/filosofo.png");
            loading_guna.wants_to_smoke = true;
            loading_guna.rotate_flip = true;

            QueryPerformanceCounter(out start_counter);
            QueryPerformanceFrequency(out counter_frequency);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (state == State.Game)
            {
                world.KeyDown(e);
                if (e.KeyCode == Keys.P || e.KeyCode == Keys.Escape)
                {
                    state = State.Pause;
                }
            }
            else if (state == State.Pause)
            { 
                if (e.KeyCode == Keys.P || e.KeyCode == Keys.Escape)
                {
                    state = State.Game;
                }
            }
        }

        private void Window_MouseUp(object sender, MouseEventArgs e)
        {
            if (state == State.Game)
            {
                game_layout.MouseUp(e.Location);
            }
            else if (state == State.Pause)
            {
                pause_layout.MouseUp(e.Location);
            }
            else if (state == State.Menu)
            {
                main_menu_layout.MouseUp(e.Location);
            }
            else if (state == State.Choose_Character)
            {
                choose_character_layout.MouseUp(e.Location);
            }
        }

        private void Window_MouseDown(object sender, MouseEventArgs e)
        {
            if (state == State.Game)
            {
                game_layout.MouseDown(e.Location);
            }
            else if (state == State.Pause)
            {
                pause_layout.MouseDown(e.Location);
            }
            else if (state == State.Menu)
            {
                main_menu_layout.MouseDown(e.Location);
            }
            else if (state == State.Choose_Character)
            {
                choose_character_layout.MouseDown(e.Location);
            }
        }

        private void Window_Paint(object sender, PaintEventArgs e)
        {
            DrawFrame();

            QueryPerformanceCounter(out var end_counter);
            var counter_delta = end_counter - start_counter;
            start_counter = end_counter;
            delta_us = (ulong)(counter_delta * 1e6f / counter_frequency);
        }

        private void Window_Resize(object sender, EventArgs e)
        {
            if (ClientRectangle.Width != 0 && ClientRectangle.Height != 0)
            {
                buffer = BufferedGraphicsManager.Current.Allocate(CreateGraphics(), ClientRectangle);
                main_menu_layout.CalculateRects(ClientWidth / 2, ClientHeight / 2);
                pause_layout.CalculateRects(ClientWidth / 2, ClientHeight / 2);
                choose_character_layout.CalculateRects(ClientWidth / 2, ClientHeight / 2);
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [SuppressUnmanagedCodeSecurity, DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);

        [return: MarshalAs(UnmanagedType.Bool)]
        [SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [return: MarshalAs(UnmanagedType.Bool)]
        [SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        [SuppressUnmanagedCodeSecurity, DllImport("dwmapi.dll", CharSet = CharSet.Auto)]
        private static extern int DwmGetCompositionTimingInfo(IntPtr hwnd, ref DWM_TIMING_INFO pTimingInfo);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern UInt32 timeGetDevCaps(ref TimeCaps timeCaps, UInt32 sizeTimeCaps);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint timeBeginPeriod(uint uMilliseconds);

        void Application_Idle(object sender, EventArgs e)
        {
            while (!PeekMessage(out Message message, IntPtr.Zero, 0, 0, 0))
            {
                UpdateFrame();
                DrawFrame();

                ulong counter_delta;
                bool sleep = true && is_sleep_granular;
                var first_start_counter = start_counter;
                while (true)
                {
                    QueryPerformanceCounter(out var end_counter);
                    counter_delta = (ulong)(end_counter - first_start_counter);
                    start_counter = end_counter;
                    if (counter_delta >= qpc_refresh_period) break;
                    if (sleep)
                    {
                        var sleep_error = 1;
                        var ms_to_sleep = (int)((qpc_refresh_period - counter_delta) * 1e3f / counter_frequency - sleep_error);
                        if (ms_to_sleep > 0)
                        {
                            Thread.Sleep(ms_to_sleep);
                            sleep = false;
                        }
                    }
                }
                delta_us = (ulong)(counter_delta * 1e6f / counter_frequency);
            }
        }

        private int ClientWidth
        {
            get { return ClientSize.Width; }
        }
        private int ClientHeight
        {
            get { return ClientSize.Height; }
        }

        private void UpdateGameState()
        { 
            if (Focused)
            {
                world.Update(delta_us);
            }
        }

        private void DrawGameState(Graphics canvas)
        {
            world.Draw(canvas, ClientWidth, ClientHeight);
            game_layout.Draw(canvas);
        }

        private void DrawPauseState(Graphics canvas)
        {
            using (var brush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
            {
                canvas.FillRectangle(brush, ClientRectangle);
            }
            pause_layout.Draw(canvas);
        }

        private void DrawMenuState(Graphics canvas)
        {
            using (var brush = new SolidBrush(Color.FromArgb(255, 53, 53, 69)))
            {
                canvas.FillRectangle(brush, ClientRectangle);
            }
            var center_x = ClientWidth / 2;
            var center_y = ClientHeight / 2;

            main_menu.IncrementTime(delta_us);
            main_menu.Draw(canvas, center_x, center_y);
            main_menu_layout.Draw(canvas);
            canvas.DrawString("Trabalho realizado no âmbito da disciplina de API por:\nDiogo Araújo Nº7 12ºK\nEdgar Araújo Nº8 12ºK", main_font, Brushes.White, 0, ClientHeight - 55);
        }

        private void DrawChooseCharacterState(Graphics canvas)
        { 
            using (var brush = new SolidBrush(Color.FromArgb(255, 53, 53, 69)))
            {
                canvas.FillRectangle(brush, ClientRectangle);
            }

            choose_character_layout.Draw(canvas);
            canvas.DrawString("Trabalho realizado no âmbito da disciplina de API por:\nDiogo Araújo Nº7 12ºK\nEdgar Araújo Nº8 12ºK", main_font, Brushes.White, 0, ClientHeight - 55);
        }

        private void DrawLoadingState(Graphics canvas)
        { 
            using (var brush = new SolidBrush(Color.FromArgb(255, 41, 42, 48)))
            { 
                canvas.FillRectangle(brush, ClientRectangle);
            }
            var center_x = ClientWidth / 2;
            var center_y = ClientHeight / 2;

            var scale = 4;
            loading_guna.Draw(canvas, scale, center_x + scale * 20, center_y);
            loading_filosofo.Draw(canvas, scale, (int)(loading_filosofo_pos * (center_x - scale * 20)), center_y);

            if (loading_filosofo_pos == 1)
            {
                canvas.DrawImage(filosofo_texto, new Point(center_x - filosofo_texto.Width / 2, center_y + 10 * scale));
            }
        }

        private void UpdateFrame()
        {
            if (state == State.Splash)
            {
                var delta_sec = delta_us / 1e6f;
                loading_progress += (target_loading_progress - loading_progress) * delta_sec;

                if (splash_timer_loading.IncrementTime(delta_us))
                {
                    target_loading_progress = loading_progress + (float)(rng.NextDouble() * 0.5f);
                }
                if (loading_progress >= 1f)
                {
                    ClientSize = new Size(1280, 720);
                    state = State.Loading;
                    CenterToScreen();
                }
            }
        }

        private void DrawFrame()
        {
            // update & render code
            var canvas = buffer.Graphics;
            using (var brush = new SolidBrush(Color.FromArgb(255, 99, 166, 80)))
            { 
                canvas.FillRectangle(brush, ClientRectangle);
            }

            var delta_sec = delta_us / 1e6f;
            if (state == State.Loading && loading_timer.IncrementTime(delta_us))
            {
                state = State.Menu;
            }

            if (state == State.Game)
            {
                UpdateGameState();
                DrawGameState(canvas);
            }
            else if (state == State.Pause)
            {
                DrawGameState(canvas);
                DrawPauseState(canvas);
            }
            else if (state == State.Menu)
            {
                DrawMenuState(canvas);
            }
            else if (state == State.Choose_Character)
            {
                DrawChooseCharacterState(canvas);
            }
            else if (state == State.Splash)
            {
                canvas.DrawImageUnscaled(splash_screen, 0, 0);
                var top = ClientHeight - progress_margin - progress_size.Height;
                canvas.DrawImageUnscaled(progress, progress_margin, top);
                canvas.SetClip(new Rectangle(progress_margin, top, (int)(progress_size.Width * loading_progress), progress_size.Height));
                canvas.DrawImageUnscaled(progress_filled, progress_margin, ClientHeight - progress_margin - progress_size.Height);
                canvas.ResetClip();
            }
            else if (state == State.Loading)
            {
                loading_filosofo_pos += delta_sec / 4f;
                loading_filosofo.velocity.Y = 150f * delta_sec;
                if (loading_filosofo_pos > 1)
                {
                    loading_filosofo_pos = 1;
                    loading_filosofo.velocity.Y = 0;
                    if (filosofo_timer.IncrementTime(delta_us))
                    {
                        loading_filosofo.wants_to_smoke = true;
                    }
                }

                loading_guna.IncrementTime(delta_us);

                loading_filosofo.IncrementTime(delta_us);
                DrawLoadingState(canvas);
            }

            if (loading_filosofo_pos == 1)
            {
                menu_music_voice.Start();
                if (loading_filosofo.wants_to_smoke == true)
                {
                    loading_fumo_pos += delta_sec / 5.5f;
                    if (loading_fumo_pos > 1) {
                        loading_fumo_pos = 1;
                    }
                }
            }

            var center_x = ClientWidth / 2;
            if (loading_fumo_pos != 1 && loading_filosofo.wants_to_smoke == true)
            {
                var bot = ClientHeight + 500;
                var top = -fumo.Height;
                var y_pos = (int)(bot - loading_fumo_pos * (bot - top));
                canvas.DrawImage(fumo, new Point(center_x - fumo.Width / 2, y_pos));
            }

#if DEBUG
            DrawPerfMetrics(canvas);
#endif

            buffer.Render();
        }

        private void DrawPerfMetrics(Graphics canvas)
        { 
            if (fps_timer.IncrementTime(delta_us))
            {
                display_ms = delta_us / 1e3f;
            }

            {
                var text = $"{(1e3f / display_ms):0.00} fps";
                var text_rect = canvas.MeasureString(text, main_font);
                canvas.DrawString(text, main_font, Brushes.White, Width - text_rect.Width - 20, 0);
            }

            {
                var text = $"{display_ms:0.00} ms";
                var text_rect = canvas.MeasureString(text, main_font);
                canvas.DrawString(text, main_font, Brushes.White, Width - text_rect.Width - 20, text_rect.Height);
            }

            {
                var text = $"[XAUDIO2]: Buffers Queued: {menu_music_voice.State.BuffersQueued}";
                var text_rect = canvas.MeasureString(text, main_font);
                canvas.DrawString(text, main_font, Brushes.White, Width - text_rect.Width - 20, text_rect.Height * 2);
            }

        }
    }
}
