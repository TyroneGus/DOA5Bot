using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DOA5Info;

namespace DOA5BotComboFrame
{
    public partial class MainForm : Form
    {
        private TextBox inputTextBox;
        private Button startButton;
        private Button executeComboButton;
        private TextBox resultTextBox;
        private const int maxUnchangedFrames = 5; // 允许的最大连续相同帧数

        public MainForm()
        {
            // InitializeComponent();
            InitializeUI();
            try
            {
                GameInfo.Init();
                if (!GameInfo.IsInitialized)
                {
                    MessageBox.Show(
                        "Failed to initialize game info. Please make sure the game is running.",
                        "Initialization Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    this.Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error initializing game info: {ex.Message}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                this.Close();
                return;
            }
        }

        private void InitializeUI()
        {
            this.Size = new System.Drawing.Size(400, 300);
            this.Text = "Combo Tester";

            inputTextBox = new TextBox
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(360, 20)
            };
            this.Controls.Add(inputTextBox);

            startButton = new Button
            {
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(100, 30),
                Text = "Start Test"
            };
            startButton.Click += StartButton_Click;
            this.Controls.Add(startButton);

            resultTextBox = new TextBox
            {
                Location = new System.Drawing.Point(10, 80),
                Size = new System.Drawing.Size(360, 150),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(resultTextBox);

            executeComboButton = new Button
            {
                Location = new System.Drawing.Point(120, 40),
                Size = new System.Drawing.Size(100, 30),
                Text = "Execute Combo"
            };
            executeComboButton.Click += ExecuteComboButton_Click;
            this.Controls.Add(executeComboButton);
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            string input = inputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter a combo string.");
                return;
            }

            startButton.Enabled = false;
            resultTextBox.Clear();

            try
            {
                SwitchToGameWindow();
                Thread.Sleep(600);
                string result = await TestComboAsync(input);
                resultTextBox.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error occurred while testing combo: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                resultTextBox.Text = $"Error: {ex.Message}";
            }
            finally
            {
                startButton.Enabled = true;
            }
        }

        private async Task<string> TestComboAsync(string input)
        {
            string[] moves = input.Split('-');
            string testedCombo = "";
            int comboCounter = 0;

            for (int i = 0; i < moves.Length - 1; i++)
            {
                string currentMove = moves[i];
                string nextMove = moves[i + 1];

                int framesBetweenMoves = await TestMovesPairAsync(
                    testedCombo,
                    currentMove,
                    nextMove
                );

                if (framesBetweenMoves == -1)
                {
                    return $"Failed to execute combo at move: {currentMove}-{nextMove}";
                }

                if (i == 0)
                {
                    testedCombo = $"{currentMove}-#{framesBetweenMoves}-{nextMove}";
                }
                else
                {
                    testedCombo += $"-#{framesBetweenMoves}-{nextMove}";
                }

                comboCounter++;
            }

            return testedCombo;
        }

        private async Task<int> TestMovesPairAsync(
            string previousCombo,
            string currentMove,
            string nextMove
        )
        {
            string comboToTest =
                previousCombo.Length > 0
                    ? $"{previousCombo}-{currentMove}-{nextMove}"
                    : $"{currentMove}-{nextMove}";
            int initialComboCounter = GameInfo.Player.ComboCounter;
            int framesBetweenMoves = 0;

            while (framesBetweenMoves < 60) // Limit to 60 frames (1 second)
            {
                string testCombo =
                    previousCombo.Length > 0
                        ? $"{previousCombo}-#{framesBetweenMoves}-{currentMove}-{nextMove}"
                        : $"{currentMove}-#{framesBetweenMoves}-{nextMove}";

                if (ComboParser.ExecuteCombo(testCombo))
                {
                    await Task.Delay(2); // Wait for the game to update
                    GameInfo.ReadCharacterInfo();
                    if (GameInfo.Player.ComboCounter > initialComboCounter)
                    {
                        return framesBetweenMoves;
                    }
                }

                framesBetweenMoves++;
                GameSynchronizer.SyncWithGameFrames(1, false);
            }

            return -1; // Failed to execute combo
        }

        private async void ExecuteComboButton_Click(object sender, EventArgs e)
        {
            string input = inputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter a combo string.");
                return;
            }

            executeComboButton.Enabled = false;
            resultTextBox.Clear();

            try
            {
                await Task.Run(() =>
                {
                    SwitchToGameWindow();
                    System.Threading.Thread.Sleep(1000); // 等待 1 秒，确保切换完成

                    bool result = ComboParser.ExecuteCombo(input);
                    UpdateUI($"Combo execution result: {(result ? "Success" : "Failed")}");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error occurred while executing combo: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                UpdateUI($"Error: {ex.Message}");
            }
            finally
            {
                executeComboButton.Enabled = true;
            }
        }

        private void SwitchToGameWindow()
        {
            // 这里需要实现切换到游戏窗口的逻辑
            // 可以使用 Windows API 来实现
            // 以下是一个示例实现，您可能需要根据实际情况调整
            /*IntPtr hWnd = FindWindow(null, "Dead or Alive 5 Last Round");
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
            }
            else
            {
                throw new Exception("Game window not found");
            }*/

            if (!GameInfo.IsInitialized)
            {
                throw new InvalidOperationException("GameInfo is not initialized.");
            }

            if (GameInfo.Process == null || GameInfo.Process.HasExited)
            {
                throw new Exception("Game process is not available.");
            }

            IntPtr hWnd = GameInfo.Process.MainWindowHandle;
            if (hWnd == IntPtr.Zero)
            {
                throw new Exception("Game window handle is not valid.");
            }

            if (!SetForegroundWindow(hWnd))
            {
                throw new Exception("Failed to set game window to foreground.");
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private CancellationTokenSource _cts;

        private void UpdateUI(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateUI), message);
            }
            else
            {
                resultTextBox.AppendText(message + Environment.NewLine);
                resultTextBox.ScrollToCaret();
            }
        }

        private void CancelSyncButton_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            base.OnFormClosing(e);
        }
    }
}
