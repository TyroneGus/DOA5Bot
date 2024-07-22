using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using DOA5Info;
using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;
using Newtonsoft.Json;

public class Move
{
    public string Inputs { get; set; }
    public int Frame_Total{ get; set; }
    public int Move_Type { get; set; }
    public int Move_Type_Detailed { get; set; }
    public int Current_Move { get; set; }
    public int Frame_StartUp { get; set; }
    public int Frame_Hit{ get; set; }
    public int Frame_Recovery{ get; set; }
	
    public string[] Next_Combos { get; set; }
}

public class Character
{
    public Dictionary<string, Move> Moves { get; set; }
}

public class GameData
{
    public Dictionary<string, Character> Characters { get; set; }
}


public class GameDataLoader
{
    public static GameData Load(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<GameData>(json);
        }
        catch (IOException e)
        {
            Console.WriteLine($"IO Error loading game data: {e.Message}");
            return null;
        }
        catch (JsonException e)
        {
            Console.WriteLine($"JSON Error loading game data: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error loading game data: {e.Message}");
            return null;
        }
    }
}


///<summary>
/// Change milliseconds to frames
/// /// AI Claude 3.5 Sonnet Prompt:
/// I want to design a text representation of the move list for Dead or Alive 5, which can be parsed and called using C#.
/// For example, P means punching and pressing the k key, K means kicking and pressing the L key, U means pressing the k l key at the same time, O means pressing the j l key at the same time, H means pressing the j key for defense, T means pressing the m key for throwing skills,
/// and 2 means Press s in the down direction, 6 means press d in the right direction, 8 means press w in the up direction, 4 means press a in the left direction, 1 means press a and s in the down and left direction at the same time, 3 means press s in the down and right direction at the same time.  9 means pressing d and w at the same time in the upper right direction, 7 means pressing a and w at the same time in the upper left direction,
/// P.10 means holding down the punch for 10 frames, K.30 means holding down the kick for 30 frames, 6P means pressing D at the same time Punch, #4 represents the delay sleep 4 frames, - represents the separation symbol.
///
/// Example 2-#4-4-#4-6.10-P.20-9K.50-U means pressing the down button with an interval of 4 frames, pressing the left button with an interval of 4 frames, holding down the right button for 10 frames, and holding down the punch for 20 frames. The upper right and kick last for 50 frames, and press k l at the same time. Please evaluate this solution and provide an optimization solution
///
/// change H+K to O, P+K to U, H+P+K to I
/// 
/// 我想为死或生5 设计一种文字表示出招表，能用C#解析成出来并调用方法。例如 P 表示拳 按下 k键，K表示踢 按下L键，U表示同时按下k l, O 表示同时按下j l, H表示防御按下j键，T表示投技 按下m键，2表示方向下按下s ,6表示方向右按下d,8表示方向上按下w，4表示方向左按下a，1表示方向左下同时按下a、s，3表示方向右下同时按下s、d，9表示方向右上同时按下d、w，7表示左上同时按下a、w，P.10表示按住拳10 frames，K.30表示按住踢30 frames，6P 表示同时按下D 和拳，#4 表示延迟sleep 4 frames, -表示分隔符号。示例2-#4-4-#4-6.10-P.20-9K.50-U,表示按下下键，间隔4 frames,按下左键，间隔4 frames,按住右键10 frames, 按住拳20 frames,按住右上和踢持续50 frames,同时按下k l.请评估这种方案，给出优化方案
/// 更改 H+K 为 O, P+K 为 U, H+P+K 为 I 
/// </summary>
public class ComboParser
{

    private static Dictionary<char, VirtualKeyCode> actionMap = new Dictionary<char, VirtualKeyCode>
    {
        {'P', VirtualKeyCode.VK_K}, {'K', VirtualKeyCode.VK_L}, {'H', VirtualKeyCode.VK_J}, {'T', VirtualKeyCode.VK_M},
        {'U', VirtualKeyCode.VK_U}, {'O', VirtualKeyCode.VK_O}, {'I', VirtualKeyCode.VK_I},  // H+K to O, P+K to U, H+P+K to I
        {'2', VirtualKeyCode.VK_S}, {'8', VirtualKeyCode.VK_W}
        // {'4', VirtualKeyCode.VK_A}, {'6', VirtualKeyCode.VK_D}
    };

