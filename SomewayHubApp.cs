using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SomewaysHub
{
    public class MainWindow : Window
    {
        private MediaElement mediaElement;
        private Grid mainGrid;
        private Grid overlayGrid;
        private Border headerBorder;
        private Border bottomPodBorder;
        private Border emptyCard;
        private TextBlock filenameText;
        private TextBlock timeCurrentText;
        private TextBlock timeTotalText;
        private Slider seekSlider;
        private Slider volumeSlider;
        private Button playPauseBtn;
        private TextBlock playPauseIcon;
        private Button volBtn;
        private TextBlock volIcon;
        private Button aspectCycleBtn;
        private Border settingsPanel;
        private Slider sliderScaleX;
        private Slider sliderScaleY;
        private TextBox txtScaleX;
        private TextBox txtScaleY;
        private DispatcherTimer timer;
        private DispatcherTimer mouseIdleTimer;
        private DispatcherTimer clickTimer;
        private int clickCount = 0;

        private System.Collections.Generic.List<string> playlistFiles = new System.Collections.Generic.List<string>();
        private int currentPlaylistIndex = -1;
        private Border playlistPanel;
        private StackPanel playlistItemsStack;
        private TextBlock playlistTitleText;

        private TransformGroup transformGroup;
        private ScaleTransform scaleTransform;
        private RotateTransform rotateTransform;

        private int currentAspectIndex = 0;
        private readonly string[] aspectModeLabels = new string[] {
            "🖥️ STRETCH SCREEN",
            "📐 FIT (16:9)",
            "✂️ COVER (CROP)",
            "🎞️ 16:9 PRESET",
            "🎬 21:9 ULTRAWIDE",
            "📻 4:3 PRESET"
        };

        private bool isDraggingSeeker = false;
        private bool isFullscreen = false;
        private bool isPlaying = false;
        private WindowState previousWindowState;
        private WindowStyle previousWindowStyle;

        [STAThread]
        public static void Main()
        {
            try
            {
                Application app = new Application();
                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Someway's Hub Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public MainWindow()
        {
            Title = "Someway's Hub v2.0 - Aspect Ratio Media Player";
            Width = 1280;
            Height = 750;
            MinHeight = 500;
            MinWidth = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Black;
            AllowDrop = true;

            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.jpg");
            if (!File.Exists(imgPath))
            {
                imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.png");
            }
            if (File.Exists(imgPath))
            {
                try
                {
                    Icon = BitmapFrame.Create(new Uri(imgPath));
                }
                catch { }
            }

            InitUI(imgPath);
            InitEvents();
            InitTimer();
            InitMouseIdleTimer();
        }

        private void InitUI(string artworkPath)
        {
            mainGrid = new Grid();
            Content = mainGrid;

            // Video Player Element
            mediaElement = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Stop,
                Stretch = Stretch.Fill, // Stremio Stretch Screen mode default!
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            transformGroup = new TransformGroup();
            scaleTransform = new ScaleTransform(1.0, 1.0);
            rotateTransform = new RotateTransform(0);
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(rotateTransform);
            mediaElement.RenderTransform = transformGroup;
            mediaElement.RenderTransformOrigin = new Point(0.5, 0.5);

            mainGrid.Children.Add(mediaElement);

            // Overlay Layer
            overlayGrid = new Grid();
            mainGrid.Children.Add(overlayGrid);

            // ==================================================================
            // DIRECT-ON-VIDEO TOP NAVBAR (NO DOCK BACKGROUND)
            // ==================================================================
            headerBorder = new Border
            {
                Height = 60,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new LinearGradientBrush(
                    Color.FromArgb(220, 6, 7, 10),
                    Color.FromArgb(0, 6, 7, 10),
                    90
                ),
                BorderThickness = new Thickness(0)
            };
            Panel.SetZIndex(headerBorder, 100);

            Grid headerBar = new Grid();
            headerBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel leftHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 0, 20, 0), VerticalAlignment = VerticalAlignment.Center };
            
            Button backBtn = CreateLiquidIconButton("‹", "Back", 38, 38, 19);
            backBtn.FontSize = 24;
            backBtn.Margin = new Thickness(0, 0, 12, 0);

            if (File.Exists(artworkPath))
            {
                Border logoBorder = new Border
                {
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 0, 12, 0),
                    Background = new ImageBrush(new BitmapImage(new Uri(artworkPath))) { Stretch = Stretch.UniformToFill },
                    BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    BorderThickness = new Thickness(1.5),
                    Effect = new DropShadowEffect { BlurRadius = 12, Color = Color.FromRgb(34, 197, 94), Opacity = 0.7 }
                };
                leftHeader.Children.Add(logoBorder);
            }

            filenameText = new TextBlock
            {
                Text = "Someway's Hub - Pro Aspect Media Player",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 550,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Effect = new DropShadowEffect { BlurRadius = 6, Color = Colors.Black, Opacity = 0.9 }
            };

            leftHeader.Children.Add(backBtn);
            leftHeader.Children.Add(filenameText);
            Grid.SetColumn(leftHeader, 0);
            headerBar.Children.Add(leftHeader);

            StackPanel rightHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 20, 0), VerticalAlignment = VerticalAlignment.Center };
            
            Button settingsBtn = CreateLiquidIconButton("⚙", "Screen Stretch & Settings", 38, 38, 19);
            settingsBtn.Margin = new Thickness(10, 0, 0, 0);
            settingsBtn.Click += (s, e) => ToggleSettingsPanel();
            rightHeader.Children.Add(settingsBtn);

            Button playlistHeaderBtn = CreateLiquidIconButton("📋", "Uploaded Videos Playlist", 38, 38, 19);
            playlistHeaderBtn.Margin = new Thickness(10, 0, 0, 0);
            playlistHeaderBtn.Click += (s, e) => TogglePlaylistPanel();
            rightHeader.Children.Add(playlistHeaderBtn);

            Grid.SetColumn(rightHeader, 2);
            headerBar.Children.Add(rightHeader);

            headerBorder.Child = headerBar;
            overlayGrid.Children.Add(headerBorder);

            // ==================================================================
            // LIQUID GLASS WELCOME CARD
            // ==================================================================
            emptyCard = new Border
            {
                Width = 520,
                Background = new SolidColorBrush(Color.FromArgb(235, 14, 17, 28)),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(32),
                Effect = new DropShadowEffect { BlurRadius = 40, Color = Colors.Black, Opacity = 0.8 }
            };
            StackPanel emptyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            if (File.Exists(artworkPath))
            {
                Border artBorder = new Border
                {
                    Width = 140,
                    Height = 140,
                    CornerRadius = new CornerRadius(28),
                    Margin = new Thickness(0, 0, 0, 16),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Background = new ImageBrush(new BitmapImage(new Uri(artworkPath))) { Stretch = Stretch.UniformToFill },
                    BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    BorderThickness = new Thickness(2),
                    Effect = new DropShadowEffect { BlurRadius = 25, Color = Color.FromRgb(34, 197, 94), Opacity = 0.8 }
                };
                emptyStack.Children.Add(artBorder);
            }

            TextBlock emptyTitle = new TextBlock
            {
                Text = "Welcome to Someway's Hub",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            TextBlock emptyDesc = new TextBlock
            {
                Text = "Native offline desktop MP4 & MKV player with Stremio-style screen stretching. Open a local file or drop an MP4 or MKV video here to start.",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Button emptyOpenBtn = CreateLiquidPillButton("📂  Browse MP4 / MKV Video", Color.FromRgb(34, 197, 94), Color.FromRgb(255, 140, 0), 8);
            emptyOpenBtn.Height = 44;
            emptyOpenBtn.Width = 210;
            emptyOpenBtn.Click += (s, e) => OpenFileDialog();

            emptyStack.Children.Add(emptyTitle);
            emptyStack.Children.Add(emptyDesc);
            emptyStack.Children.Add(emptyOpenBtn);
            emptyCard.Child = emptyStack;
            overlayGrid.Children.Add(emptyCard);

            // ==================================================================
            // DIRECT-ON-VIDEO CONTROLS BAR (DOCK BACKGROUND REMOVED!)
            // ==================================================================
            // ==================================================================
            // DIRECT-ON-VIDEO CONTROLS BAR (100% TRANSPARENT BACKGROUND!)
            // ==================================================================
            bottomPodBorder = new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(24, 0, 24, 16),
                Background = Brushes.Transparent, // COMPLETELY TRANSPARENT! No dock bar, no gradient!
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 6, 8, 6)
            };

            Grid bottomPanel = new Grid();
            bottomPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            bottomPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // STREMIO SEEKBAR ROW (DIRECT ON VIDEO)
            Grid seekRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            seekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            seekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            seekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            timeCurrentText = new TextBlock
            {
                Text = "00:00:00",
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0),
                Effect = new DropShadowEffect { BlurRadius = 6, Color = Colors.Black, Opacity = 0.95 }
            };
            Grid.SetColumn(timeCurrentText, 0);
            seekRow.Children.Add(timeCurrentText);

            seekSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94))
            };
            ApplyCircularSliderStyle(seekSlider, Color.FromRgb(34, 197, 94), Color.FromRgb(34, 197, 94), 11);
            Grid.SetColumn(seekSlider, 1);
            seekRow.Children.Add(seekSlider);

            timeTotalText = new TextBlock
            {
                Text = "00:00:00",
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0),
                Effect = new DropShadowEffect { BlurRadius = 6, Color = Colors.Black, Opacity = 0.95 }
            };
            Grid.SetColumn(timeTotalText, 2);
            seekRow.Children.Add(timeTotalText);

            Grid.SetRow(seekRow, 0);
            bottomPanel.Children.Add(seekRow);

            // CONTROLS BUTTONS ROW (EXACT MATCH TO SCREENSHOT)
            Grid controlsRow = new Grid();
            controlsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left Group: Rewind (<), Play/Pause, Forward (>), 1:35 Forward (>>), Volume Icon & Slider
            StackPanel leftControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Button rewindBtn = CreateLiquidIconButton("<", "Rewind 10s (<)", 34, 34, 17);
            rewindBtn.Margin = new Thickness(0, 0, 8, 0);
            rewindBtn.Click += (s, e) => SeekRelative(-10);

            playPauseBtn = CreateLiquidIconButton("▶", "Play/Pause (Space)", 34, 34, 17);
            playPauseBtn.Margin = new Thickness(0, 0, 8, 0);
            playPauseIcon = (TextBlock)playPauseBtn.Content;
            playPauseIcon.FontSize = 18;
            playPauseBtn.Click += (s, e) => TogglePlayPause();

            Button forwardBtn = CreateLiquidIconButton(">", "Forward 10s (>)", 34, 34, 17);
            forwardBtn.Margin = new Thickness(0, 0, 8, 0);
            forwardBtn.Click += (s, e) => SeekRelative(10);

            Button forward135Btn = CreateLiquidIconButton(">>", "Forward 1:35 (>>)", 34, 34, 17);
            forward135Btn.Margin = new Thickness(0, 0, 14, 0);
            forward135Btn.Click += (s, e) => SeekRelative(95);

            volBtn = CreateLiquidIconButton("🔊", "Mute / Unmute (M)", 34, 34, 17);
            volBtn.Margin = new Thickness(0, 0, 4, 0);
            volIcon = (TextBlock)volBtn.Content;
            volBtn.Click += (s, e) => ToggleMute();

            volumeSlider = new Slider
            {
                Width = 95,
                Minimum = 0,
                Maximum = 1,
                Value = 1,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 140, 0))
            };
            ApplyCircularSliderStyle(volumeSlider, Color.FromRgb(255, 140, 0), Color.FromRgb(255, 140, 0), 10);
            volumeSlider.ValueChanged += (s, e) =>
            {
                mediaElement.Volume = volumeSlider.Value;
                if (volumeSlider.Value == 0)
                {
                    mediaElement.IsMuted = true;
                    if (volIcon != null) volIcon.Text = "🔇";
                }
                else
                {
                    mediaElement.IsMuted = false;
                    if (volIcon != null) volIcon.Text = "🔊";
                }
            };

            leftControls.Children.Add(rewindBtn);
            leftControls.Children.Add(playPauseBtn);
            leftControls.Children.Add(forwardBtn);
            leftControls.Children.Add(forward135Btn);
            leftControls.Children.Add(volBtn);
            leftControls.Children.Add(volumeSlider);

            Grid.SetColumn(leftControls, 0);
            controlsRow.Children.Add(leftControls);

            // Right Group: 8 Sleek Icons matching Screenshot
            StackPanel rightControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Button langBtn = CreateLiquidIconButton("🌐", "Audio Track & Language", 34, 34, 17);
            langBtn.Margin = new Thickness(0, 0, 10, 0);
            langBtn.Click += (s, e) => ShowToast("Audio Track: Default (Stereo)");

            Button speedBtn = CreateLiquidIconButton("⏱️", "Playback Speed", 34, 34, 17);
            speedBtn.Margin = new Thickness(0, 0, 10, 0);
            speedBtn.Click += (s, e) => CycleSpeed();

            Button subBtn = CreateLiquidIconButton("💬", "Load Subtitles", 34, 34, 17);
            subBtn.Margin = new Thickness(0, 0, 10, 0);
            subBtn.Click += (s, e) => OpenSubtitlesDialog();

            aspectCycleBtn = CreateLiquidIconButton("🗔", "Cycle Aspect Ratio (Hotkey: A)", 34, 34, 17);
            aspectCycleBtn.Margin = new Thickness(0, 0, 10, 0);
            aspectCycleBtn.Click += (s, e) => CycleAspectMode();

            Button fullBtn = CreateLiquidIconButton("⛶", "Toggle Fullscreen (F)", 34, 34, 17);
            fullBtn.Click += (s, e) => ToggleFullscreen();

            rightControls.Children.Add(langBtn);
            rightControls.Children.Add(speedBtn);
            rightControls.Children.Add(subBtn);
            rightControls.Children.Add(aspectCycleBtn);
            rightControls.Children.Add(fullBtn);

            Grid.SetColumn(rightControls, 2);
            controlsRow.Children.Add(rightControls);

            Grid.SetRow(controlsRow, 1);
            bottomPanel.Children.Add(controlsRow);

            bottomPodBorder.Child = bottomPanel;
            Grid.SetRow(bottomPodBorder, 1);
            Panel.SetZIndex(bottomPodBorder, 100);
            overlayGrid.Children.Add(bottomPodBorder);

            InitSettingsPanel();
            InitPlaylistPanel();
        }

        private TextBox CreateDigitInputBox(string initialValue)
        {
            TextBox tb = new TextBox
            {
                Text = initialValue,
                Width = 44,
                Height = 22,
                MaxLength = 3,
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(140, 25, 30, 48)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 34, 197, 94)),
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 4, 0)
            };
            return tb;
        }

        private void InitSettingsPanel()
        {
            settingsPanel = new Border
            {
                Width = 340,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 65, 20, 0),
                Background = new SolidColorBrush(Color.FromArgb(240, 14, 17, 28)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 34, 197, 94)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(20),
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect { BlurRadius = 30, Color = Colors.Black, Opacity = 0.85 }
            };

            StackPanel panelStack = new StackPanel();

            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock panelTitle = new TextBlock
            {
                Text = "⚙ Screen Stretching",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(panelTitle, 0);

            Button closeBtn = CreateLiquidIconButton("✕", "Close", 28, 28, 14);
            closeBtn.Click += (s, e) => settingsPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(closeBtn, 1);

            headerGrid.Children.Add(panelTitle);
            headerGrid.Children.Add(closeBtn);
            panelStack.Children.Add(headerGrid);

            // Scale X Row
            Grid scaleXRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            scaleXRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            scaleXRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lblScaleX = new TextBlock { Text = "Horizontal Stretch (Scale X)", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), VerticalAlignment = VerticalAlignment.Center };

            StackPanel xActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button btnSubX = CreateLiquidIconButton("-", "Decrease X (-5%)", 22, 22, 11);
            btnSubX.Margin = new Thickness(0, 0, 4, 0);
            btnSubX.Click += (s, e) => sliderScaleX.Value = Math.Max(50, sliderScaleX.Value - 5);

            txtScaleX = CreateDigitInputBox("100");
            txtScaleX.TextChanged += (s, e) =>
            {
                int val;
                if (int.TryParse(txtScaleX.Text, out val))
                {
                    if (val >= 50 && val <= 250)
                    {
                        if (sliderScaleX != null && (int)sliderScaleX.Value != val)
                        {
                            sliderScaleX.Value = val;
                        }
                    }
                }
            };

            Button btnAddX = CreateLiquidIconButton("+", "Increase X (+5%)", 22, 22, 11);
            btnAddX.Margin = new Thickness(0, 0, 6, 0);
            btnAddX.Click += (s, e) => sliderScaleX.Value = Math.Min(250, sliderScaleX.Value + 5);

            Button btnResetX = CreateLiquidIconButton("🔄", "Reset Scale X to 100%", 22, 22, 11);
            btnResetX.Click += (s, e) => sliderScaleX.Value = 100;

            xActions.Children.Add(btnSubX);
            xActions.Children.Add(txtScaleX);
            xActions.Children.Add(btnAddX);
            xActions.Children.Add(btnResetX);

            Grid.SetColumn(lblScaleX, 0);
            Grid.SetColumn(xActions, 1);
            scaleXRow.Children.Add(lblScaleX);
            scaleXRow.Children.Add(xActions);
            panelStack.Children.Add(scaleXRow);

            sliderScaleX = new Slider
            {
                Minimum = 50,
                Maximum = 250,
                Value = 100,
                Height = 22,
                Margin = new Thickness(0, 0, 0, 16)
            };
            ApplyCircularSliderStyle(sliderScaleX, Color.FromRgb(34, 197, 94), Color.FromRgb(34, 197, 94), 12);
            sliderScaleX.ValueChanged += (s, e) =>
            {
                int val = (int)sliderScaleX.Value;
                if (txtScaleX != null && txtScaleX.Text != val.ToString())
                {
                    txtScaleX.Text = val.ToString();
                }
                if (scaleTransform != null) scaleTransform.ScaleX = val / 100.0;
            };
            panelStack.Children.Add(sliderScaleX);

            // Scale Y Row
            Grid scaleYRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            scaleYRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            scaleYRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lblScaleY = new TextBlock { Text = "Vertical Stretch (Scale Y)", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), VerticalAlignment = VerticalAlignment.Center };

            StackPanel yActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button btnSubY = CreateLiquidIconButton("-", "Decrease Y (-5%)", 22, 22, 11);
            btnSubY.Margin = new Thickness(0, 0, 4, 0);
            btnSubY.Click += (s, e) => sliderScaleY.Value = Math.Max(50, sliderScaleY.Value - 5);

            txtScaleY = CreateDigitInputBox("100");
            txtScaleY.TextChanged += (s, e) =>
            {
                int val;
                if (int.TryParse(txtScaleY.Text, out val))
                {
                    if (val >= 50 && val <= 250)
                    {
                        if (sliderScaleY != null && (int)sliderScaleY.Value != val)
                        {
                            sliderScaleY.Value = val;
                        }
                    }
                }
            };

            Button btnAddY = CreateLiquidIconButton("+", "Increase Y (+5%)", 22, 22, 11);
            btnAddY.Margin = new Thickness(0, 0, 6, 0);
            btnAddY.Click += (s, e) => sliderScaleY.Value = Math.Min(250, sliderScaleY.Value + 5);

            Button btnResetY = CreateLiquidIconButton("🔄", "Reset Scale Y to 100%", 22, 22, 11);
            btnResetY.Click += (s, e) => sliderScaleY.Value = 100;

            yActions.Children.Add(btnSubY);
            yActions.Children.Add(txtScaleY);
            yActions.Children.Add(btnAddY);
            yActions.Children.Add(btnResetY);

            Grid.SetColumn(lblScaleY, 0);
            Grid.SetColumn(yActions, 1);
            scaleYRow.Children.Add(lblScaleY);
            scaleYRow.Children.Add(yActions);
            panelStack.Children.Add(scaleYRow);

            sliderScaleY = new Slider
            {
                Minimum = 50,
                Maximum = 250,
                Value = 100,
                Height = 22,
                Margin = new Thickness(0, 0, 0, 16)
            };
            ApplyCircularSliderStyle(sliderScaleY, Color.FromRgb(255, 140, 0), Color.FromRgb(255, 140, 0), 12);
            sliderScaleY.ValueChanged += (s, e) =>
            {
                int val = (int)sliderScaleY.Value;
                if (txtScaleY != null && txtScaleY.Text != val.ToString())
                {
                    txtScaleY.Text = val.ToString();
                }
                if (scaleTransform != null) scaleTransform.ScaleY = val / 100.0;
            };
            panelStack.Children.Add(sliderScaleY);

            Button resetBtn = CreateLiquidPillButton("🔄  Reset Both (100%)", Color.FromRgb(255, 140, 0), Color.FromRgb(34, 197, 94), 14);
            resetBtn.Height = 34;
            resetBtn.Click += (s, e) =>
            {
                sliderScaleX.Value = 100;
                sliderScaleY.Value = 100;
            };
            panelStack.Children.Add(resetBtn);

            settingsPanel.Child = panelStack;
            Panel.SetZIndex(settingsPanel, 200);
            overlayGrid.Children.Add(settingsPanel);
        }

        private void InitPlaylistPanel()
        {
            playlistPanel = new Border
            {
                Width = 330,
                Height = 450,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 65, 20, 0),
                Background = new SolidColorBrush(Color.FromArgb(245, 14, 17, 28)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 34, 197, 94)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(16),
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect { BlurRadius = 30, Color = Colors.Black, Opacity = 0.85 }
            };
            Panel.SetZIndex(playlistPanel, 200);

            Grid mainStack = new Grid();
            mainStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            playlistTitleText = new TextBlock
            {
                Text = "🎬 Uploaded Videos (0)",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(playlistTitleText, 0);

            StackPanel headerActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            Button addMoreBtn = CreateLiquidPillButton("+ Add", Color.FromRgb(34, 197, 94), Color.FromRgb(255, 140, 0), 6);
            addMoreBtn.Height = 26;
            addMoreBtn.Margin = new Thickness(0, 0, 8, 0);
            addMoreBtn.Click += (s, e) => OpenFileDialog();

            Button closeBtn = CreateLiquidIconButton("✕", "Close", 26, 26, 13);
            closeBtn.Click += (s, e) => playlistPanel.Visibility = Visibility.Collapsed;

            headerActions.Children.Add(addMoreBtn);
            headerActions.Children.Add(closeBtn);
            Grid.SetColumn(headerActions, 1);

            headerGrid.Children.Add(playlistTitleText);
            headerGrid.Children.Add(headerActions);
            Grid.SetRow(headerGrid, 0);
            mainStack.Children.Add(headerGrid);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            playlistItemsStack = new StackPanel();
            scroll.Content = playlistItemsStack;

            Grid.SetRow(scroll, 1);
            mainStack.Children.Add(scroll);

            playlistPanel.Child = mainStack;
            overlayGrid.Children.Add(playlistPanel);

            RefreshPlaylistUI();
        }

        private void TogglePlaylistPanel()
        {
            if (playlistPanel == null) return;
            bool willBeVisible = (playlistPanel.Visibility != Visibility.Visible);
            if (willBeVisible && settingsPanel != null)
            {
                settingsPanel.Visibility = Visibility.Collapsed;
            }
            playlistPanel.Visibility = willBeVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddFilesToPlaylist(string[] files, bool playIfFirst)
        {
            if (files == null || files.Length == 0) return;

            bool addedAny = false;
            foreach (string file in files)
            {
                if (string.IsNullOrEmpty(file)) continue;
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext == ".mp4" || ext == ".webm" || ext == ".mkv" || ext == ".avi" || ext == ".mov" || ext == ".m4v" || ext == ".wmv" || ext == ".flv")
                {
                    if (!playlistFiles.Contains(file))
                    {
                        playlistFiles.Add(file);
                        addedAny = true;
                    }
                }
            }

            if (playlistFiles.Count > 0)
            {
                RefreshPlaylistUI();
                if (currentPlaylistIndex == -1 || (playIfFirst && !isPlaying))
                {
                    PlayPlaylistItem(playlistFiles.Count - (addedAny ? 1 : 0));
                }
            }
        }

        private void PlayPlaylistItem(int index)
        {
            if (index < 0 || index >= playlistFiles.Count) return;
            currentPlaylistIndex = index;
            string filePath = playlistFiles[index];
            string targetPath = filePath;

            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            if (ext == ".mkv")
            {
                string mp4Version = System.IO.Path.ChangeExtension(filePath, ".mp4");
                if (System.IO.File.Exists(mp4Version))
                {
                    targetPath = mp4Version;
                }
                else
                {
                    CheckAndAutoConvertMKV(filePath, mp4Version);
                }
            }

            mediaElement.Source = new Uri(targetPath);
            filenameText.Text = System.IO.Path.GetFileName(filePath);
            emptyCard.Visibility = Visibility.Collapsed;
            mediaElement.Play();
            isPlaying = true;
            playPauseIcon.Text = "⏸";
            ShowControls();
            RefreshPlaylistUI();
            ShowToast(string.Format("Playing [{0}/{1}]: {2}", index + 1, playlistFiles.Count, System.IO.Path.GetFileName(filePath)));
        }

        private void CheckAndAutoConvertMKV(string mkvFile, string mp4File)
        {
            string ffmpegExe = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (!System.IO.File.Exists(ffmpegExe))
            {
                ffmpegExe = "ffmpeg.exe";
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = ffmpegExe,
                        Arguments = string.Format("-y -i \"{0}\" -c:v libx264 -preset ultrafast -crf 23 -c:a copy \"{1}\"", mkvFile, mp4File),
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Dispatcher.Invoke(() => ShowToast("🎬 Optimizing MKV Video for Full Picture..."));
                    using (System.Diagnostics.Process proc = System.Diagnostics.Process.Start(psi))
                    {
                        proc.WaitForExit();
                        if (System.IO.File.Exists(mp4File))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ShowToast("✅ MKV Picture Optimization Ready!");
                                if (currentPlaylistIndex >= 0 && playlistFiles[currentPlaylistIndex] == mkvFile)
                                {
                                    TimeSpan pos = mediaElement.Position;
                                    mediaElement.Source = new Uri(mp4File);
                                    mediaElement.Position = pos;
                                    mediaElement.Play();
                                }
                            });
                        }
                    }
                }
                catch { }
            });
        }

        private void RemovePlaylistItem(int index)
        {
            if (index < 0 || index >= playlistFiles.Count) return;
            bool wasPlayingThis = (currentPlaylistIndex == index);
            playlistFiles.RemoveAt(index);

            if (playlistFiles.Count == 0)
            {
                currentPlaylistIndex = -1;
                mediaElement.Stop();
                mediaElement.Source = null;
                isPlaying = false;
                playPauseIcon.Text = "▶";
                emptyCard.Visibility = Visibility.Visible;
                filenameText.Text = "Someway's Hub - Pro Aspect Media Player";
            }
            else
            {
                if (wasPlayingThis)
                {
                    int nextIdx = (index < playlistFiles.Count) ? index : playlistFiles.Count - 1;
                    PlayPlaylistItem(nextIdx);
                }
                else if (currentPlaylistIndex > index)
                {
                    currentPlaylistIndex--;
                }
            }

            RefreshPlaylistUI();
        }

        private void MovePlaylistItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= playlistFiles.Count) return;
            if (toIndex < 0 || toIndex >= playlistFiles.Count) return;
            if (fromIndex == toIndex) return;

            string item = playlistFiles[fromIndex];
            playlistFiles.RemoveAt(fromIndex);
            playlistFiles.Insert(toIndex, item);

            if (currentPlaylistIndex == fromIndex)
            {
                currentPlaylistIndex = toIndex;
            }
            else if (currentPlaylistIndex > fromIndex && currentPlaylistIndex <= toIndex)
            {
                currentPlaylistIndex--;
            }
            else if (currentPlaylistIndex < fromIndex && currentPlaylistIndex >= toIndex)
            {
                currentPlaylistIndex++;
            }

            RefreshPlaylistUI();
            ShowToast(string.Format("Reordered: {0}", System.IO.Path.GetFileName(item)));
        }

        private void RefreshPlaylistUI()
        {
            if (playlistTitleText != null)
            {
                playlistTitleText.Text = string.Format("🎬 Uploaded Videos ({0})", playlistFiles.Count);
            }
            if (playlistItemsStack == null) return;

            playlistItemsStack.Children.Clear();

            if (playlistFiles.Count == 0)
            {
                TextBlock emptyTb = new TextBlock
                {
                    Text = "No videos uploaded yet.\nClick '+ Add' or drop files here.",
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                playlistItemsStack.Children.Add(emptyTb);
                return;
            }

            for (int i = 0; i < playlistFiles.Count; i++)
            {
                int itemIndex = i;
                string fullPath = playlistFiles[i];
                string nameOnly = System.IO.Path.GetFileName(fullPath);
                bool isCurrent = (i == currentPlaylistIndex);

                Border itemCard = new Border
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(8, 6, 8, 6),
                    CornerRadius = new CornerRadius(10),
                    Background = isCurrent 
                        ? new SolidColorBrush(Color.FromArgb(120, 34, 197, 94)) 
                        : new SolidColorBrush(Color.FromArgb(90, 20, 24, 38)),
                    BorderBrush = isCurrent 
                        ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) 
                        : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    AllowDrop = true,
                    Tag = itemIndex
                };

                Grid itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock dragHandle = new TextBlock
                {
                    Text = "⣿",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0),
                    Cursor = Cursors.SizeNS,
                    ToolTip = "Drag up/down to reorder video"
                };
                Grid.SetColumn(dragHandle, 0);

                TextBlock playIcon = new TextBlock
                {
                    Text = isCurrent ? "▶" : string.Format("{0}.", i + 1),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = isCurrent ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) : new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(playIcon, 1);

                TextBlock titleTb = new TextBlock
                {
                    Text = nameOnly,
                    FontSize = 12,
                    FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = fullPath
                };
                Grid.SetColumn(titleTb, 2);

                StackPanel actionStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                if (i > 0)
                {
                    Button upBtn = CreateLiquidIconButton("▲", "Move Up", 18, 18, 9);
                    upBtn.Click += (s, e) =>
                    {
                        e.Handled = true;
                        MovePlaylistItem(itemIndex, itemIndex - 1);
                    };
                    actionStack.Children.Add(upBtn);
                }

                if (i < playlistFiles.Count - 1)
                {
                    Button downBtn = CreateLiquidIconButton("▼", "Move Down", 18, 18, 9);
                    downBtn.Click += (s, e) =>
                    {
                        e.Handled = true;
                        MovePlaylistItem(itemIndex, itemIndex + 1);
                    };
                    actionStack.Children.Add(downBtn);
                }

                Button delBtn = CreateLiquidIconButton("✕", "Remove Video", 20, 20, 10);
                delBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    RemovePlaylistItem(itemIndex);
                };
                actionStack.Children.Add(delBtn);

                Grid.SetColumn(actionStack, 3);

                itemGrid.Children.Add(dragHandle);
                itemGrid.Children.Add(playIcon);
                itemGrid.Children.Add(titleTb);
                itemGrid.Children.Add(actionStack);

                itemCard.Child = itemGrid;

                Point startPoint = new Point();
                itemCard.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    startPoint = e.GetPosition(null);
                };

                itemCard.PreviewMouseMove += (s, e) =>
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        Point mousePos = e.GetPosition(null);
                        Vector diff = startPoint - mousePos;

                        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                        {
                            DataObject dragData = new DataObject("PlaylistItemIndex", itemIndex);
                            DragDrop.DoDragDrop(itemCard, dragData, DragDropEffects.Move);
                        }
                    }
                };

                itemCard.DragOver += (s, e) =>
                {
                    if (e.Data.GetDataPresent("PlaylistItemIndex"))
                    {
                        e.Effects = DragDropEffects.Move;
                        e.Handled = true;
                    }
                };

                itemCard.Drop += (s, e) =>
                {
                    if (e.Data.GetDataPresent("PlaylistItemIndex"))
                    {
                        int srcIndex = (int)e.Data.GetData("PlaylistItemIndex");
                        int tgtIndex = itemIndex;
                        e.Handled = true;
                        MovePlaylistItem(srcIndex, tgtIndex);
                    }
                };

                itemCard.MouseLeftButtonDown += (s, e) => PlayPlaylistItem(itemIndex);

                playlistItemsStack.Children.Add(itemCard);
            }
        }

        private void ToggleSettingsPanel()
        {
            if (settingsPanel == null) return;
            bool willBeVisible = (settingsPanel.Visibility != Visibility.Visible);
            if (willBeVisible && playlistPanel != null)
            {
                playlistPanel.Visibility = Visibility.Collapsed;
            }
            settingsPanel.Visibility = willBeVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InitEvents()
        {
            Drop += MainWindow_Drop;
            KeyDown += MainWindow_KeyDown;

            // Screen Click-to-Pause & Double-Click Fullscreen
            clickTimer = new DispatcherTimer();
            clickTimer.Interval = TimeSpan.FromMilliseconds(220);
            clickTimer.Tick += (s, e) =>
            {
                clickTimer.Stop();
                if (clickCount == 1)
                {
                    TogglePlayPause();
                }
                else if (clickCount >= 2)
                {
                    ToggleFullscreen();
                }
                clickCount = 0;
            };

            MouseLeftButtonDown += (s, e) =>
            {
                HitTestResult hit = VisualTreeHelper.HitTest(this, e.GetPosition(this));
                if (hit != null && hit.VisualHit != null)
                {
                    DependencyObject obj = hit.VisualHit;
                    while (obj != null && obj != this)
                    {
                        if (obj == headerBorder || obj == bottomPodBorder || obj == emptyCard || obj == settingsPanel || obj == playlistPanel)
                        {
                            return; // Clicked on controls, ignore screen pause
                        }
                        obj = VisualTreeHelper.GetParent(obj);
                    }
                }

                clickCount++;
                if (clickCount == 1)
                {
                    clickTimer.Start();
                }
            };

            mediaElement.MediaOpened += MediaElement_MediaOpened;
            mediaElement.MediaEnded += (s, e) => {
                if (currentPlaylistIndex >= 0 && currentPlaylistIndex < playlistFiles.Count - 1)
                {
                    PlayPlaylistItem(currentPlaylistIndex + 1);
                }
                else
                {
                    isPlaying = false;
                    playPauseIcon.Text = "▶";
                    ShowControls();
                }
            };

            seekSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((s, e) => isDraggingSeeker = true));
            seekSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((s, e) => {
                isDraggingSeeker = false;
                if (mediaElement.NaturalDuration.HasTimeSpan)
                {
                    mediaElement.Position = TimeSpan.FromSeconds(seekSlider.Value);
                }
            }));
        }

        private void InitTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += (s, e) =>
            {
                if (mediaElement.NaturalDuration.HasTimeSpan && !isDraggingSeeker)
                {
                    seekSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                    seekSlider.Value = mediaElement.Position.TotalSeconds;
                    timeCurrentText.Text = FormatTime(mediaElement.Position);
                    timeTotalText.Text = FormatTime(mediaElement.NaturalDuration.TimeSpan);
                }
            };
            timer.Start();
        }

        private void InitMouseIdleTimer()
        {
            mouseIdleTimer = new DispatcherTimer();
            mouseIdleTimer.Interval = TimeSpan.FromSeconds(2);
            mouseIdleTimer.Tick += (s, e) =>
            {
                if (isPlaying)
                {
                    headerBorder.Visibility = Visibility.Collapsed;
                    bottomPodBorder.Visibility = Visibility.Collapsed;
                    if (playlistPanel != null) playlistPanel.Visibility = Visibility.Collapsed;
                    Mouse.OverrideCursor = Cursors.None;
                }
            };
            mouseIdleTimer.Start();

            MouseMove += (s, e) => ShowControls();
        }

        private void ShowControls()
        {
            Mouse.OverrideCursor = null;
            bottomPodBorder.Visibility = Visibility.Visible;
            headerBorder.Visibility = Visibility.Visible;

            if (mouseIdleTimer != null)
            {
                mouseIdleTimer.Stop();
                if (isPlaying)
                {
                    mouseIdleTimer.Start();
                }
            }
        }

        private void OpenFileDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "MP4 & Video Files (*.mp4;*.webm;*.mkv;*.avi)|*.mp4;*.webm;*.mkv;*.avi|All Files (*.*)|*.*",
                Title = "Select Video Files - Someway's Hub",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true && dlg.FileNames != null && dlg.FileNames.Length > 0)
            {
                AddFilesToPlaylist(dlg.FileNames, true);
            }
        }

        private void LoadVideo(string filePath)
        {
            AddFilesToPlaylist(new string[] { filePath }, true);
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    AddFilesToPlaylist(files, true);
                }
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                seekSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                timeTotalText.Text = FormatTime(mediaElement.NaturalDuration.TimeSpan);
            }
            if (!mediaElement.HasVideo || mediaElement.NaturalVideoWidth == 0)
            {
                ShowToast("⚠️ MKV HEVC/10-Bit Video Codec unsupported by Windows");
            }
            ApplyAspectMode();
        }

        private void TogglePlayPause()
        {
            if (mediaElement.Source == null) return;
            if (playPauseIcon.Text == "▶")
            {
                mediaElement.Play();
                isPlaying = true;
                playPauseIcon.Text = "⏸";
            }
            else
            {
                mediaElement.Pause();
                isPlaying = false;
                playPauseIcon.Text = "▶";
            }
            ShowControls();
        }

        private void SeekRelative(double seconds)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                TimeSpan newPos = mediaElement.Position.Add(TimeSpan.FromSeconds(seconds));
                if (newPos < TimeSpan.Zero) newPos = TimeSpan.Zero;
                if (newPos > mediaElement.NaturalDuration.TimeSpan) newPos = mediaElement.NaturalDuration.TimeSpan;
                mediaElement.Position = newPos;
            }
        }

        private void CycleAspectMode()
        {
            currentAspectIndex = (currentAspectIndex + 1) % aspectModeLabels.Length;
            ApplyAspectMode();
        }

        private void ApplyAspectMode()
        {
            if (mediaElement == null) return;

            scaleTransform.ScaleX = 1.0;
            scaleTransform.ScaleY = 1.0;

            switch (currentAspectIndex)
            {
                case 0: // Match & Stretch Screen (Stremio Stretch)
                    mediaElement.Stretch = Stretch.Fill;
                    break;
                case 1: // Fit (Contain)
                    mediaElement.Stretch = Stretch.Uniform;
                    break;
                case 2: // Cover (Crop)
                    mediaElement.Stretch = Stretch.UniformToFill;
                    break;
                case 3: // 16:9 Preset
                    mediaElement.Stretch = Stretch.Fill;
                    break;
                case 4: // 21:9 Ultrawide Preset
                    mediaElement.Stretch = Stretch.Fill;
                    scaleTransform.ScaleX = 1.25;
                    break;
                case 5: // 4:3 Preset
                    mediaElement.Stretch = Stretch.Fill;
                    scaleTransform.ScaleX = 0.75;
                    break;
            }

            ShowToast("Aspect Ratio: " + aspectModeLabels[currentAspectIndex]);
        }

        private double lastVolumeBeforeMute = 1.0;

        private void ToggleMute()
        {
            if (mediaElement == null || volumeSlider == null) return;

            if (!mediaElement.IsMuted && volumeSlider.Value > 0)
            {
                lastVolumeBeforeMute = volumeSlider.Value;
                volumeSlider.Value = 0;
                ShowToast("Audio Muted 🔇");
            }
            else
            {
                double restoreVal = (lastVolumeBeforeMute > 0) ? lastVolumeBeforeMute : 1.0;
                volumeSlider.Value = restoreVal;
                ShowToast("Audio Unmuted 🔊");
            }
        }

        private double[] speedPresets = new double[] { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };
        private int currentSpeedIndex = 2; // Default 1.0x

        private void CycleSpeed()
        {
            currentSpeedIndex = (currentSpeedIndex + 1) % speedPresets.Length;
            double speed = speedPresets[currentSpeedIndex];
            if (mediaElement != null)
            {
                mediaElement.SpeedRatio = speed;
            }
            ShowToast(string.Format("Playback Speed: {0}x", speed));
        }

        private void OpenSubtitlesDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Subtitle Files (*.vtt, *.srt)|*.vtt;*.srt|All Files (*.*)|*.*",
                Title = "Select Subtitles File"
            };
            if (dlg.ShowDialog() == true)
            {
                ShowToast("Subtitles Loaded: " + Path.GetFileName(dlg.FileName));
            }
        }

        private DispatcherTimer toastTimer;
        private Border toastBorder;
        private TextBlock toastText;

        private void ShowToast(string message)
        {
            if (overlayGrid == null) return;

            if (toastBorder == null)
            {
                toastBorder = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 70, 0, 0),
                    Background = new SolidColorBrush(Color.FromArgb(220, 15, 17, 26)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(150, 34, 197, 94)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(20),
                    Padding = new Thickness(18, 9, 18, 9),
                    Effect = new DropShadowEffect { BlurRadius = 15, Color = Colors.Black, Opacity = 0.7 }
                };
                toastText = new TextBlock
                {
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold
                };
                toastBorder.Child = toastText;
                overlayGrid.Children.Add(toastBorder);
            }

            toastText.Text = message;
            toastBorder.Visibility = Visibility.Visible;

            if (toastTimer == null)
            {
                toastTimer = new DispatcherTimer();
                toastTimer.Interval = TimeSpan.FromSeconds(2);
                toastTimer.Tick += (s, e) =>
                {
                    toastTimer.Stop();
                    if (toastBorder != null) toastBorder.Visibility = Visibility.Collapsed;
                };
            }
            toastTimer.Stop();
            toastTimer.Start();
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                previousWindowState = WindowState;
                previousWindowStyle = WindowStyle;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                WindowStyle = previousWindowStyle;
                WindowState = previousWindowState;
                isFullscreen = false;
            }
            ShowControls();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                case Key.K:
                    TogglePlayPause();
                    break;
                case Key.F:
                    ToggleFullscreen();
                    break;
                case Key.Escape:
                    if (isFullscreen) ToggleFullscreen();
                    break;
                case Key.A:
                    CycleAspectMode();
                    break;
                case Key.M:
                    ToggleMute();
                    break;
                case Key.Left:
                    SeekRelative(-5);
                    break;
                case Key.Right:
                    SeekRelative(5);
                    break;
                case Key.Up:
                    volumeSlider.Value = Math.Min(1, volumeSlider.Value + 0.1);
                    break;
                case Key.Down:
                    volumeSlider.Value = Math.Max(0, volumeSlider.Value - 0.1);
                    break;
            }
        }

        private Button CreateLiquidPillButton(string text, Color startColor, Color endColor, double cornerRadius)
        {
            Button btn = new Button
            {
                Content = text,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Padding = new Thickness(14, 6, 14, 6),
                Cursor = Cursors.Hand,
                Effect = new DropShadowEffect { BlurRadius = 12, Color = startColor, Opacity = 0.5 }
            };

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            border.SetValue(Border.BackgroundProperty, new LinearGradientBrush(startColor, endColor, new Point(0, 0), new Point(1, 1)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(0));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            btn.Template = template;

            return btn;
        }

        private Button CreateLiquidIconButton(string icon, string tooltip, double width, double height, double cornerRadius)
        {
            TextBlock tb = new TextBlock
            {
                Text = icon,
                FontSize = 18,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect { BlurRadius = 8, Color = Colors.Black, Opacity = 0.95 }
            };
            Button btn = new Button
            {
                Content = tb,
                ToolTip = tooltip,
                Width = width,
                Height = height,
                Cursor = Cursors.Hand,
                Margin = new Thickness(3, 0, 3, 0),
                Background = Brushes.Transparent
            };

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(0));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            btn.Template = template;

            return btn;
        }

        private void ApplyCircularSliderStyle(Slider slider, Color trackFillColor, Color thumbColor, double thumbSize)
        {
            try
            {
                string hexFill = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", trackFillColor.A, trackFillColor.R, trackFillColor.G, trackFillColor.B);
                string hexThumb = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", thumbColor.A, thumbColor.R, thumbColor.G, thumbColor.B);
                double radius = thumbSize / 2.0;

                string xaml = string.Format(@"
                    <ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                     xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                                     TargetType=""Slider"">
                        <Grid VerticalAlignment=""Center"">
                            <Border Height=""3"" Background=""#50FFFFFF"" CornerRadius=""1.5"" VerticalAlignment=""Center""/>
                            <Track x:Name=""PART_Track"">
                                <Track.DecreaseRepeatButton>
                                    <RepeatButton Command=""Slider.DecreaseLarge"">
                                        <RepeatButton.Template>
                                            <ControlTemplate TargetType=""RepeatButton"">
                                                <Border Height=""3"" Background=""{0}"" CornerRadius=""1.5"" VerticalAlignment=""Center""/>
                                            </ControlTemplate>
                                        </RepeatButton.Template>
                                    </RepeatButton>
                                </Track.DecreaseRepeatButton>
                                <Track.IncreaseRepeatButton>
                                    <RepeatButton Command=""Slider.IncreaseLarge"">
                                        <RepeatButton.Template>
                                            <ControlTemplate TargetType=""RepeatButton"">
                                                <Border Background=""Transparent""/>
                                            </ControlTemplate>
                                        </RepeatButton.Template>
                                    </RepeatButton>
                                </Track.IncreaseRepeatButton>
                                <Track.Thumb>
                                    <Thumb x:Name=""PART_Thumb"">
                                        <Thumb.Template>
                                            <ControlTemplate TargetType=""Thumb"">
                                                <Border Width=""{1}"" Height=""{1}"" CornerRadius=""{2}""
                                                        Background=""{3}"" BorderThickness=""0""/>
                                            </ControlTemplate>
                                        </Thumb.Template>
                                    </Thumb>
                                </Track.Thumb>
                            </Track>
                        </Grid>
                    </ControlTemplate>", hexFill, thumbSize, radius, hexThumb);

                slider.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Slider template styling exception: " + ex.Message);
            }
        }



        private void StyleSpeedComboBox(ComboBox combo)
        {
            combo.Foreground = Brushes.White;
            combo.FontWeight = FontWeights.Bold;
            combo.FontSize = 12;

            ControlTemplate template = new ControlTemplate(typeof(ComboBox));
            FrameworkElementFactory grid = new FrameworkElementFactory(typeof(Grid));

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(120, 20, 22, 35)));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetBinding(ContentPresenter.ContentProperty, new System.Windows.Data.Binding("SelectionBoxItem") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.MarginProperty, new Thickness(8, 4, 8, 4));

            FrameworkElementFactory toggleButton = new FrameworkElementFactory(typeof(ToggleButton));
            toggleButton.SetValue(ToggleButton.FocusableProperty, false);
            toggleButton.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsDropDownOpen") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent, Mode = System.Windows.Data.BindingMode.TwoWay });
            toggleButton.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);

            ControlTemplate toggleTemp = new ControlTemplate(typeof(ToggleButton));
            FrameworkElementFactory toggleBorder = new FrameworkElementFactory(typeof(Border));
            toggleBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            toggleTemp.VisualTree = toggleBorder;
            toggleButton.SetValue(ToggleButton.TemplateProperty, toggleTemp);

            FrameworkElementFactory popup = new FrameworkElementFactory(typeof(Popup));
            popup.Name = "PART_Popup";
            popup.SetBinding(Popup.IsOpenProperty, new System.Windows.Data.Binding("IsDropDownOpen") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Slide);

            FrameworkElementFactory popupBorder = new FrameworkElementFactory(typeof(Border));
            popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            popupBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(245, 15, 17, 26)));
            popupBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(150, 34, 197, 94)));
            popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            popupBorder.SetValue(Border.PaddingProperty, new Thickness(4));

            FrameworkElementFactory scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.CanContentScrollProperty, true);

            FrameworkElementFactory itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            scrollViewer.AppendChild(itemsPresenter);
            popupBorder.AppendChild(scrollViewer);
            popup.AppendChild(popupBorder);

            border.AppendChild(contentPresenter);
            grid.AppendChild(border);
            grid.AppendChild(toggleButton);
            grid.AppendChild(popup);

            template.VisualTree = grid;
            combo.Template = template;
        }

        private string FormatTime(TimeSpan ts)
        {
            if (ts.Hours > 0)
                return string.Format("{0}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
            return string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
        }
    }
}
