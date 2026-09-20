using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using NAudio.CoreAudioApi;

namespace audio_controller
{
    public partial class MainWindow : Window
    {
        // 画面にバインドするアプリ音量リスト
        public ObservableCollection<AppVolumeData> AppVolumes { get; set; } = new ObservableCollection<AppVolumeData>();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            AppVolumeList.ItemsSource = AppVolumes;

            // 起動時にオーディオセッション（アプリ一覧）を取得
            LoadAudioSessions();
        }

        // オーディオセッションを取得してリストに追加するメソッド
        private void LoadAudioSessions()
        {
            AppVolumes.Clear();
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                var sessions = device.AudioSessionManager;

                for (int i = 0; i < sessions.Sessions.Count; i++)
                {
                    var session = sessions.Sessions[i];
                    var processId = session.GetProcessID;

                    if (processId > 0)
                    {
                        try
                        {
                            var process = Process.GetProcessById((int)processId);
                            string appName = process.ProcessName;

                            // 重複追加を防ぎたい場合や、特定のシステム音を除外したい場合のフィルタもここに書けます
                            var volData = new AppVolumeData
                            {
                                AppName = appName,
                                Volume = session.SimpleAudioVolume.Volume * 100
                            };

                            // スライダーが動いたときにWindows側の音量を変更する
                            volData.VolumeChanged += (val) =>
                            {
                                session.SimpleAudioVolume.Volume = (float)(val / 100.0);
                            };

                            AppVolumes.Add(volData);
                        }
                        catch
                        {
                            // プロセスがすでに終了している場合などのエラーをスルー
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"オーディオセッションの取得に失敗しました: {ex.Message}");
            }
        }

        // ================= Window操作用 =================

        // タイトルバーをドラッグしてウィンドウを動かす
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // 最小化ボタン
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 閉じるボタン
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // 各アプリの音量データを管理するクラス
    public class AppVolumeData : INotifyPropertyChanged
    {
        private double _volume;
        public string AppName { get; set; } = "";

        // スライダーの値が変更されたときに発火するイベント
        public event Action<double>? VolumeChanged;

        public double Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged(nameof(Volume));
                    VolumeChanged?.Invoke(_volume); // 実際の音量変更処理へ通知
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}