    public static int DefautKeyPressDuration = 3;
    

    public static bool ExecuteCombo(string combo)
    {
        string[] actions = combo.Split('-');
        foreach (string action in actions)
        {
            if (action.StartsWith("#"))
            {
                int delay = int.Parse(action.Substring(1));
                if(GameSynchronizer.SyncResult.Success != GameSynchronizer.SyncWithGameFrames(delay, false)) return false; // SyncWithGameFrames(delay);
            }
            else
            {
                if(!ExecuteAction(action)) return false;
            }
            
            // 如果人物动作类型为8，9，10，11，12，则执行失败
            GameInfo.ReadCharacterInfo();
            int move_type = GameInfo.Player.MoveType;
            if (move_type == 8 || move_type == 9 || move_type == 10 || move_type == 11 || move_type == 12)
            {
                Console.WriteLine("Executing Combo Failed: " + combo);
                return false;
            }
        }

        return true;
    }

    
    private static bool ExecuteAction(string action)
    {
        if (action.Contains("."))
        {
            string[] parts = action.Split('.');
            int duration = int.Parse(parts[1]);
            return PressKeys(parts[0], duration);
        }
        else
        {
            return PressKeys(action, DefautKeyPressDuration); // 默认按键时间为3 frames
        }
    }

    private static InputSimulator simulator = new InputSimulator();
    private static bool PressKeys(string keys, int duration)
    {
        List<VirtualKeyCode> keysToPress = new List<VirtualKeyCode>();

        foreach (char c in keys)
        {
            // right side ,revert 4,6 direction
            if (c == '4')
            {
                if (GameInfo.Player.Direction == 1 || GameInfo.Player.Direction == 65792)
                {
                    keysToPress.Add(VirtualKeyCode.VK_D);
                }
                else
                {
                    keysToPress.Add(VirtualKeyCode.VK_A);
                }
            }
            else if (c == '6')
            {
                if (GameInfo.Player.Direction == 1 || GameInfo.Player.Direction == 65792)
                {
                    keysToPress.Add(VirtualKeyCode.VK_A);
                }
                else
                {
                    keysToPress.Add(VirtualKeyCode.VK_D);
                }
            }
            else if (c == '1')
            {
                if (GameInfo.Player.Direction == 1 || GameInfo.Player.Direction == 65792)
                {
                    keysToPress.Add(VirtualKeyCode.VK_D);
                    keysToPress.Add(VirtualKeyCode.VK_S);
                }
                else
                {
                    keysToPress.Add(VirtualKeyCode.VK_A);
                    keysToPress.Add(VirtualKeyCode.VK_S);
                }
            }
            else if (c == '3')
            {
                if (GameInfo.Player.Direction == 1 || GameInfo.Player.Direction == 65792)
                {
                    keysToPress.Add(VirtualKeyCode.VK_S);
                    keysToPress.Add(VirtualKeyCode.VK_A);
                }
                else
                {
                    keysToPress.Add(VirtualKeyCode.VK_S);
                    keysToPress.Add(VirtualKeyCode.VK_D);
                }
            }
            else if (c == '7')
            {
                if (GameInfo.Player.Direction == 1 || GameInfo.Player.Direction == 65792)
                {
                    keysToPress.Add(VirtualKeyCode.VK_D);
                    keysToPress.Add(VirtualKeyCode.VK_W);
                }
                else
                {
                    keysToPress.Add(VirtualKeyCode.VK_A);
                    keysToPress.Add(VirtualKeyCode.VK_W);
                }
            }
            else if (c == '9')
            {
                if (GameInfo.Player.Direction == 1 || GameInfo.Player.Direction == 65792)
                {
                    keysToPress.Add(VirtualKeyCode.VK_A);
                    keysToPress.Add(VirtualKeyCode.VK_W);
                }
                else
                {
                    keysToPress.Add(VirtualKeyCode.VK_D);
                    keysToPress.Add(VirtualKeyCode.VK_W);
                }

            }
            else
            {
                if (actionMap.ContainsKey(c))
                {
                    keysToPress.Add(actionMap[c]);
                }
                else
                {
                    Console.WriteLine("Invalid key: " + c);
                }               
            }
        }


        foreach (VirtualKeyCode key in keysToPress)
        {
            simulator.Keyboard.KeyDown(key);
            Console.WriteLine("Key Down: " + key);
        }

        bool success = GameSynchronizer.SyncResult.Success == GameSynchronizer.SyncWithGameFrames(duration);
           
        foreach (VirtualKeyCode key in keysToPress)
        {
            
            simulator.Keyboard.KeyUp(key);
            Console.WriteLine("Key Up: " + key);
        }

        return success;
    }
}

