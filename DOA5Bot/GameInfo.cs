namespace DOA5Info
{
    using System.Diagnostics;
    using MemoryScanner;
    
    public static class GameInfo
    {
        private static string processName = "game";
        private static Process process = null;
        public static Process Process => process;
        private static MemScanner memScan = null;
        private static string _aobPattern = "af 47 e9 42";
        private static IntPtr _aobAddress = 0;
        public static CharacterInfo Player = new CharacterInfo();
        public static CharacterInfo Opponent = new CharacterInfo();
        public static float PX_Distance;
        public static short PX_TotalActiveFrames;
        public static bool IsInitialized { get; private set; }

        /*public static class Player
        {
	        public static byte Airborne;
	        public static byte ComboCounter;
	        public static byte CurrentCharacter;
	        public static short CurrentMove;
	        public static short CurrentMoveFrame;
	        public static int Direction;
	        public static byte HighMidLowGround;
	        public static byte MoveType;
	        public static int MoveTypeDetailed;
	        public static short Stance;
	        public static byte StrikeType;
	        public static short TotalRecovery;
	        public static short TotalStartup;
        }

        public static class Opponent
        {
	        public static byte Airborne;
	        public static byte ComboCounter;
	        public static byte CurrentCharacter;
	        public static short CurrentMove;
	        public static short CurrentMoveFrame;
	        public static int Direction;
	        public static byte HighMidLowGround;
	        public static byte MoveType;
	        public static int MoveTypeDetailed;
	        public static short Stance;
	        public static byte StrikeType;
	        public static short TotalRecovery;
	        public static short TotalStartup;
        }*/

        public struct CharacterInfo
        {
            public byte Airborne;
            public byte ComboCounter;
            public byte CurrentCharacter;
            public short CurrentMove;
            public short CurrentMoveFrame;
            public int Direction;
            public byte HighMidLowGround;
            public byte MoveType;
            public int MoveTypeDetailed;
            public short Stance;
            public byte StrikeType;
            public short TotalRecovery;
            public short TotalStartup;
        
        }
        public static void Init()
        {
            try
            {
                process = Process.GetProcessesByName(processName)[0];
                Console.WriteLine($"Process ID: {process.Id}, Title: {process.MainWindowTitle}");
                
				memScan = new MemScanner(process);
            }
            catch (Exception e)
            {
                Console.WriteLine("Process not found: " + e.ToString());
            }
            
            FindAOB();
            
            IsInitialized = true;
        }
        
        
        /*public static void Close()
        {
            if (memScan != null)
            {
                memScan.Dispose();
            }
        }*/
        private static void FindAOB()
        {
            try
            {
                List<IntPtr> results = memScan.ScanMemory(_aobPattern);
                 
                if (results != null && results.Count > 0)
                {
                    _aobAddress = results[0];
                    Console.WriteLine("Find aob address: " + _aobAddress.ToString("X"));
                }               
            }
            catch (Exception e)
            {
                Console.WriteLine("Find aob address Error: " + e.ToString());
            }

        }

        /*public static string GetAOBString()
        {
            if (_aobAddress == 0)
            {
                FindAOB();
            }
            return _aobAddress.ToString("X");
        }*/

        public enum PlayerSlot
		{
			P1 = 0,
			P2 = 1
        }
        
        private static PlayerSlot _curremtSlot = PlayerSlot.P1;
        public static void SelectSide(PlayerSlot playerSlot)
        {
	        if (playerSlot == PlayerSlot.P1)
	        {
		        _curremtSlot = PlayerSlot.P1;
	        }
	        else
	        {
		        _curremtSlot = PlayerSlot.P2;
	        }
        }
        
        public static void ReadCharacterInfo()
        {
	        if (!IsInitialized)
	        {
		        throw new InvalidOperationException("GameInfo is not initialized. Call Init() first.");
	        }
	        
	        if (_curremtSlot == PlayerSlot.P1)
	        {
		        Player.Airborne = memScan.ReadMem(_aobAddress + 0x35EF8, 1)[0]; // 0x35EF8;
				// Console.WriteLine("P1.Airborne: {0}", P1.Airborne);
				Player.ComboCounter = memScan.ReadMem(_aobAddress + 0x34E28, 1)[0];
				// Console.WriteLine("P1.ComboCounter: {0}", P1.ComboCounter);
				Player.CurrentCharacter = memScan.ReadMem(_aobAddress + 0x268F4, 1)[0];
				// Console.WriteLine("P1.CurrentCharacter: {0}", P1.CurrentCharacter);
				
				Player.CurrentMove = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x375EC, 2), 0);
				// Console.WriteLine("P1.CurrentMove: {0}", P1.CurrentMove);

				Player.CurrentMoveFrame = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x35EE0, 2), 0);
				// Console.WriteLine("P1.CurrentMoveFrame: {0}", P1.CurrentMoveFrame);
				
				Player.Direction = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x37B58, 4), 0);
				// Console.WriteLine("P1.Direction: {0}", P1.Direction);
				
				Player.HighMidLowGround = memScan.ReadMem(_aobAddress + 0x690, 1)[0];
				// Console.WriteLine("P1.HighMidLowGround: {0}", P1.HighMidLowGround);
				
				Player.MoveType = memScan.ReadMem(_aobAddress + 0x268FE, 1)[0];
				// Console.WriteLine("P1.MoveType: {0}", P1.MoveType);
				
				Player.MoveTypeDetailed = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x35E34, 4), 0); // 0x35E34
				// Console.WriteLine("P1.MoveTypeDetailed: {0}", P1.MoveTypeDetailed);
				
				Player.Stance = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37BD0, 2), 0);	// 0x37BD0
				// Console.WriteLine("P1.Stance: {0}", P1.Stance);
				Player.StrikeType = memScan.ReadMem(_aobAddress + 0x692, 1)[0]; // 0x692
				// Console.WriteLine("P1.StrikeType: {0}", P1.StrikeType);
				Player.TotalRecovery = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37BB2, 2), 0);  // 0x37BB2
				// Console.WriteLine("P1.TotalRecovery: {0}", P1.TotalRecovery);
				Player.TotalStartup = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37BAE, 2), 0);  // 0x37BAE
				// Console.WriteLine("P1.TotalStartup: {0}", P1.TotalStartup);
				
				// P2 info
				Opponent.Airborne = memScan.ReadMem(_aobAddress + 0x36380, 1)[0];
				// Console.WriteLine("P2.Airborne: {0}", P2.Airborne);
				Opponent.ComboCounter = memScan.ReadMem(_aobAddress + 0x34E3C, 1)[0];
				// Console.WriteLine("P2.ComboCounter: {0}", P2.ComboCounter);
				Opponent.CurrentCharacter = memScan.ReadMem(_aobAddress + 0x26924, 1)[0];
				// Console.WriteLine("P2.CurrentCharacter: {0}", P2.CurrentCharacter);
				Opponent.CurrentMove = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37CB4, 2), 0);
				// Console.WriteLine("P2.CurrentMove: {0}", P2.CurrentMove);
				Opponent.CurrentMoveFrame = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x36368, 2), 0);
				// Console.WriteLine("P2.CurrentMoveFrame: {0}", P2.CurrentMoveFrame);
				Opponent.Direction = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x38220, 4), 0);
				// Console.WriteLine("P2.Direction: {0}", P2.Direction);
				Opponent.HighMidLowGround = memScan.ReadMem(_aobAddress + 0x7D4, 1)[0];
				// Console.WriteLine("P2.HighMidLowGround: {0}", P2.HighMidLowGround);
				Opponent.MoveType = memScan.ReadMem(_aobAddress + 0x2692E, 1)[0];
				// Console.WriteLine("P2.MoveType: {0}", P2.MoveType);
				Opponent.MoveTypeDetailed = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x35E68, 4), 0);
				// Console.WriteLine("P2.moveTypeDetailed: {0}", P2.MoveTypeDetailed);
				Opponent.Stance = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x38298, 2), 0);
				// Console.WriteLine("P2.Stance: {0}", P2.Stance);
				Opponent.StrikeType = memScan.ReadMem(_aobAddress + 0x7D6, 1)[0];
				// Console.WriteLine("P2.StrikeType: {0}", P2.StrikeType);
				Opponent.TotalRecovery = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x3827A, 2), 0);
				// Console.WriteLine("P2.TotalRecovery: {0}", P2.TotalRecovery);
				Opponent.TotalStartup = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x38276, 2), 0);
				// Console.WriteLine("P2.TotalStartup: {0}", P2.TotalStartup);
	        }
	        else
	        {
		        Opponent.Airborne = memScan.ReadMem(_aobAddress + 0x35EF8, 1)[0]; // 0x35EF8;
		        // Console.WriteLine("P1.Airborne: {0}", P1.Airborne);
		        Opponent.ComboCounter = memScan.ReadMem(_aobAddress + 0x34E28, 1)[0];
		        // Console.WriteLine("P1.ComboCounter: {0}", P1.ComboCounter);
		        Opponent.CurrentCharacter = memScan.ReadMem(_aobAddress + 0x268F4, 1)[0];
		        // Console.WriteLine("P1.CurrentCharacter: {0}", P1.CurrentCharacter);

		        Opponent.CurrentMove = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x375EC, 2), 0);
		        // Console.WriteLine("P1.CurrentMove: {0}", P1.CurrentMove);

		        Opponent.CurrentMoveFrame = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x35EE0, 2), 0);
		        // Console.WriteLine("P1.CurrentMoveFrame: {0}", P1.CurrentMoveFrame);

		        Opponent.Direction = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x37B58, 4), 0);
		        // Console.WriteLine("P1.Direction: {0}", P1.Direction);

		        Opponent.HighMidLowGround = memScan.ReadMem(_aobAddress + 0x690, 1)[0];
		        // Console.WriteLine("P1.HighMidLowGround: {0}", P1.HighMidLowGround);

		        Opponent.MoveType = memScan.ReadMem(_aobAddress + 0x268FE, 1)[0];
		        // Console.WriteLine("P1.MoveType: {0}", P1.MoveType);

		        Opponent.MoveTypeDetailed = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x35E34, 4), 0); // 0x35E34
		        // Console.WriteLine("P1.MoveTypeDetailed: {0}", P1.MoveTypeDetailed);

		        Opponent.Stance = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37BD0, 2), 0); // 0x37BD0
		        // Console.WriteLine("P1.Stance: {0}", P1.Stance);
		        Opponent.StrikeType = memScan.ReadMem(_aobAddress + 0x692, 1)[0]; // 0x692
		        // Console.WriteLine("P1.StrikeType: {0}", P1.StrikeType);
		        Opponent.TotalRecovery = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37BB2, 2), 0); // 0x37BB2
		        // Console.WriteLine("P1.TotalRecovery: {0}", P1.TotalRecovery);
		        Opponent.TotalStartup = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37BAE, 2), 0); // 0x37BAE
		        // Console.WriteLine("P1.TotalStartup: {0}", P1.TotalStartup);

		        // P2 info
		        Player.Airborne = memScan.ReadMem(_aobAddress + 0x36380, 1)[0];
		        // Console.WriteLine("P2.Airborne: {0}", P2.Airborne);
		        Player.ComboCounter = memScan.ReadMem(_aobAddress + 0x34E3C, 1)[0];
		        // Console.WriteLine("P2.ComboCounter: {0}", P2.ComboCounter);
		        Player.CurrentCharacter = memScan.ReadMem(_aobAddress + 0x26924, 1)[0];
		        // Console.WriteLine("P2.CurrentCharacter: {0}", P2.CurrentCharacter);
		        Player.CurrentMove = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37CB4, 2), 0);
		        // Console.WriteLine("P2.CurrentMove: {0}", P2.CurrentMove);
		        Player.CurrentMoveFrame = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x36368, 2), 0);
		        // Console.WriteLine("P2.CurrentMoveFrame: {0}", P2.CurrentMoveFrame);
		        Player.Direction = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x38220, 4), 0);
		        // Console.WriteLine("P2.Direction: {0}", P2.Direction);
		        Player.HighMidLowGround = memScan.ReadMem(_aobAddress + 0x7D4, 1)[0];
		        // Console.WriteLine("P2.HighMidLowGround: {0}", P2.HighMidLowGround);
		        Player.MoveType = memScan.ReadMem(_aobAddress + 0x2692E, 1)[0];
		        // Console.WriteLine("P2.MoveType: {0}", P2.MoveType);
		        Player.MoveTypeDetailed = BitConverter.ToInt32(memScan.ReadMem(_aobAddress + 0x35E68, 4), 0);
		        // Console.WriteLine("P2.moveTypeDetailed: {0}", P2.MoveTypeDetailed);
		        Player.Stance = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x38298, 2), 0);
		        // Console.WriteLine("P2.Stance: {0}", P2.Stance);
		        Player.StrikeType = memScan.ReadMem(_aobAddress + 0x7D6, 1)[0];
		        // Console.WriteLine("P2.StrikeType: {0}", P2.StrikeType);
		        Player.TotalRecovery = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x3827A, 2), 0);
		        // Console.WriteLine("P2.TotalRecovery: {0}", P2.TotalRecovery);
		        Player.TotalStartup = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x38276, 2), 0);
		        // Console.WriteLine("P2.TotalStartup: {0}", P2.TotalStartup);
	        }
	        
		        // ;######
		        PX_Distance = BitConverter.ToSingle(memScan.ReadMem(_aobAddress + 0x1FE68, 4), 0);
		        // Console.WriteLine("PX_Distance: {0}", PX_Distance);
		        PX_TotalActiveFrames = BitConverter.ToInt16(memScan.ReadMem(_aobAddress + 0x37578, 2), 0);
		        // Console.WriteLine("PX_TotalActiveFrames: {0}", PX_TotalActiveFrames);
        }
    }
}