using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using DOA5Info;
// using BehaviorTree;
using BTree;


public static class DOA5Bot
{

	private static Node root = null;
	private static string treePath = "GUI_BTree_complexCondition.json";

	private static void LoadBehaviorTree()
	{
		root = BehaviorTreeSerialized.LoadTree(treePath);
		Console.WriteLine("Behavior tree reloaded successfully.");
	}

	// private static void buildTreeSerialized()
	// {
	// 	LoadBehaviorTree();
	// }
	private static void buildTreeSerialized()
	{
		// var gameData = GameDataLoader.Load("DOA5LR_Moves.json");

		// 假设我们要测试 Sarah 的 PunchCombo 动作
		// var SarahMoves = gameData.Characters["Sarah"].Moves;
		// Console.WriteLine("Sarah moves [\"9K\"] is {0}", SarahMoves["9K"].Inputs);
		// var punchComboMove = SarahMoves["9K"];
		//
        // 创建行为树
      
	
        // 保存行为树到文件
        // string filePath = "behavior_tree.json";
        // BehaviorTreeSerialized.SaveTree(rootToFile, filePath);
        //
        string filePath = "GUI_BTree_complexCondition.json";
        Node loadedRoot = BehaviorTreeSerialized.LoadTree(filePath);
        
        root = loadedRoot;
        
		// root = new SelectorNode(new List<Node>					   //
		// {														   //
		//    new SequenceNode(new List<Node>						   //
		//    {														   //
		//      new PerformMoveActionNode("4O-#71-6U-#25-P-#36-K"),	   //
		//    })													   //
		// }														   //
		//);

	}
	
    
	public static async Task Main(string[] args)
	{
		GameInfo.Init();	// open process, find aob address
		buildTreeSerialized();
		
		// select your side
		Console.WriteLine("Select your Player Slot: 1 = P1, 2 = P2");
		string side= Console.ReadLine();
		if (side == "2") GameInfo.SelectSide(GameInfo.PlayerSlot.P2);
		else GameInfo.SelectSide(GameInfo.PlayerSlot.P1);
		
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