public class GameSynchronizer
{
    private const int maxUnchangedFrames = 5; // 允许的最大连续相同帧数
    
    public enum SyncResult
    {
        Success,
        Interrupted,
        Failed
    }

    public static SyncResult SyncWithGameFrames(int frames, bool isKeyAction = false)
    {
        int lastFrame = GameInfo.Player.CurrentMoveFrame;
        int currentFrame = lastFrame;
        int unchangedFrameCount = 0;

        // 记录初始状态
        int initialMoveType = GameInfo.Player.MoveType;

        while (frames > 0)
        {
            GameInfo.ReadCharacterInfo();
            currentFrame = GameInfo.Player.CurrentMoveFrame;
            Console.WriteLine($"Current Frame: {currentFrame}, MoveType: {GameInfo.Player.MoveType}");

            if (currentFrame != lastFrame)
            {
                lastFrame = currentFrame;
                frames--;
                Console.WriteLine($"Frame Counter: {frames}");
                unchangedFrameCount = 0;
            }
            else
            {
                unchangedFrameCount++;
                if (unchangedFrameCount >= maxUnchangedFrames)
                {
                    Console.WriteLine("Frame unchanged for too long, possible game pause or lag.");
                    return SyncResult.Failed;
                }
            }

            // 检查是否被中断
            if (IsActionInterrupted(initialMoveType, GameInfo.Player.MoveType, isKeyAction))
            {
                Console.WriteLine("Action interrupted.");
                return SyncResult.Interrupted;
            }

            ShortDelay(2);
        }

        Console.WriteLine($"Frame Delay Done: {currentFrame}");
        return SyncResult.Success;
    }

    private static bool IsActionInterrupted(int initialMoveType, int currentMoveType, bool isKeyAction)
    {
        // 定义可能的正常状态转换
        bool isNormalTransition = IsNormalMoveTypeTransition(initialMoveType, currentMoveType);

        // 如果是正常的状态转换，不视为中断
        if (isNormalTransition)
        {
            return false;
        }

        // 检查是否进入了被打断状态（7-12或15）
        return currentMoveType >= 7 && currentMoveType <= 12 || currentMoveType == 15;
    }

    private static bool IsNormalMoveTypeTransition(int initialMoveType, int currentMoveType)
    {
        // define the possible normal move type transitions
        // TODO: more test
        switch (initialMoveType)
        {
            case 0: // 从空闲状态
                return currentMoveType == 2 || currentMoveType == 3 || currentMoveType == 4 || currentMoveType==5 || currentMoveType==6 || currentMoveType == 13 || currentMoveType == 14;
            case 2: // 从攻击状态
                return currentMoveType == 2 || currentMoveType == 3 || currentMoveType == 4 || currentMoveType==5 || currentMoveType==6 || currentMoveType == 13 || currentMoveType == 14;
            case 3:
                return currentMoveType == 2 || currentMoveType == 3 || currentMoveType == 4 || currentMoveType==5 || currentMoveType==6 || currentMoveType == 13 || currentMoveType == 14;
            case 4: // 从投掷状态
                return currentMoveType == 2 || currentMoveType == 3 || currentMoveType == 4 || currentMoveType==5 || currentMoveType==6 || currentMoveType == 13 || currentMoveType == 14;
            case 13: // 从移动状态
                return currentMoveType == 2 || currentMoveType == 3 || currentMoveType == 4 || currentMoveType==5 || currentMoveType==6 || currentMoveType == 13 || currentMoveType == 14;
            default:
                return false; // 其他情况视为非正常转换
        }
    }
    private static void ShortDelay(int milliseconds)
    {
        var startTime = Stopwatch.GetTimestamp();
        SpinWait.SpinUntil(() => Stopwatch.GetElapsedTime(startTime).TotalMilliseconds >= milliseconds);
    } 
}
