using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NAudio.Wave;
using Newtonsoft.Json;

namespace ClaudeVoice
{
    public partial class MainWindow : Window
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string? currentRecordingPath;
        private readonly HttpClient httpClient;
        private Process? pythonBackendProcess;
        private Process? mcpServerProcess;
        private const string PYTHON_BACKEND_URL = "http://localhost:5000";
        private const string MCP_SERVER_PORT = "3000";

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await StartBackendServices();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRecording();
            pythonBackendProcess?.Kill();
            mcpServerProcess?.Kill();
        }

        private async Task StartBackendServices()
        {
            try
            {
                // Start Python backend
                var pythonBackendPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "PythonBackend");
                pythonBackendProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "whisper_server.py",
                        WorkingDirectory = pythonBackendPath,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                pythonBackendProcess.Start();

                // Wait a bit for the server to start
                await Task.Delay(2000);

                // Start MCP server
                var mcpServerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "MCPServer");
                mcpServerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = "server.js",
                        WorkingDirectory = mcpServerPath,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                mcpServerProcess.Start();

                await Task.Delay(1000);
                await CheckMcpServerStatus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error starting services: {ex.Message}");
            }
        }

        private async Task CheckMcpServerStatus()
        {
            try
            {
                var response = await httpClient.GetAsync($"{PYTHON_BACKEND_URL}/health");
                if (response.IsSuccessStatusCode)
                {
                    McpStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                    McpStatusText.Text = "Online";
                    McpStatusIndicator.ToolTip = "MCP Server Online";
                }
            }
            catch
            {
                // Keep offline status
            }
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
            _ = TranscribeAudio();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            TranscriptionPanel.Children.Clear();
            UpdateStatus("Transcription cleared");
        }

        private void StartRecording()
        {
            try
            {
                currentRecordingPath = Path.Combine(Path.GetTempPath(), $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

                waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 1) // 16kHz, Mono - optimal for Whisper
                };

                writer = new WaveFileWriter(currentRecordingPath, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                };

                waveIn.StartRecording();

                RecordButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                UpdateStatus("Recording... Speak now");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting recording: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopRecording()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }

            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }

            RecordButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            UpdateStatus("Recording stopped, transcribing...");
        }

        private async Task TranscribeAudio()
        {
            if (string.IsNullOrEmpty(currentRecordingPath) || !File.Exists(currentRecordingPath))
            {
                MessageBox.Show("No recording found to transcribe.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                UpdateStatus("Transcribing audio...");

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(File.ReadAllBytes(currentRecordingPath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(fileContent, "audio", Path.GetFileName(currentRecordingPath));

                var response = await httpClient.PostAsync($"{PYTHON_BACKEND_URL}/transcribe", content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TranscriptionResponse>(jsonResponse);

                if (result?.Words != null)
                {
                    DisplayTranscription(result.Words);
                    UpdateStatus($"Transcription complete - {result.Words.Count} words");
                }
                else
                {
                    UpdateStatus("No transcription result");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error transcribing audio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Transcription failed");
            }
            finally
            {
                // Clean up the recording file
                if (File.Exists(currentRecordingPath))
                {
                    try { File.Delete(currentRecordingPath); } catch { }
                }
            }
        }

        private void DisplayTranscription(List<WordInfo> words)
        {
            TranscriptionPanel.Children.Clear();

            foreach (var wordInfo in words)
            {
                var button = new Button
                {
                    Content = wordInfo.Word,
                    Style = null,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(2),
                    Margin = new Thickness(2),
                    FontSize = 16,
                    Cursor = Cursors.Hand,
                    ToolTip = $"Click to edit. Confidence: {wordInfo.Confidence:P0}"
                };

                // Color code by confidence
                if (wordInfo.Confidence < 0.7)
                {
                    button.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red for low confidence
                }
                else if (wordInfo.Confidence < 0.9)
                {
                    button.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange for medium confidence
                }
                else
                {
                    button.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)); // Dark for high confidence
                }

                button.Click += (s, e) => EditWord(button, wordInfo);

                TranscriptionPanel.Children.Add(button);
            }
        }

        private void EditWord(Button button, WordInfo wordInfo)
        {
            var inputBox = new TextBox
            {
                Text = wordInfo.Word,
                Width = 100,
                FontSize = 16,
                Padding = new Thickness(2),
                Margin = new Thickness(2)
            };

            inputBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    wordInfo.Word = inputBox.Text;
                    button.Content = inputBox.Text;
                    var index = TranscriptionPanel.Children.IndexOf(inputBox);
                    TranscriptionPanel.Children.RemoveAt(index);
                    TranscriptionPanel.Children.Insert(index, button);
                    UpdateStatus("Word updated");
                }
                else if (e.Key == Key.Escape)
                {
                    var index = TranscriptionPanel.Children.IndexOf(inputBox);
                    TranscriptionPanel.Children.RemoveAt(index);
                    TranscriptionPanel.Children.Insert(index, button);
                }
            };

            inputBox.LostFocus += (s, e) =>
            {
                if (TranscriptionPanel.Children.Contains(inputBox))
                {
                    wordInfo.Word = inputBox.Text;
                    button.Content = inputBox.Text;
                    var index = TranscriptionPanel.Children.IndexOf(inputBox);
                    TranscriptionPanel.Children.RemoveAt(index);
                    TranscriptionPanel.Children.Insert(index, button);
                }
            };

            var index = TranscriptionPanel.Children.IndexOf(button);
            TranscriptionPanel.Children.RemoveAt(index);
            TranscriptionPanel.Children.Insert(index, inputBox);
            inputBox.Focus();
            inputBox.SelectAll();
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }
    }

    public class TranscriptionResponse
    {
        public string? Text { get; set; }
        public List<WordInfo>? Words { get; set; }
    }

    public class WordInfo
    {
        public string Word { get; set; } = "";
        public double Start { get; set; }
        public double End { get; set; }
        public double Confidence { get; set; }
    }
}
