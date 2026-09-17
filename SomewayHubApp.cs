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
        private TextBlock volPercentText;
        private Button volBoostBtn;
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

        private System.Collections.Generic.List<PlaylistTab> playlistTabs = new System.Collections.Generic.List<PlaylistTab>();
        private int activeTabIndex = 0;
        private int playingTabIndex = 0;
        private System.Collections.Generic.HashSet<string> watchedFiles = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Border playlistPanel;
        private StackPanel playlistItemsStack;
        private StackPanel tabBarStack;
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
        private DispatcherTimer seekDebounceTimer;
        private double pendingSeekSeconds = -1;
        private bool isFullscreen = false;
        private bool isPlaying = false;
        private WindowState previousWindowState;
        private WindowStyle previousWindowStyle;
        private double skipIntroDuration = 95; // seconds, editable in settings
        private Button forward135Btn; // reference so tooltip can be updated

        // Hotkey Bindings Dictionary
        private System.Collections.Generic.Dictionary<string, Key> hotkeys;
        private string currentlyRebinding = null; // action name being rebound
        private System.Collections.Generic.Dictionary<string, Button> hotkeyBtns = new System.Collections.Generic.Dictionary<string, Button>();

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
            Title = "Someway's Hub v3.0 - Aspect Ratio Media Player";
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
            StackPanel emptyActionsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            Button emptyOpenBtn = CreateLiquidPillButton("📂  Open MP4 / MKV", Color.FromRgb(255, 120, 0), Color.FromRgb(255, 60, 0), 8);
            emptyOpenBtn.Height = 44;
            emptyOpenBtn.Width = 190;
            emptyOpenBtn.Margin = new Thickness(0, 0, 10, 0);
            emptyOpenBtn.Click += (s, e) => OpenFileDialog();

            Button emptyPlaylistBtn = CreateLiquidPillButton("📋  Playlist", Color.FromRgb(34, 197, 94), Color.FromRgb(16, 160, 70), 8);
            emptyPlaylistBtn.Height = 44;
            emptyPlaylistBtn.Width = 150;
            emptyPlaylistBtn.Click += (s, e) => TogglePlaylistPanel();

            emptyActionsRow.Children.Add(emptyOpenBtn);
            emptyActionsRow.Children.Add(emptyPlaylistBtn);

            emptyStack.Children.Add(emptyTitle);
            emptyStack.Children.Add(emptyDesc);
            emptyStack.Children.Add(emptyActionsRow);
            emptyCard.Child = emptyStack;
            overlayGrid.Children.Add(emptyCard);

            LoadWatchedFromDisk();
            LoadAllPlaylistsFromDisk();

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
            ApplyCircularSliderStyle(seekSlider, new SolidColorBrush(Color.FromRgb(34, 197, 94)), Color.FromRgb(34, 197, 94), 11);
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

            forward135Btn = CreateLiquidIconButton(">>", "Skip Intro (>>)", 34, 34, 17);
            forward135Btn.Margin = new Thickness(0, 0, 14, 0);
            forward135Btn.Click += (s, e) => SeekRelative(skipIntroDuration);

            volBtn = CreateLiquidIconButton("🔊", "Mute / Unmute (M)", 34, 34, 17);
            volBtn.Margin = new Thickness(0, 0, 4, 0);
            volIcon = (TextBlock)volBtn.Content;
            volBtn.Click += (s, e) => ToggleMute();

            volumeSlider = new Slider
            {
                Width = 95,
                Minimum = 0,
                Maximum = 5.0,
                Value = 1.0,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94))
            };

            volPercentText = new TextBlock
            {
                Text = "100%",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 6, 0),
                MinWidth = 36,
                TextAlignment = TextAlignment.Center
            };

            volBoostBtn = CreateLiquidPillButton("⚡ 500%", Color.FromRgb(34, 197, 94), Color.FromRgb(255, 140, 0), 6);
            volBoostBtn.Height = 26;
            volBoostBtn.Margin = new Thickness(2, 0, 0, 0);
            volBoostBtn.ToolTip = "Toggle 500% Max Volume Boost (HotKey: B)";
            volBoostBtn.Click += (s, e) => Toggle200PercentBoost();

            volumeSlider.ValueChanged += (s, e) =>
            {
                UpdateVolumeUI(volumeSlider.Value);
            };
            UpdateVolumeUI(1.0);

            // Enable mouse wheel volume control over left controls
            leftControls.PreviewMouseWheel += (s, e) =>
            {
                e.Handled = true;
                double step = e.Delta > 0 ? 0.05 : -0.05;
                SetVolume(volumeSlider.Value + step);
            };

            leftControls.Children.Add(rewindBtn);
            leftControls.Children.Add(playPauseBtn);
            leftControls.Children.Add(forwardBtn);
            leftControls.Children.Add(forward135Btn);
            leftControls.Children.Add(volBtn);
            leftControls.Children.Add(volumeSlider);
            leftControls.Children.Add(volPercentText);
            leftControls.Children.Add(volBoostBtn);

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

            InitHotkeys();
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
                Width = 360,
                MaxHeight = SystemParameters.PrimaryScreenHeight * 0.5,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 65, 20, 0),
                Background = new SolidColorBrush(Color.FromArgb(245, 14, 17, 28)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 34, 197, 94)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(0),
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect { BlurRadius = 30, Color = Colors.Black, Opacity = 0.85 }
            };

            // Outer DockPanel: pinned header on top, scrollable body below
            DockPanel outerDock = new DockPanel { LastChildFill = true };

            // Pinned header
            Grid headerGrid = new Grid
            {
                Margin = new Thickness(20, 16, 20, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock panelTitle = new TextBlock
            {
                Text = "\u2699 Settings",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(panelTitle, 0);

            Button closeBtn = CreateLiquidIconButton("\u2715", "Close", 28, 28, 14);
            closeBtn.Click += (s, e) => settingsPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(closeBtn, 1);

            headerGrid.Children.Add(panelTitle);
            headerGrid.Children.Add(closeBtn);
            DockPanel.SetDock(headerGrid, Dock.Top);
            outerDock.Children.Add(headerGrid);

            // Thin separator line under header
            Border headerSep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Margin = new Thickness(0, 0, 0, 0)
            };
            DockPanel.SetDock(headerSep, Dock.Top);
            outerDock.Children.Add(headerSep);

            // Scrollable content
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0)
            };
            ApplyThinScrollbarStyle(scrollViewer);

            StackPanel panelStack = new StackPanel { Margin = new Thickness(20, 14, 20, 20) };
            scrollViewer.Content = panelStack;
            outerDock.Children.Add(scrollViewer);
            settingsPanel.Child = outerDock;

            // (No separate headerGrid/panelStack additions below — they are declared above)

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
            ApplyCircularSliderStyle(sliderScaleX, new SolidColorBrush(Color.FromRgb(34, 197, 94)), Color.FromRgb(34, 197, 94), 12);
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
            ApplyCircularSliderStyle(sliderScaleY, new SolidColorBrush(Color.FromRgb(255, 140, 0)), Color.FromRgb(255, 140, 0), 12);
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

            // Separator
            Border sep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 14, 0, 14)
            };
            panelStack.Children.Add(sep);

            // Skip Intro Duration Row
            TextBlock skipLabel = new TextBlock
            {
                Text = "⏭  Skip Intro (>> button) Duration",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            panelStack.Children.Add(skipLabel);

            Grid skipRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            skipRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            skipRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock skipRowLabel = new TextBlock
            {
                Text = "Seconds to skip:",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel skipActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Button btnSkipSub = CreateLiquidIconButton("-", "Decrease by 5s", 22, 22, 11);
            btnSkipSub.Margin = new Thickness(0, 0, 4, 0);

            TextBox txtSkipDuration = CreateDigitInputBox(((int)skipIntroDuration).ToString());
            txtSkipDuration.Width = 50;

            Button btnSkipAdd = CreateLiquidIconButton("+", "Increase by 5s", 22, 22, 11);
            btnSkipAdd.Margin = new Thickness(4, 0, 6, 0);

            Button btnSkipReset = CreateLiquidIconButton("🔄", "Reset to 1:35 (95s)", 22, 22, 11);

            // Wire up skip duration controls
            btnSkipSub.Click += (s, e) =>
            {
                skipIntroDuration = Math.Max(5, skipIntroDuration - 5);
                txtSkipDuration.Text = ((int)skipIntroDuration).ToString();
                UpdateSkipIntroTooltip();
            };
            btnSkipAdd.Click += (s, e) =>
            {
                skipIntroDuration = Math.Min(600, skipIntroDuration + 5);
                txtSkipDuration.Text = ((int)skipIntroDuration).ToString();
                UpdateSkipIntroTooltip();
            };
            btnSkipReset.Click += (s, e) =>
            {
                skipIntroDuration = 95;
                txtSkipDuration.Text = "95";
                UpdateSkipIntroTooltip();
            };
            txtSkipDuration.TextChanged += (s, e) =>
            {
                int val;
                if (int.TryParse(txtSkipDuration.Text, out val) && val >= 1 && val <= 600)
                {
                    skipIntroDuration = val;
                    UpdateSkipIntroTooltip();
                }
            };

            skipActions.Children.Add(btnSkipSub);
            skipActions.Children.Add(txtSkipDuration);
            skipActions.Children.Add(btnSkipAdd);
            skipActions.Children.Add(btnSkipReset);

            Grid.SetColumn(skipRowLabel, 0);
            Grid.SetColumn(skipActions, 1);
            skipRow.Children.Add(skipRowLabel);
            skipRow.Children.Add(skipActions);
            panelStack.Children.Add(skipRow);

            TextBlock skipHint = new TextBlock
            {
                Text = "Default: 95s (1:35). Range: 1–600s.",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 4, 0, 0)
            };
            panelStack.Children.Add(skipHint);

            // ================================================================
            // 200% VOLUME BOOSTER SECTION
            // ================================================================
            Border volBoostSep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 14, 0, 14)
            };
            panelStack.Children.Add(volBoostSep);

            TextBlock volBoostTitle = new TextBlock
            {
                Text = "🚀  500% Audio Volume Booster",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 114, 182)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            panelStack.Children.Add(volBoostTitle);

            TextBlock volBoostHint = new TextBlock
            {
                Text = "Amplify media audio output up to 500% (+14dB gain). Quick select volume level:",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            panelStack.Children.Add(volBoostHint);

            StackPanel presetStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            double[] volPresets = new double[] { 1.0, 1.5, 2.0, 3.0, 5.0 };
            string[] presetLabels = new string[] { "100%", "150%", "200%", "300%", "🔥 500% MAX" };

            for (int pIndex = 0; pIndex < volPresets.Length; pIndex++)
            {
                double pVal = volPresets[pIndex];
                string pLabel = presetLabels[pIndex];
                Color pColor;
                if (pVal <= 1.0)
                {
                    pColor = Color.FromRgb(34, 197, 94);
                }
                else
                {
                    double t = Math.Min(1.0, pVal - 1.0);
                    byte r = (byte)(34 + (255 - 34) * t);
                    byte g = (byte)(197 + (140 - 197) * t);
                    byte b = (byte)(94 + (0 - 94) * t);
                    pColor = Color.FromRgb(r, g, b);
                }

                Button btnPreset = CreateLiquidPillButton(pLabel, pColor, pColor, 6);
                btnPreset.Height = 28;
                btnPreset.Margin = new Thickness(0, 0, 6, 0);
                btnPreset.Click += (s, e) => SetVolume(pVal);
                presetStack.Children.Add(btnPreset);
            }
            panelStack.Children.Add(presetStack);

            // ================================================================
            // KEYBINDS SECTION
            // ================================================================
            Border keySep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 14, 0, 14)
            };
            panelStack.Children.Add(keySep);

            TextBlock keyTitle = new TextBlock
            {
                Text = "⌨  Hotkey Bindings",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            panelStack.Children.Add(keyTitle);

            TextBlock keyHint = new TextBlock
            {
                Text = "Click a key button and press any key to rebind.",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            panelStack.Children.Add(keyHint);

            string[] actionNames = new string[]
            {
                "Play / Pause",
                "Fullscreen",
                "Aspect Ratio",
                "Mute",
                "Seek Back 5s",
                "Seek Forward 5s",
                "Volume Up",
                "Volume Down",
                "Toggle 200% Boost",
                "Rewind 10s",
                "Forward 10s",
                "Skip Intro (>>)"
            };

            foreach (string action in actionNames)
            {
                string act = action;
                Grid row = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock lbl = new TextBlock
                {
                    Text = act,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    VerticalAlignment = VerticalAlignment.Center
                };

                Key currentKey = hotkeys.ContainsKey(act) ? hotkeys[act] : Key.None;
                Button rebindBtn = CreateRebindButton(KeyToLabel(currentKey));
                rebindBtn.Tag = act;
                rebindBtn.Click += (s, e) => StartRebinding(act);
                hotkeyBtns[act] = rebindBtn;

                Button resetBtn2 = CreateLiquidIconButton("\u21ba", "Reset to default", 22, 22, 8);
                resetBtn2.Margin = new Thickness(4, 0, 0, 0);
                resetBtn2.Click += (s, e) =>
                {
                    Key def = GetDefaultKey(act);
                    hotkeys[act] = def;
                    if (hotkeyBtns.ContainsKey(act))
                        UpdateRebindBtnLabel(hotkeyBtns[act], KeyToLabel(def));
                    if (currentlyRebinding == act) CancelRebinding();
                };

                StackPanel rbWrap = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                rbWrap.Children.Add(rebindBtn);
                rbWrap.Children.Add(resetBtn2);

                Grid.SetColumn(lbl, 0);
                Grid.SetColumn(rbWrap, 1);
                row.Children.Add(lbl);
                row.Children.Add(rbWrap);
                panelStack.Children.Add(row);
            }

            Button resetAllBtn = CreateLiquidPillButton("\ud83d\udd04  Reset All Keybinds", Color.FromRgb(100, 116, 139), Color.FromRgb(71, 85, 105), 10);
            resetAllBtn.Height = 30;
            resetAllBtn.Margin = new Thickness(0, 8, 0, 0);
            resetAllBtn.Click += (s, e) => ResetAllHotkeys();
            panelStack.Children.Add(resetAllBtn);

            Panel.SetZIndex(settingsPanel, 200);
            overlayGrid.Children.Add(settingsPanel);
        }

        private void InitPlaylistPanel()
        {
            playlistPanel = new Border
            {
                Width = 370,
                Height = 540,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 65, 20, 0),
                Background = new SolidColorBrush(Color.FromArgb(245, 14, 17, 28)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 34, 197, 94)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(14),
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect { BlurRadius = 30, Color = Colors.Black, Opacity = 0.85 }
            };
            Panel.SetZIndex(playlistPanel, 200);

            Grid plGrid = new Grid();
            plGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // tab bar
            plGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // separator
            plGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header row
            plGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // items

            // ── Tab Bar ────────────────────────────────────────────────────────
            ScrollViewer tabScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 0, 8)
            };
            tabBarStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            tabScroll.Content = tabBarStack;
            Grid.SetRow(tabScroll, 0);
            plGrid.Children.Add(tabScroll);

            // ── Separator ─────────────────────────────────────────────────────
            Border tabSep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(tabSep, 1);
            plGrid.Children.Add(tabSep);

            // ── Header Row ────────────────────────────────────────────────────
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            playlistTitleText = new TextBlock
            {
                Text = "🎬 Playlist 1 (0)",
                FontSize = 14,
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
            Grid.SetRow(headerGrid, 2);
            plGrid.Children.Add(headerGrid);

            // ── Items List ────────────────────────────────────────────────────
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            playlistItemsStack = new StackPanel();
            scroll.Content = playlistItemsStack;
            ApplyThinScrollbarStyle(scroll);
            Grid.SetRow(scroll, 3);
            plGrid.Children.Add(scroll);

            playlistPanel.Child = plGrid;
            overlayGrid.Children.Add(playlistPanel);

            RefreshTabBar();
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
            var tab = GetActiveTab();
            if (tab == null) return;

            bool addedAny = false;
            foreach (string file in files)
            {
                if (string.IsNullOrEmpty(file)) continue;
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext == ".mp4" || ext == ".webm" || ext == ".mkv" || ext == ".avi" || ext == ".mov" || ext == ".m4v" || ext == ".wmv" || ext == ".flv")
                {
                    if (!tab.Files.Contains(file))
                    {
                        tab.Files.Add(file);
                        addedAny = true;
                    }
                }
            }

            if (tab.Files.Count > 0)
            {
                RefreshPlaylistUI();
                SaveAllPlaylistsToDisk();
                if (tab.CurrentIndex == -1 || (playIfFirst && !isPlaying))
                {
                    PlayPlaylistItem(tab.Files.Count - (addedAny ? 1 : 0));
                }
            }
        }

        private void PlayPlaylistItem(int index)
        {
            var tab = GetActiveTab();
            if (tab == null) return;
            if (index < 0 || index >= tab.Files.Count) return;

            // When moving away from a video, mark the previously playing video as watched (red)
            if (tab.CurrentIndex >= 0 && tab.CurrentIndex < tab.Files.Count && tab.CurrentIndex != index)
            {
                string prevFile = tab.Files[tab.CurrentIndex];
                if (!string.IsNullOrEmpty(prevFile))
                {
                    watchedFiles.Add(prevFile);
                    SaveWatchedToDisk();
                }
            }

            tab.CurrentIndex = index;
            playingTabIndex = activeTabIndex;
            string filePath = tab.Files[index];
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
            ShowToast(string.Format("Playing [{0}/{1}]: {2}", index + 1, tab.Files.Count, System.IO.Path.GetFileName(filePath)));
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
                                var pTab = (playingTabIndex >= 0 && playingTabIndex < playlistTabs.Count) ? playlistTabs[playingTabIndex] : null;
                                if (pTab != null && pTab.CurrentIndex >= 0 && pTab.CurrentIndex < pTab.Files.Count && pTab.Files[pTab.CurrentIndex] == mkvFile)
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
            var tab = GetActiveTab();
            if (tab == null) return;
            if (index < 0 || index >= tab.Files.Count) return;
            bool wasPlayingThis = (tab.CurrentIndex == index);
            tab.Files.RemoveAt(index);

            if (tab.Files.Count == 0)
            {
                tab.CurrentIndex = -1;
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
                    int nextIdx = (index < tab.Files.Count) ? index : tab.Files.Count - 1;
                    PlayPlaylistItem(nextIdx);
                }
                else if (tab.CurrentIndex > index)
                {
                    tab.CurrentIndex--;
                }
            }

            RefreshPlaylistUI();
            SaveAllPlaylistsToDisk();
        }

        private void MovePlaylistItem(int fromIndex, int toIndex)
        {
            var tab = GetActiveTab();
            if (tab == null) return;
            if (fromIndex < 0 || fromIndex >= tab.Files.Count) return;
            if (toIndex < 0 || toIndex >= tab.Files.Count) return;
            if (fromIndex == toIndex) return;

            string item = tab.Files[fromIndex];
            tab.Files.RemoveAt(fromIndex);
            tab.Files.Insert(toIndex, item);

            if (tab.CurrentIndex == fromIndex)
            {
                tab.CurrentIndex = toIndex;
            }
            else if (tab.CurrentIndex > fromIndex && tab.CurrentIndex <= toIndex)
            {
                tab.CurrentIndex--;
            }
            else if (tab.CurrentIndex < fromIndex && tab.CurrentIndex >= toIndex)
            {
                tab.CurrentIndex++;
            }

            RefreshPlaylistUI();
            SaveAllPlaylistsToDisk();
            ShowToast(string.Format("Reordered: {0}", System.IO.Path.GetFileName(item)));
        }

        private void SaveAllPlaylistsToDisk()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = System.IO.Path.Combine(appData, "SomewayHub");
                Directory.CreateDirectory(folder);
                string path = System.IO.Path.Combine(folder, "playlists_v2.txt");
                var lines = new System.Collections.Generic.List<string>();
                lines.Add("ACTIVE:" + activeTabIndex.ToString());
                foreach (var t in playlistTabs)
                {
                    lines.Add("TAB:" + t.Name + "|" + t.CurrentIndex.ToString());
                    foreach (string f in t.Files)
                        lines.Add(f);
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        private void SaveWatchedToDisk()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = System.IO.Path.Combine(appData, "SomewayHub");
                Directory.CreateDirectory(folder);
                string path = System.IO.Path.Combine(folder, "watched.txt");
                File.WriteAllLines(path, watchedFiles);
            }
            catch { }
        }

        private void LoadWatchedFromDisk()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = System.IO.Path.Combine(appData, "SomewayHub", "watched.txt");
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    watchedFiles.Clear();
                    foreach (string f in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(f))
                            watchedFiles.Add(f);
                    }
                }
            }
            catch { }
        }

        private void LoadAllPlaylistsFromDisk()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // Try new multi-playlist format first
                string newPath = System.IO.Path.Combine(appData, "SomewayHub", "playlists_v2.txt");
                if (File.Exists(newPath))
                {
                    string[] lines = File.ReadAllLines(newPath);
                    playlistTabs.Clear();
                    PlaylistTab curTab = null;
                    activeTabIndex = 0;

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("ACTIVE:"))
                        {
                            int.TryParse(line.Substring(7), out activeTabIndex);
                        }
                        else if (line.StartsWith("TAB:"))
                        {
                            string rest = line.Substring(4);
                            string[] parts = rest.Split('|');
                            string tabName = parts[0];
                            int tabCurIdx = -1;
                            if (parts.Length > 1) int.TryParse(parts[1], out tabCurIdx);
                            curTab = new PlaylistTab(tabName);
                            curTab.CurrentIndex = tabCurIdx;
                            playlistTabs.Add(curTab);
                        }
                        else if (!string.IsNullOrWhiteSpace(line) && curTab != null)
                        {
                            if (File.Exists(line) && !curTab.Files.Contains(line))
                                curTab.Files.Add(line);
                        }
                    }

                    if (activeTabIndex < 0 || activeTabIndex >= playlistTabs.Count) activeTabIndex = 0;
                }
                else
                {
                    // Migrate from old single-playlist format
                    string oldPath = System.IO.Path.Combine(appData, "SomewayHub", "playlist.txt");
                    if (File.Exists(oldPath))
                    {
                        PlaylistTab migrated = new PlaylistTab("Playlist 1");
                        foreach (string f in File.ReadAllLines(oldPath))
                        {
                            if (!string.IsNullOrWhiteSpace(f) && File.Exists(f) && !migrated.Files.Contains(f))
                                migrated.Files.Add(f);
                        }
                        playlistTabs.Add(migrated);
                    }
                }

                // Always ensure at least one tab exists
                if (playlistTabs.Count == 0)
                    playlistTabs.Add(new PlaylistTab("Playlist 1"));

                activeTabIndex = Math.Max(0, Math.Min(activeTabIndex, playlistTabs.Count - 1));
                playingTabIndex = activeTabIndex;
                RefreshTabBar();
                RefreshPlaylistUI();
            }
            catch
            {
                if (playlistTabs.Count == 0)
                    playlistTabs.Add(new PlaylistTab("Playlist 1"));
                RefreshTabBar();
                RefreshPlaylistUI();
            }
        }

        private void RefreshPlaylistUI()
        {
            var tab = GetActiveTab();

            if (playlistTitleText != null)
            {
                string tabName = tab != null ? tab.Name : "Playlist 1";
                int count = tab != null ? tab.Files.Count : 0;
                playlistTitleText.Text = string.Format("🎬 {0} ({1})", tabName, count);
            }
            if (playlistItemsStack == null) return;

            playlistItemsStack.Children.Clear();

            if (tab == null || tab.Files.Count == 0)
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

            for (int i = 0; i < tab.Files.Count; i++)
            {
                int itemIndex = i;
                string fullPath = tab.Files[i];
                string nameOnly = System.IO.Path.GetFileName(fullPath);
                bool isCurrent = (i == tab.CurrentIndex);
                bool isWatched = watchedFiles.Contains(fullPath);

                Color statusColor;
                string statusTooltip;

                if (isCurrent)
                {
                    statusColor = Color.FromRgb(234, 179, 8);
                    statusTooltip = "Currently Watching";
                }
                else if (isWatched)
                {
                    statusColor = Color.FromRgb(239, 68, 68);
                    statusTooltip = "Watched";
                }
                else
                {
                    statusColor = Color.FromRgb(34, 197, 94);
                    statusTooltip = "Unwatched";
                }

                Border itemCard = new Border
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(8, 6, 8, 6),
                    CornerRadius = new CornerRadius(10),
                    Background = isCurrent
                        ? new SolidColorBrush(Color.FromArgb(110, 234, 179, 8))
                        : new SolidColorBrush(Color.FromArgb(90, 20, 24, 38)),
                    BorderBrush = isCurrent
                        ? new SolidColorBrush(Color.FromRgb(234, 179, 8))
                        : (isWatched ? new SolidColorBrush(Color.FromArgb(80, 239, 68, 68)) : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255))),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    AllowDrop = true,
                    Tag = itemIndex
                };

                Grid itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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

                System.Windows.Shapes.Ellipse watchedDot = new System.Windows.Shapes.Ellipse
                {
                    Width = 9,
                    Height = 9,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 7, 0),
                    Fill = new SolidColorBrush(statusColor),
                    ToolTip = statusTooltip,
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 6,
                        Color = statusColor,
                        Opacity = 0.85
                    }
                };
                Grid.SetColumn(watchedDot, 1);

                TextBlock playIcon = new TextBlock
                {
                    Text = isCurrent ? "▶" : string.Format("{0}.", i + 1),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = isCurrent ? new SolidColorBrush(Color.FromRgb(234, 179, 8)) : (isWatched ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) : new SolidColorBrush(Color.FromRgb(148, 163, 184))),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(playIcon, 2);

                TextBlock titleTb = new TextBlock
                {
                    Text = nameOnly,
                    FontSize = 12,
                    FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isCurrent ? new SolidColorBrush(Color.FromRgb(254, 240, 138)) : Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = fullPath
                };
                Grid.SetColumn(titleTb, 3);

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

                if (i < tab.Files.Count - 1)
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

                Grid.SetColumn(actionStack, 4);

                itemGrid.Children.Add(dragHandle);
                itemGrid.Children.Add(watchedDot);
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

        // ================================================================
        // MULTI-PLAYLIST TAB MANAGEMENT
        // ================================================================

        private PlaylistTab GetActiveTab()
        {
            if (playlistTabs == null || playlistTabs.Count == 0) return null;
            if (activeTabIndex < 0 || activeTabIndex >= playlistTabs.Count) activeTabIndex = 0;
            return playlistTabs[activeTabIndex];
        }

        private void RefreshTabBar()
        {
            if (tabBarStack == null) return;
            tabBarStack.Children.Clear();

            for (int i = 0; i < playlistTabs.Count; i++)
            {
                int tabIdx = i;
                bool isActive = (i == activeTabIndex);
                PlaylistTab pTab = playlistTabs[i];

                Border tabChip = new Border
                {
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(10, 5, 6, 5),
                    Margin = new Thickness(0, 0, 4, 0),
                    Background = isActive
                        ? new LinearGradientBrush(Color.FromArgb(210, 34, 197, 94), Color.FromArgb(210, 255, 140, 0), new Point(0, 0), new Point(1, 1))
                        : new SolidColorBrush(Color.FromArgb(80, 40, 48, 70)),
                    BorderBrush = isActive
                        ? new SolidColorBrush(Color.FromArgb(200, 34, 197, 94))
                        : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Effect = isActive ? new DropShadowEffect { BlurRadius = 10, Color = Color.FromRgb(34, 197, 94), Opacity = 0.65 } : null
                };

                StackPanel chipContent = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                TextBlock nameBlock = new TextBlock
                {
                    Text = pTab.Name,
                    FontSize = 12,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    VerticalAlignment = VerticalAlignment.Center,
                    MaxWidth = 90,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                chipContent.Children.Add(nameBlock);

                if (playlistTabs.Count > 1)
                {
                    Button closeTabBtn = CreateLiquidIconButton("×", "Close Tab", 16, 16, 8);
                    ((TextBlock)closeTabBtn.Content).FontSize = 12;
                    closeTabBtn.Margin = new Thickness(4, 0, 0, 0);
                    closeTabBtn.Click += (s, e) =>
                    {
                        e.Handled = true;
                        CloseTab(tabIdx);
                    };
                    chipContent.Children.Add(closeTabBtn);
                }

                tabChip.Child = chipContent;

                tabChip.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    if (e.ClickCount >= 2)
                        StartTabRename(tabIdx);
                    else
                        SwitchToTab(tabIdx);
                };

                // Right-click context menu
                ContextMenu tabCtxMenu = new ContextMenu
                {
                    Background = new SolidColorBrush(Color.FromArgb(248, 14, 17, 28)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(160, 34, 197, 94)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(0, 4, 0, 4),
                    Effect = new DropShadowEffect { BlurRadius = 20, Color = Colors.Black, Opacity = 0.8 }
                };

                MenuItem renameMenuItem = new MenuItem
                {
                    Header = "✏   Rename Playlist",
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(14, 7, 20, 7)
                };
                renameMenuItem.Click += (s, e) => StartTabRename(tabIdx);
                tabCtxMenu.Items.Add(renameMenuItem);

                if (playlistTabs.Count > 1)
                {
                    Separator sep = new Separator
                    {
                        Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                        Margin = new Thickness(8, 2, 8, 2)
                    };
                    tabCtxMenu.Items.Add(sep);

                    MenuItem closeMenuItem = new MenuItem
                    {
                        Header = "✕   Close Tab",
                        Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        Background = Brushes.Transparent,
                        FontSize = 12,
                        Padding = new Thickness(14, 7, 20, 7)
                    };
                    closeMenuItem.Click += (s, e) => CloseTab(tabIdx);
                    tabCtxMenu.Items.Add(closeMenuItem);
                }

                tabChip.ContextMenu = tabCtxMenu;

                tabBarStack.Children.Add(tabChip);
            }

            // "+" New Playlist button
            Button addTabBtn = CreateLiquidIconButton("＋", "New Playlist Tab", 26, 26, 8);
            addTabBtn.Margin = new Thickness(4, 0, 0, 0);
            addTabBtn.Click += (s, e) => AddNewTab();
            tabBarStack.Children.Add(addTabBtn);
        }

        private void SwitchToTab(int index)
        {
            if (index < 0 || index >= playlistTabs.Count) return;
            activeTabIndex = index;
            RefreshTabBar();
            RefreshPlaylistUI();
            SaveAllPlaylistsToDisk();
            var switchedTab = GetActiveTab();
            if (switchedTab != null) ShowToast("📋 " + switchedTab.Name);
        }

        private void AddNewTab()
        {
            int newNum = playlistTabs.Count + 1;
            string newName = "Playlist " + newNum;
            playlistTabs.Add(new PlaylistTab(newName));
            activeTabIndex = playlistTabs.Count - 1;
            RefreshTabBar();
            RefreshPlaylistUI();
            SaveAllPlaylistsToDisk();
            ShowToast("✨ Created: " + newName);
        }

        private void CloseTab(int index)
        {
            if (playlistTabs.Count <= 1) return;
            string closedName = playlistTabs[index].Name;
            playlistTabs.RemoveAt(index);
            if (activeTabIndex >= playlistTabs.Count)
                activeTabIndex = playlistTabs.Count - 1;
            else if (activeTabIndex > index)
                activeTabIndex--;
            if (playingTabIndex >= playlistTabs.Count)
                playingTabIndex = activeTabIndex;
            else if (playingTabIndex > index)
                playingTabIndex--;
            RefreshTabBar();
            RefreshPlaylistUI();
            SaveAllPlaylistsToDisk();
            ShowToast("🗑 Closed: " + closedName);
        }

        private void StartTabRename(int tabIdx)
        {
            if (tabIdx < 0 || tabIdx >= playlistTabs.Count) return;
            if (tabBarStack == null || tabIdx >= tabBarStack.Children.Count) return;

            Border chip = tabBarStack.Children[tabIdx] as Border;
            if (chip == null) return;
            StackPanel chipContent = chip.Child as StackPanel;
            if (chipContent == null || chipContent.Children.Count == 0) return;
            TextBlock nameBlock = chipContent.Children[0] as TextBlock;
            if (nameBlock == null) return;

            PlaylistTab tabToRename = playlistTabs[tabIdx];

            TextBox renameBox = new TextBox
            {
                Text = tabToRename.Name,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(140, 14, 17, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                BorderThickness = new Thickness(1),
                Width = 85,
                MaxLength = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(3, 1, 3, 1),
                CaretBrush = Brushes.White
            };

            chipContent.Children.RemoveAt(0);
            chipContent.Children.Insert(0, renameBox);
            renameBox.Focus();
            renameBox.SelectAll();

            bool committed = false;
            Action commit = () =>
            {
                if (committed) return;
                committed = true;
                string newName = renameBox.Text.Trim();
                if (!string.IsNullOrEmpty(newName))
                    tabToRename.Name = newName;
                SaveAllPlaylistsToDisk();
                RefreshTabBar();
                RefreshPlaylistUI();
            };

            renameBox.LostFocus += (s, e) => commit();
            renameBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Return || e.Key == Key.Escape) { e.Handled = true; commit(); }
            };
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
                PlaylistTab playingTab = (playingTabIndex >= 0 && playingTabIndex < playlistTabs.Count)
                    ? playlistTabs[playingTabIndex] : GetActiveTab();

                if (playingTab != null && playingTab.CurrentIndex >= 0 && playingTab.CurrentIndex < playingTab.Files.Count)
                {
                    string finishedFile = playingTab.Files[playingTab.CurrentIndex];
                    if (!string.IsNullOrEmpty(finishedFile))
                    {
                        watchedFiles.Add(finishedFile);
                        SaveWatchedToDisk();
                    }
                }

                if (playingTab != null && playingTab.CurrentIndex >= 0 && playingTab.CurrentIndex < playingTab.Files.Count - 1)
                {
                    if (playingTabIndex != activeTabIndex)
                    {
                        activeTabIndex = playingTabIndex;
                        RefreshTabBar();
                    }
                    PlayPlaylistItem(playingTab.CurrentIndex + 1);
                }
                else
                {
                    isPlaying = false;
                    playPauseIcon.Text = "▶";
                    ShowControls();
                    RefreshPlaylistUI();
                }
            };

            seekSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((s, e) => isDraggingSeeker = true));
            seekSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((s, e) =>
            {
                isDraggingSeeker = false;
                if (mediaElement.NaturalDuration.HasTimeSpan)
                {
                    CommitSeek(seekSlider.Value);
                }
            }));

            // Click-to-seek: jump instantly to wherever the user clicks on the bar
            seekSlider.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!mediaElement.NaturalDuration.HasTimeSpan) return;
                double ratio = e.GetPosition(seekSlider).X / seekSlider.ActualWidth;
                ratio = Math.Max(0, Math.Min(1, ratio));
                double targetSeconds = ratio * seekSlider.Maximum;
                seekSlider.Value = targetSeconds;
                isDraggingSeeker = true;
                QueueSeek(targetSeconds);  // debounced — won't flood decoder
                e.Handled = false;
            };

            seekSlider.PreviewMouseLeftButtonUp += (s, e) =>
            {
                isDraggingSeeker = false;
                // Flush whatever was queued when user releases
                if (pendingSeekSeconds >= 0)
                {
                    seekDebounceTimer?.Stop();
                    CommitSeek(pendingSeekSeconds);
                    pendingSeekSeconds = -1;
                }
            };

            // Real-time scrub while dragging — debounced, not every pixel
            seekSlider.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && mediaElement.NaturalDuration.HasTimeSpan)
                {
                    double ratio = e.GetPosition(seekSlider).X / seekSlider.ActualWidth;
                    ratio = Math.Max(0, Math.Min(1, ratio));
                    double targetSeconds = ratio * seekSlider.Maximum;
                    seekSlider.Value = targetSeconds;  // update UI immediately
                    QueueSeek(targetSeconds);          // commit to decoder after 150ms idle
                }
            };
        }

        private void InitTimer()
        {
            // Seek debounce timer: waits 150ms of idle before flushing seek to decoder
            seekDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            seekDebounceTimer.Tick += (s, e) =>
            {
                seekDebounceTimer.Stop();
                if (pendingSeekSeconds >= 0)
                {
                    CommitSeek(pendingSeekSeconds);
                    pendingSeekSeconds = -1;
                }
            };
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

        // Queues a seek to be committed after a short idle — prevents flooding the decoder
        // during fast slider drags.
        private void QueueSeek(double seconds)
        {
            pendingSeekSeconds = seconds;
            seekDebounceTimer?.Stop();
            seekDebounceTimer?.Start();
        }

        // The actual seek flush: Pause → Position → Play.
        // This forces the audio/video decoder to fully resync at the new timestamp,
        // eliminating the ghost-audio lag that occurs when only setting .Position.
        private void CommitSeek(double seconds)
        {
            if (!mediaElement.NaturalDuration.HasTimeSpan) return;
            TimeSpan ts = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, mediaElement.NaturalDuration.TimeSpan.TotalSeconds));
            bool wasPlaying = isPlaying;
            mediaElement.Pause();
            mediaElement.Position = ts;
            if (wasPlaying) mediaElement.Play();
            seekSlider.Value = ts.TotalSeconds;
            timeCurrentText.Text = FormatTime(ts);
        }

        private void SeekRelative(double seconds)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                double newSecs = Math.Clamp(
                    mediaElement.Position.TotalSeconds + seconds,
                    0,
                    mediaElement.NaturalDuration.TimeSpan.TotalSeconds);
                CommitSeek(newSecs);
            }
        }

        private void UpdateSkipIntroTooltip()
        {
            if (forward135Btn == null) return;
            int secs = (int)skipIntroDuration;
            int m = secs / 60;
            int s = secs % 60;
            string label = m > 0 ? string.Format("Skip Intro: {0}:{1:D2} ({2}s) (>>)", m, s, secs)
                                 : string.Format("Skip Intro: {0}s (>>)", secs);
            forward135Btn.ToolTip = label;
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

        private void UpdateVolumeUI(double val)
        {
            if (mediaElement == null || volumeSlider == null) return;

            // WPF MediaElement.Volume is capped at 1.0 — push real gain through
            // Windows CoreAudio SimpleAudioVolume on this process instead.
            mediaElement.Volume = Math.Min(val, 1.0);
            if (val > 1.0)
                SetProcessAudioGain((float)(val / 5.0f)); // map 1–5 → 0.2–1.0 of process gain
            else
                SetProcessAudioGain((float)(val * 0.2f)); // map 0–1 → 0–0.2 of process gain (normal)

            int percent = (int)Math.Round(val * 100);

            if (val == 0)
            {
                mediaElement.IsMuted = true;
                if (volIcon != null) volIcon.Text = "🔇";
            }
            else
            {
                mediaElement.IsMuted = false;
                if (volIcon != null)
                {
                    if (val <= 0.5) volIcon.Text = "🔈";
                    else if (val <= 1.0) volIcon.Text = "🔊";
                    else volIcon.Text = "🚀";
                }
            }

            Color greenColor = Color.FromRgb(34, 197, 94);
            Color activeThumbColor;
            Brush trackBrush;

            if (val <= 1.0)
            {
                activeThumbColor = greenColor;
                trackBrush = new SolidColorBrush(greenColor);
            }
            else
            {
                double t = Math.Min(1.0, (val - 1.0) / 4.0); // 1.0–5.0 → 0–1
                byte r = (byte)(34 + (255 - 34) * t);
                byte g = (byte)(197 + (140 - 197) * t);
                byte b = (byte)(94 + (0 - 94) * t);
                activeThumbColor = Color.FromRgb(r, g, b);

                double greenStop = Math.Max(0.01, 1.0 / val);
                LinearGradientBrush lgb = new LinearGradientBrush();
                lgb.StartPoint = new Point(0, 0);
                lgb.EndPoint = new Point(1, 0);
                lgb.GradientStops.Add(new GradientStop(greenColor, 0.0));
                lgb.GradientStops.Add(new GradientStop(greenColor, greenStop));
                lgb.GradientStops.Add(new GradientStop(activeThumbColor, 1.0));
                trackBrush = lgb;
            }

            if (volPercentText != null)
            {
                volPercentText.Text = $"{percent}%";
                volPercentText.Foreground = new SolidColorBrush(activeThumbColor);
            }

            if (volBoostBtn != null)
            {
                if (val > 1.0)
                {
                    volBoostBtn.Background = new SolidColorBrush(Color.FromArgb(220, activeThumbColor.R, activeThumbColor.G, activeThumbColor.B));
                    volBoostBtn.BorderBrush = new SolidColorBrush(activeThumbColor);
                    volBoostBtn.Effect = new DropShadowEffect { BlurRadius = 12, Color = activeThumbColor, Opacity = 0.85 };
                    volBoostBtn.Content = "🔥 BOOST";
                }
                else
                {
                    volBoostBtn.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                    volBoostBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    volBoostBtn.Effect = null;
                    volBoostBtn.Content = "⚡ 200%";
                }
            }

            ApplyCircularSliderStyle(volumeSlider, trackBrush, activeThumbColor, val > 1.0 ? 11 : 10);
        }

        private void SetVolume(double val)
        {
            if (volumeSlider == null) return;
            val = Math.Clamp(Math.Round(val * 20.0) / 20.0, 0.0, 5.0);
            volumeSlider.Value = val;

            int percent = (int)Math.Round(val * 100);
            if (val > 1.0)
            {
                ShowToast(val == 5.0 ? "🔥 500% MAX BOOST ENABLED" : $"🚀 Volume: {percent}% BOOSTED");
            }
            else
            {
                ShowToast($"🔊 Volume: {percent}%");
            }
        }

        private void Toggle200PercentBoost()
        {
            if (volumeSlider == null) return;
            if (volumeSlider.Value < 5.0)
            {
                SetVolume(5.0);
            }
            else
            {
                SetVolume(1.0);
            }
        }

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
            // If waiting for a rebind, capture the pressed key
            if (currentlyRebinding != null)
            {
                if (e.Key == Key.Escape)
                {
                    CancelRebinding();
                }
                else
                {
                    hotkeys[currentlyRebinding] = e.Key;
                    if (hotkeyBtns.ContainsKey(currentlyRebinding))
                        UpdateRebindBtnLabel(hotkeyBtns[currentlyRebinding], KeyToLabel(e.Key));
                    CancelRebinding();
                }
                e.Handled = true;
                return;
            }

            Key k = e.Key;
            if (k == GetHotkey("Play / Pause"))                { TogglePlayPause(); }
            else if (k == GetHotkey("Fullscreen"))             { ToggleFullscreen(); }
            else if (k == Key.Escape && isFullscreen)          { ToggleFullscreen(); }
            else if (k == GetHotkey("Aspect Ratio"))           { CycleAspectMode(); }
            else if (k == GetHotkey("Mute"))                   { ToggleMute(); }
            else if (k == GetHotkey("Seek Back 5s"))           { SeekRelative(-5); }
            else if (k == GetHotkey("Seek Forward 5s"))        { SeekRelative(5); }
            else if (k == GetHotkey("Volume Up"))              { SetVolume(volumeSlider.Value + 0.05); }
            else if (k == GetHotkey("Volume Down"))            { SetVolume(volumeSlider.Value - 0.05); }
            else if (k == GetHotkey("Toggle 200% Boost"))      { Toggle200PercentBoost(); }
            else if (k == GetHotkey("Rewind 10s"))             { SeekRelative(-10); }
            else if (k == GetHotkey("Forward 10s"))            { SeekRelative(10); }
            else if (k == GetHotkey("Skip Intro (>>)"))        { SeekRelative(skipIntroDuration); }
        }

        private Key GetHotkey(string action)
        {
            Key k;
            return (hotkeys != null && hotkeys.TryGetValue(action, out k)) ? k : Key.None;
        }

        private void InitHotkeys()
        {
            hotkeys = new System.Collections.Generic.Dictionary<string, Key>
            {
                { "Play / Pause",      Key.Space },
                { "Fullscreen",        Key.F },
                { "Aspect Ratio",      Key.A },
                { "Mute",              Key.M },
                { "Seek Back 5s",      Key.Left },
                { "Seek Forward 5s",   Key.Right },
                { "Volume Up",         Key.Up },
                { "Volume Down",       Key.Down },
                { "Toggle 200% Boost", Key.B },
                { "Rewind 10s",        Key.OemComma },
                { "Forward 10s",       Key.OemPeriod },
                { "Skip Intro (>>)",   Key.OemCloseBrackets }
            };
        }

        private Key GetDefaultKey(string action)
        {
            switch (action)
            {
                case "Play / Pause":    return Key.Space;
                case "Fullscreen":      return Key.F;
                case "Aspect Ratio":    return Key.A;
                case "Mute":            return Key.M;
                case "Seek Back 5s":    return Key.Left;
                case "Seek Forward 5s": return Key.Right;
                case "Volume Up":       return Key.Up;
                case "Volume Down":     return Key.Down;
                case "Toggle 200% Boost": return Key.B;
                case "Rewind 10s":      return Key.OemComma;
                case "Forward 10s":     return Key.OemPeriod;
                case "Skip Intro (>>)": return Key.OemCloseBrackets;
                default:                return Key.None;
            }
        }

        private void ResetAllHotkeys()
        {
            InitHotkeys();
            foreach (var pair in hotkeyBtns)
            {
                Key k = GetDefaultKey(pair.Key);
                UpdateRebindBtnLabel(pair.Value, KeyToLabel(k));
            }
            CancelRebinding();
        }

        private void StartRebinding(string action)
        {
            // Cancel any previous
            if (currentlyRebinding != null) CancelRebinding();
            currentlyRebinding = action;
            if (hotkeyBtns.ContainsKey(action))
            {
                Button btn = hotkeyBtns[action];
                UpdateRebindBtnLabel(btn, "[ Press key... ]");
                btn.Background = new SolidColorBrush(Color.FromArgb(180, 34, 197, 94));
            }
        }

        private void CancelRebinding()
        {
            if (currentlyRebinding != null && hotkeyBtns.ContainsKey(currentlyRebinding))
            {
                Button btn = hotkeyBtns[currentlyRebinding];
                Key k = hotkeys.ContainsKey(currentlyRebinding) ? hotkeys[currentlyRebinding] : Key.None;
                UpdateRebindBtnLabel(btn, KeyToLabel(k));
                btn.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            }
            currentlyRebinding = null;
        }

        private Button CreateRebindButton(string label)
        {
            Button btn = new Button
            {
                Content = label,
                Width = 110,
                Height = 26,
                Foreground = Brushes.White,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Courier New"),
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Cursor = Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            FrameworkElementFactory bdr = new FrameworkElementFactory(typeof(Border));
            bdr.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            bdr.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            bdr.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)));
            bdr.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bdr.AppendChild(cp);

            ControlTemplate tmpl = new ControlTemplate(typeof(Button));
            tmpl.VisualTree = bdr;
            btn.Template = tmpl;

            return btn;
        }

        private void UpdateRebindBtnLabel(Button btn, string label)
        {
            btn.Content = label;
        }

        private string KeyToLabel(Key k)
        {
            switch (k)
            {
                case Key.Space:           return "Space";
                case Key.Left:            return "\u2190";
                case Key.Right:           return "\u2192";
                case Key.Up:              return "\u2191";
                case Key.Down:            return "\u2193";
                case Key.Return:          return "Enter";
                case Key.Tab:             return "Tab";
                case Key.Back:            return "Backspace";
                case Key.Delete:          return "Delete";
                case Key.Escape:          return "Esc";
                case Key.OemComma:        return ",";
                case Key.OemPeriod:       return ".";
                case Key.OemCloseBrackets:return "]";
                case Key.OemOpenBrackets: return "[";
                case Key.OemSemicolon:    return ";";
                case Key.OemQuotes:       return "'";
                case Key.OemMinus:        return "-";
                case Key.OemPlus:         return "=";
                case Key.OemQuestion:     return "/";
                case Key.None:            return "(none)";
                default:                  return k.ToString();
            }
        }

        private void ApplyThinScrollbarStyle(ScrollViewer sv)
        {
            // XamlReader is the correct way to build templates with Track (which doesn't implement IAddChild)
            string xaml =
                "<ResourceDictionary" +
                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" +
                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
                "  <Style TargetType=\"{x:Type ScrollBar}\">" +
                "    <Setter Property=\"Width\" Value=\"6\"/>" +
                "    <Setter Property=\"MinWidth\" Value=\"6\"/>" +
                "    <Setter Property=\"Background\" Value=\"Transparent\"/>" +
                "    <Setter Property=\"Template\">" +
                "      <Setter.Value>" +
                "        <ControlTemplate TargetType=\"{x:Type ScrollBar}\">" +
                "          <Grid>" +
                "            <Border CornerRadius=\"3\" Background=\"#18FFFFFF\" Margin=\"1,2\"/>" +
                "            <Track Name=\"PART_Track\" IsDirectionReversed=\"True\">" +
                "              <Track.DecreaseRepeatButton>" +
                "                <RepeatButton Command=\"ScrollBar.PageUpCommand\" Opacity=\"0\" IsTabStop=\"False\" Focusable=\"False\"/>" +
                "              </Track.DecreaseRepeatButton>" +
                "              <Track.IncreaseRepeatButton>" +
                "                <RepeatButton Command=\"ScrollBar.PageDownCommand\" Opacity=\"0\" IsTabStop=\"False\" Focusable=\"False\"/>" +
                "              </Track.IncreaseRepeatButton>" +
                "              <Track.Thumb>" +
                "                <Thumb MinHeight=\"20\">" +
                "                  <Thumb.Template>" +
                "                    <ControlTemplate TargetType=\"{x:Type Thumb}\">" +
                "                      <Border CornerRadius=\"3\" Margin=\"0,2\">" +
                "                        <Border.Background>" +
                "                          <LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"0,1\">" +
                "                            <GradientStop Color=\"#FF7800\" Offset=\"0\"/>" +
                "                            <GradientStop Color=\"#22C55E\" Offset=\"1\"/>" +
                "                          </LinearGradientBrush>" +
                "                        </Border.Background>" +
                "                      </Border>" +
                "                    </ControlTemplate>" +
                "                  </Thumb.Template>" +
                "                </Thumb>" +
                "              </Track.Thumb>" +
                "            </Track>" +
                "          </Grid>" +
                "        </ControlTemplate>" +
                "      </Setter.Value>" +
                "    </Setter>" +
                "  </Style>" +
                "</ResourceDictionary>";

            var rd = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(xaml);
            sv.Resources.MergedDictionaries.Add(rd);
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

        private void ApplyCircularSliderStyle(Slider slider, Brush trackFillBrush, Color thumbColor, double thumbSize)
        {
            try
            {
                string hexThumb = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", thumbColor.A, thumbColor.R, thumbColor.G, thumbColor.B);
                double radius = thumbSize / 2.0;

                string brushXaml;
                if (trackFillBrush is SolidColorBrush sb)
                {
                    brushXaml = string.Format("<SolidColorBrush Color=\"#{0:X2}{1:X2}{2:X2}{3:X2}\"/>", sb.Color.A, sb.Color.R, sb.Color.G, sb.Color.B);
                }
                else if (trackFillBrush is LinearGradientBrush lgb)
                {
                    System.Text.StringBuilder sbXaml = new System.Text.StringBuilder("<LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"1,0\">");
                    foreach (var gs in lgb.GradientStops)
                    {
                        sbXaml.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "<GradientStop Color=\"#{0:X2}{1:X2}{2:X2}{3:X2}\" Offset=\"{4:F3}\"/>", gs.Color.A, gs.Color.R, gs.Color.G, gs.Color.B, gs.Offset);
                    }
                    sbXaml.Append("</LinearGradientBrush>");
                    brushXaml = sbXaml.ToString();
                }
                else
                {
                    brushXaml = "<SolidColorBrush Color=\"#22C55E\"/>";
                }

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
                                                <Border Height=""3"" CornerRadius=""1.5"" VerticalAlignment=""Center"">
                                                    <Border.Background>
                                                        {0}
                                                    </Border.Background>
                                                </Border>
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
                    </ControlTemplate>", brushXaml, thumbSize, radius, hexThumb);

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

        // ================================================================
        // WINDOWS CORE AUDIO — PROCESS-LEVEL GAIN (real volume amplification)
        // ================================================================
        // Maps our 0–5x slider into a Windows per-app SimpleAudioVolume scalar
        // so audio actually goes louder than WPF's hard-capped Volume=1.0.
        private static void SetProcessAudioGain(float scalar)
        {
            try
            {
                scalar = Math.Clamp(scalar, 0f, 1f);
                // Activate the device enumerator
                var enumType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"))!;
                object enumObj = Activator.CreateInstance(enumType)!;
                var enumerator = (IMMDeviceEnumerator)enumObj;
                IMMDevice device;
                enumerator.GetDefaultAudioEndpoint(0 /*eRender*/, 1 /*eMultimedia*/, out device);
                object sessionManagerObj;
                device.Activate(typeof(IAudioSessionManager2).GUID, 0, IntPtr.Zero, out sessionManagerObj);
                var sessionManager = (IAudioSessionManager2)sessionManagerObj;
                IAudioSessionEnumerator sessionEnum;
                sessionManager.GetSessionEnumerator(out sessionEnum);
                int count;
                sessionEnum.GetCount(out count);
                int thisPid = System.Diagnostics.Process.GetCurrentProcess().Id;
                for (int i = 0; i < count; i++)
                {
                    IAudioSessionControl sessionCtrl;
                    sessionEnum.GetSession(i, out sessionCtrl);
                    var sessionCtrl2 = sessionCtrl as IAudioSessionControl2;
                    if (sessionCtrl2 == null) continue;
                    uint pid;
                    sessionCtrl2.GetProcessId(out pid);
                    if ((int)pid == thisPid)
                    {
                        var simpleVol = sessionCtrl as ISimpleAudioVolume;
                        if (simpleVol != null)
                        {
                            Guid empty = Guid.Empty;
                            simpleVol.SetMasterVolume(scalar, ref empty);
                        }
                    }
                }
            }
            catch { /* silent fallback — volume stays at WPF level */ }
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int NotImpl1();
            [System.Runtime.InteropServices.PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [System.Runtime.InteropServices.PreserveSig]
            int Activate([System.Runtime.InteropServices.In] Guid iid, int dwClsCtx, IntPtr pActivationParams, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.IUnknown)] out object ppInterface);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionManager2
        {
            int NotImpl1();
            int NotImpl2();
            [System.Runtime.InteropServices.PreserveSig]
            int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionEnumerator
        {
            [System.Runtime.InteropServices.PreserveSig]
            int GetCount(out int sessionCount);
            [System.Runtime.InteropServices.PreserveSig]
            int GetSession(int sessionCount, out IAudioSessionControl session);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl { }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl2
        {
            int NotImpl1(); int NotImpl2(); int NotImpl3(); int NotImpl4();
            int NotImpl5(); int NotImpl6(); int NotImpl7(); int NotImpl8();
            [System.Runtime.InteropServices.PreserveSig]
            int GetProcessId(out uint retvVal);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISimpleAudioVolume
        {
            [System.Runtime.InteropServices.PreserveSig]
            int SetMasterVolume(float fLevel, ref Guid eventContext);
        }
    }

    // ====================================================================
    // PLAYLIST TAB DATA MODEL
    // ====================================================================
    public class PlaylistTab
    {
        public string Name { get; set; }
        public System.Collections.Generic.List<string> Files { get; set; }
        public int CurrentIndex { get; set; }

        public PlaylistTab(string name)
        {
            Name = name;
            Files = new System.Collections.Generic.List<string>();
            CurrentIndex = -1;
        }
    }
}
