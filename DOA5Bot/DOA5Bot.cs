using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using DOA5Info;
// using BehaviorTree;
using BTree;



namespace BehaviorTree
{
	public class PerformMoveActionNode : Node
	{
		private Move move;
		// private int targetMoveType;

		public PerformMoveActionNode(Move moveData)
		{
			move = moveData;
		}

		public override NodeState Evaluate()
		{
			// int lastFrame = GameInfo.Player.CurrentMoveFrame;
			Console.WriteLine("enter perform move action node");
			if (state != NodeState.Running)
			{
				state = NodeState.Running;
				// 如果执行输入的动作不成功,则返回失败
				if (!ComboParser.ExecuteCombo(move.Inputs))
				{
					state = NodeState.Failure;
					// return state;
				}
				else
				{
					state = NodeState.Success;
					// return state;
				}
			}
			/*
			GameInfo.ReadCharacterInfo();
			int currentMoveType = GameInfo.Player.MoveType;
			int currentMoveTypeDetailed = GameInfo.Player.MoveTypeDetailed;
			int currentMoveFrame = GameInfo.Player.CurrentMoveFrame;
			int currentMove = GameInfo.Player.CurrentMove;

			// 这里需要注意的是如果连招输入后，两个动作间隔帧数恰好使得currentMoveFrame相同的特殊情况
			if(currentMoveFrame != lastFrame){
				if (currentMoveType == move.Move_Type && currentMove == move.Current_Move) // && currentMoveFrame < move.TotalFrames)
				{
					Console.WriteLine("Executing Move: " + move.Inputs);
					Console.WriteLine("Current Move Type: " + currentMoveType);
					Console.WriteLine("Current Move Type Detailed: " + currentMoveTypeDetailed);
					Console.WriteLine("Current Move Frame: " + currentMoveFrame);
					// if (currentMoveFrame < move.Frame_Total)
					// {
					// 	state = NodeState.Running;
					// }
					state = NodeState.Running;
				}else if (currentMoveType == 8 || currentMoveType == 9 || currentMoveType == 10 || currentMoveType == 11 || currentMoveType == 12)
				{
					Console.WriteLine("Move Failed: {0}, move type: {1}", move.Inputs, currentMoveType);
					state = NodeState.Failure;
				}
				else
				{
					// 有疑问的是，如果上面按键执行单个动作，比如输入P,1帧按键，成功之后，游戏状态确没刷新到Move_Type更新动作,那么这里也会成功
					// 或者执行一套动作，但是还没刷新到Move_Type更新动作，或Current_Move没到目标动作，那么这里也会成功, 所以能否在这里检查执行一套动作吗？怎样判断状态呢？
					state = NodeState.Success;
				}
			}else{
				// 还在同一帧中，应该等待
				state = NodeState.Running;
			}



			Console.WriteLine("exit perform move action node, node state: " + state);*/
			return state;
		}
	}
}

public static class DOA5Bot
{

	private static Node root = null;

	private static void buildTreeSerialized()
	{
		var gameData = GameDataLoader.Load("DOA5LR_Moves.json");

		// 假设我们要测试 Sarah 的 PunchCombo 动作
		var SarahMoves = gameData.Characters["Sarah"].Moves;
		Console.WriteLine("Sarah moves [\"9K\"] is {0}", SarahMoves["9K"].Inputs);
		// var punchComboMove = SarahMoves["9K"];
		
		/*
        // 创建行为树
        Node attack = new AttackNode();
        Node defend = new DefendNode();
        Node move = new MoveNode();

        var opThrowConditions = new List<Condition>
        {
            // new Condition { Property = "GameInfo.Opponent.MoveTypeDetailed", Operator = "==", TypedValue = new TypedValue {Type = "System.Int32", Value = 16} }
            new Condition { Property = "GameInfo.Opponent.MoveTypeDetailed", Operator = "==", Value = 16}
        };
            
        Node conditionOpThrow = new ConditionNode(opThrowConditions);
        
        var conditionsStandThrow = new List<Condition>
        {
            // new Condition { Property = "GameInfo.Opponent.HighMidLowGround", Operator = "<=", TypedValue = new TypedValue{Type = "System.Byte", Value = 2}},
            // new Condition { Property = "GameInfo.PX_Distance", Operator = "<", TypedValue = new TypedValue{Type = "System.Single",Value = 2.16f }},
            // new Condition { Property = "GameInfo.Opponent.TotalStartup", Operator = "<=", TypedValue = new TypedValue{Type = "System.Int16",Value = 10 }}
            new Condition { Property = "GameInfo.Opponent.HighMidLowGround", Operator = "<=", Value = 2 },
            new Condition { Property = "GameInfo.PX_Distance", Operator = "<", Value = 2.16 },
            new Condition { Property = "GameInfo.Opponent.TotalStartup", Operator = "<=", Value = 10 }
        };

        // string json = JsonConvert.SerializeObject(conditions, Formatting.Indented);
        // File.WriteAllText("conditions.json", json);
        
        // string json = File.ReadAllText("conditions.json");
        // var conditions = JsonConvert.DeserializeObject<List<Condition>>(json);
        var condsCloseCmdThrow = new List<Condition>
        {
        	new Condition{ Property = "GameInfo.Opponent.HighMidLowGround", Operator = "<=", Value = 2},
            new Condition{ Property = "GameInfo.PX_Distance", Operator = "<",Value = 1.90},
            new Condition {Property = "GameInfo.Opponent.TotalStartup", Operator = ">=", Value =  11}
            // new Condition{ Property = "GameInfo.Opponent.HighMidLowGround", Operator = "<=", TypedValue = new TypedValue{Type = "System.Byte", Value = 2}},
            // new Condition{ Property = "GameInfo.PX_Distance", Operator = "<",TypedValue = new TypedValue{Type = "System.Single",Value = 1.90f}},
            // new Condition {Property = "GameInfo.Opponent.TotalStartup", Operator = ">=", TypedValue = new TypedValue{Type = "System.Int16",Value =  11}}
        };
        
         var condsCrouchThrow = new List<Condition>
        {
            new Condition{ Property = "GameInfo.Opponent.HighMidLowGround", Operator = ">=",Value = 3},
            new Condition{ Property = "GameInfo.PX_Distance", Operator = "<",Value =  2.16},
            // new Condition{ Property = "GameInfo.Opponent.HighMidLowGround", Operator = ">=",TypedValue = new TypedValue{Type = "System.Byte", Value = 3}},
            // new Condition{ Property = "GameInfo.PX_Distance", Operator = "<",TypedValue = new TypedValue{Type = "System.Single",Value =  2.16f}},
        };
                

        Node selectThrow = new SelectorNode(new List<Node>
        {
            new SequenceNode(new List<Node>
            {
                new ConditionNode(conditionsStandThrow), // 示例条件
                new PerformMoveActionNode(SarahMoves["9K"].Inputs)
            }),
            new SequenceNode(new List<Node>
            {
                new ConditionNode(condsCloseCmdThrow), // 示例条件
                new PerformMoveActionNode(SarahMoves["2H+K"].Inputs)
            }),
            new SequenceNode(new List<Node>
            {
                new ConditionNode(condsCrouchThrow), // 示例条件
                new PerformMoveActionNode(SarahMoves["7K"].Inputs)
            })
        });

        Node counterThrow = new SequenceNode(new List<Node>
        {
            conditionOpThrow,
            selectThrow
        });

        Node rootToFile = new SelectorNode(new List<Node>
        {
            counterThrow,
            // defend,
            // move
        });
*/
        // 保存行为树到文件
        // string filePath = "behavior_tree.json";
        // BehaviorTreeSerialized.SaveTree(rootToFile, filePath);

        string filePath = "GUI_BTree_complexCondition.json";
        // 从文件读取行为树
        Node loadedRoot = BehaviorTreeSerialized.LoadTree(filePath);
        root = loadedRoot;
        // root = rootToFile;

        // root = new PerformMoveActionNode(SarahMoves["2H+K"].Inputs);
        // root = new SequenceNode(new List<Node>
        // {
        //  new ConditionNode(conditionsStandThrow), // 示例条件
        //  new PerformMoveActionNode(SarahMoves["9K"].Inputs)
        // });

        // 评估读取后的行为树
        // Console.WriteLine("Starting Behavior Tree Evaluation");
        // NodeState result = loadedRoot.Evaluate();
        // Console.WriteLine($"Behavior Tree Evaluation Result: {result}");
	}
	
	// build tree old without json serialization
	/*private static void buildTree()
	{
		var gameData = GameDataLoader.Load("DOA5LR_Moves.json");

		// 假设我们要测试 Sarah 的 PunchCombo 动作
		var SarahMoves = gameData.Characters["Sarah"].Moves;
		// var punchComboMove = SarahMoves["9K"];
		//
		// Console.WriteLine("Punch Combo Move: " + punchComboMove.Inputs);
		// Console.WriteLine("Punch Combo Move MoveType: " + punchComboMove.Move_Type);
		// Console.WriteLine("Punch Combo Move MoveTypeDetailed: " + punchComboMove.Move_Type_Detailed);
		// Console.WriteLine("Punch Combo Move CurrentMove: " + punchComboMove.Current_Move);
		// Console.WriteLine("Punch Combo Move TotalFrames: " + punchComboMove.Frame_Total);
		// Console.WriteLine("Punch Combo Move Frame_StartUp: " + punchComboMove.Frame_StartUp);
		// Console.WriteLine("Punch Combo Move Frame_Hit: " + punchComboMove.Frame_Hit);
		// Console.WriteLine("Punch Combo Move Frame_recovery: " + punchComboMove.Frame_Recovery);
		
		// 创建并执行行为树节点
		// var actionNode = new PerformMoveActionNode(punchComboMove);

		
		// 创建叶子节点
		Node attack = new AttackNode();
		Node defend = new DefendNode();
		Node move = new MoveNode();
		// THROWS AND OFFENSIVE HOLDS
		Node conditionOpThrow = new ConditionNode(() => GameInfo.Opponent.MoveTypeDetailed == 16);

		Node selectThrow = new SelectorNode(new List<Node>
		{
			new SequenceNode(new List<Node>
			{
				new ConditionNode(() => GameInfo.Opponent.HighMidLowGround <= 2 && GameInfo.PX_Distance < 2.16 && GameInfo.Opponent.TotalStartup <= 10),
				new PerformMoveActionNode(SarahMoves["9K"])
			}),
			new SequenceNode(new List<Node>
			{
				new ConditionNode(() => GameInfo.Opponent.HighMidLowGround <= 2 && GameInfo.PX_Distance < 1.90 && GameInfo.Opponent.TotalStartup >= 11),
				new PerformMoveActionNode(SarahMoves["2H+K"])
			}),
			new SequenceNode(new List<Node>
			{
				new ConditionNode(() => GameInfo.Opponent.HighMidLowGround >= 3 && GameInfo.PX_Distance < 2.16),
				new PerformMoveActionNode(SarahMoves["7K"])
			})
		});

		Node counterThrow = new SequenceNode(new List<Node>
		{
			conditionOpThrow,
			selectThrow
		});
	 
		
		// 创建复合节点
		root = new SelectorNode(new List<Node>
		{
			// attack,
			// actionNode,
			counterThrow,
			defend,
			move
		});
    
	}*/
	
	public static async Task Main(string[] args)
	{
		
	
		GameInfo.Init();	// open process, find aob address
		// buildTree();  // Build Behavior Tree
		buildTreeSerialized();
	
		
		
		
		// select your side
		Console.WriteLine("Select your Player Slot: 1 = P1, 2 = P2");
		string side= Console.ReadLine();
		if (side == "2") GameInfo.SelectSide(GameInfo.PlayerSlot.P2);
		else GameInfo.SelectSide(GameInfo.PlayerSlot.P1);
		
		// Combo parserexamples 
		// 6K 
		// 4-2-6P
		// 6-6K
		// string combo = "#3400-2-#40-4-#40-6.100-P.200-9K.500-U";
		// string combo = "#3400-P-#100-P-#100-P-#120-6.500";
		// ComboParser.ExecuteCombo(combo);
		// Console.ReadLine();

		
		using var cts = new CancellationTokenSource();
		Console.CancelKeyPress += (s, e) => {
			cts.Cancel();
			e.Cancel = true;
		};

		await RunLoopAsync(cts.Token);
	}

	static async Task RunLoopAsync(CancellationToken token)
	{
		
		var stopwatch = new Stopwatch();
		const int targetElapsedMilliseconds = 2;

		while (!token.IsCancellationRequested)
		{
			stopwatch.Restart();

			try
			{
				
				GameInfo.ReadCharacterInfo();	// Read Character Information
				
				
				Console.WriteLine("Starting Behavior Tree Evaluation");
				NodeState result = root.Evaluate();
				Console.WriteLine($"Behavior Tree Evaluation Result: {result}");	
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				// 考虑添加更多错误处理逻辑
			}

			int elapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds;
			int remainingTime = targetElapsedMilliseconds - elapsedMilliseconds;

			if (remainingTime > 0)
			{
				await Task.Delay(remainingTime, token);
			}
		}
	}
	
}



