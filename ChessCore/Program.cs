using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ChessCoreEngine.Utils;
using ChessEngine.Engine;

class Program
{
    static Logger logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public enum ProgramModes { BOT, TEST }

    bool ShowBoard { get; set; }
    Engine engine;
    ProgramModes programMode;
    bool runInThinkingMode = false;

    static void Main(string[] args)
	{
        Program p = new Program();
        p.Init(args);
		p.RunEngine();
	}

    private void Init(string[] args)
    {
        string[] coords = { "" };
        bool enabledConsoleLog = false;
        Logger.LogLevels logLevel = Logger.LogLevels.INFO;
        List<LogTarget> logTagets = new List<LogTarget>() { new FileLogTarget("MyLogFile.log", createEmptyLogFile: true) };

        foreach (string arg in args)
        {
            string[] argData = arg.Split('=', 2);

            string command = argData[0].Trim();

            if (command.StartsWith("/consoleLog") && argData.Length > 1)
            {
                if (string.Equals(argData[1].Trim().ToLower(), "true")) { logTagets.Add(new ConsoleLogTarget()); enabledConsoleLog = true; }
                continue;
            }

            if (command.StartsWith("/logLevel") && argData.Length > 1)
            {
                if (!Enum.TryParse<Logger.LogLevels>(argData[1].Trim(), true, out logLevel)) logLevel = Logger.LogLevels.INFO;
                continue;
            }

            if (command.StartsWith("/mode") && argData.Length > 1)
            {
                if (!Enum.TryParse<ProgramModes>(argData[1].Trim(), true, out programMode)) programMode = ProgramModes.BOT;
                continue;
            }

            if (command.StartsWith("/setwin") && argData.Length > 1)
            {
                coords = argData[1].Split(',');
                if (coords.Length == 4)
                {
                    Span<string> coordinates = new Span<string>(coords);
                    SeConsoleWindowPosition(coordinates);
                }

                continue;
            }
        }

        LogManager.LogLevel = logLevel;
        LogManager.LogTargets.AddRange(logTagets);

        if (logger.IsInfoLevelLog) logger.Info($"Command line args: \"{string.Join(" ",args)}\"");
        if (logger.IsInfoLevelLog) logger.Info($"Program configured data: /mode={programMode} /consoleLog={enabledConsoleLog.ToString().ToLower()} /logLevel={logLevel.ToString().ToUpper()} /setwin={string.Join(",", coords)}");

        ShowBoard = false;
        engine = new Engine();
    }

    private void RunEngine()
    {
        if (programMode == ProgramModes.BOT)
        {
            RunAsBot();
        }
        else
        {

            RunInTestMode();
        }
    }

    void RunAsBot()
    {
        (bool requestedExit, string[] replyMessage) calculatedResult;

        Console.WriteLine("Chess Core");
		Console.WriteLine("Created by Adam Berent");
		Console.WriteLine("Version: 1.0.0");
		Console.WriteLine("");
		Console.WriteLine("Type \"quit\" to exit");
		Console.WriteLine("Type \"show\" to show board");
        Console.WriteLine("Type \"win\" to show console windows position");
        //Console.WriteLine("Type \"setwin 396 -1149 993 519\" to set windows position and size.");
        Console.WriteLine("");
        //Console.WriteLine("feature setboard=1");

		while (true)
		{
			try
			{
                if (ShowBoard) Console.WriteLine(engine.DrawBoard());

				if (engine.WhoseMove != engine.HumanPlayer)
				{
                    calculatedResult =  (false, MakeEngineMove(engine, runInThinkingMode));
				}
				else
				{
					Console.WriteLine();

					string move = Console.ReadLine();

                    if (logger.IsInfoLevelLog) logger.Info($">>: \"{move}\"");

                    if (String.IsNullOrWhiteSpace(move)) continue;

					move = move.Trim();

                    calculatedResult = ApplyCommand(move, runInThinkingMode);

                    if (calculatedResult.requestedExit) { if (logger.IsInfoLevelLog) logger.Info($"Program ending"); return; }

                    if (logger.IsInfoLevelLog) logger.Info($"Chess FEN:\"{engine.FEN}\"");

                    if (calculatedResult.replyMessage != null)
                    {
                        foreach (string messageItem in calculatedResult.replyMessage)
                        {
                            if (logger.IsInfoLevelLog) logger.Info($"<<: \"{messageItem}\"");
                            Console.WriteLine(messageItem);
                        }
                    }
				}
			}
			catch (Exception ex)
			{
                if (logger.IsErrorLevelLog) logger.Error($"Received error:{ex.Message}"); 
				return;
			}
		}
	}

    class Command
    {
        public string ExternalCommand { get; set; }
        public string[] Results { get; set; }
        //public Command(string externalCommand, string[] results) { ExternalCommand = externalCommand; Results = results; }
        public Command(string externalCommand, params string[] results) { ExternalCommand = externalCommand; Results = results; }
        public Command(string externalCommand, string result = null) : this(externalCommand, ((result == null) ? null : new string[] { result })) { }
    }

    void RunInTestMode()
    {
        List<Command> commands = new List<Command>()
        {
            new Command("xboard"),
            new Command("protover 2", "feature setboard=1", "feature debug=1"),
            // new Command(), //, "accepted setboard"),
            // new Command(), //, "accepted debug"),
            new Command("new"),
            new Command("random"),
            new Command("level 40 10 0"),
            new Command("post"),
            new Command("hard"),
            new Command("time 60000"),
            new Command("otim 60000"),
            new Command("d2d4", ".*", "move Ng8f6"),
            new Command("time 59990"),
            new Command("otim 59829"),
            new Command("e2e4", ".*Nf6xe4 Nb1d2 Ne4d6 Ng1f3 Nb8c6 Bd3", "move Nf6xe4"),
            new Command("time 57904"),
            new Command("otim 59273"),
            new Command("f1d3", ".*d5 Bf4 Nb8c6 Ng1f3 Be6 O-O", "move d5"),
            new Command("time 56619"),
            new Command("otim 57937"),
            new Command("f2f3", ".*Ne4d6 Nb1c3 e6 Ng1e2 Qh4 g3", "move Ne4d6"),
            new Command("time 55600"),
            new Command("otim 56782"),
            new Command("c1f4", ".*e6 Ng1e2 Be7 O-O O-O Nb1c3", "move e6"),
            new Command("time 53313"),
            new Command("otim 56171"),
            new Command("b1c3", ".*Be7 Ng1e2 O-O O-O Nb8c6 Be5", "move Be7"),
            new Command("time 50623"),
            new Command("otim 55650"),
            new Command("c3b5", ".*O - O Ng1e2 Nd6xb5 Bxb5 Bd7 O-O", "move Kg8"),
            new Command("time 48540"),
            new Command("otim 54967"),
            new Command("b5d6", ".*Bxd6 Bxd6 Qxd6 Ng1e2 Nb8c6 O-O", "move Bxd6"),
            new Command("time 47018"),
            new Command("otim 54748"),
            new Command("f4d6", ".*Qxd6 Qd2 Qb6 Ng1e2 Qxb2 O-O", "move Qxd6"),
            new Command("time 45812"),
            new Command("otim 54104"),
            new Command("g1e2", ".*Qb4 Qd2 Qxd2 Kxd2 Nb8c6 Kc1", "move Qb4"),
            new Command("time 44538"),
            new Command("otim 53014"),
            new Command("c2c3", ".* Qxb2 O-O Nb8c6 Qb3 Qd2 Ra1d1", "move Qxb2"),
            new Command("time 43016"),
            new Command("otim 52541"),
            new Command("d1b1", ".*Qxb1 Ra1xb1 Nb8d7 O-O Nd7f6 Ne2f4", "move Qxb1"),
            new Command("time 42460"),
            new Command("otim 52342"),
            new Command("a1b1", ".*b6 O-O Ba6 Bxa6 Nb8xa6 Ne2f4", "move b6"),
            new Command("time 41939"),
            new Command("otim 51891"),
            new Command("c3c4", ".* Ba6 Ne2f4 Bxc4 Bxc4 dxc4 O-O", "move Ba6"),
            new Command("time 40890"),
            new Command("otim 50960"),
            new Command("e2f4", ".*Bxc4 Bxc4 dxc4 O-O Nb8d7 Rf1c1", "move Bxc4"),
            new Command("time 40061"),
            new Command("otim 50636"),
            new Command("d3c4", ".*dxc4 O-O Nb8c6 d5 exd5 Nf4xd5", "move dxc4"),
            new Command("time 39042"),
            new Command("otim 49789"),
            new Command("d4d5", ".*exd5 Nf4xd5 Rf8e8 Kf1 c5 Nd5c7", "move exd5"),
            new Command("time 38561"),
            new Command("otim 49339"),
            new Command("f4d5", ".*Rf8e8 Kf2 Nb8a6 Rh1c1 c6 Nd5b4", "move Rf8e8"),
            new Command("time 38151"),
            new Command("otim 48722"),
            new Command("e1f2", ".*Nb8a6 Rh1e1 Re8xe1 Rb1xe1 c5 Kg1", "move Nb8a6"),
            new Command("time 37110"),
            new Command("otim 47908"),
            new Command("h1e1", ".*Re8xe1 Rb1xe1 Na6c5 Kg1 Nc5d3 Re1e7", "move Re8xe1"),
            new Command("time 36331"),
            new Command("otim 47652"),
            new Command("b1e1", ".*Na6c5 Kg1 Nc5e6 Re1e4 c6 Nd5e7 Kf8", "move Na6c5"),
            new Command("time 35758"),
            new Command("otim 47122"),
            new Command("d5c7", ".*Nc5d3 Kf1 Nd3xe1 Nc7xa8 Ne1d3 Na8c7", "move Nc5d3"),
            new Command("result 0 - 1 { White resigns}"),
            new Command("force"),
            new Command("quit")
        };


        try
        {
            foreach (Command command in commands)
            {
                if (logger.IsInfoLevelLog) logger.Info($">>: \"{command.ExternalCommand}\"");

                (bool requestedExit, string[] resultMassages) = ApplyCommand(command.ExternalCommand, runInThinkingMode);

                if (resultMassages != null)
                {
                    if (command.Results == null || resultMassages.Length != command.Results.Length) logger.Error($"Expected empty returned result but returned # {resultMassages.Length} messages.");

                    for (int i = 0; i < resultMassages.Length; i++)
                    {
                        string messageItem = (string)resultMassages[i];
                        if (logger.IsInfoLevelLog) logger.Info($"<<: \"{messageItem}\"");

                        if (i < command.Results.Length)
                        {
                            Regex regex = new Regex(command.Results[i]);
                            if (!regex.IsMatch(messageItem)) logger.Error($"Returned result \"{messageItem}\" different from expected \"{command.Results[i]}\" message.");
                        }
                    }

                    if (logger.IsInfoLevelLog) logger.Info($"Chess FEN:\"{engine.FEN}\"");
                    Console.WriteLine(engine.DrawBoard());
                }
                else
                {
                    if (command.Results != null) logger.Error($"Expected # {command.Results.Length} messages but returned empty result set.");
                }
            }
        }
        catch(Exception e)
        {
            if (logger.IsErrorLevelLog) logger.Error($"Error {e.Message}");
        }
    }

    (bool requestedExit, string[] resultMassage) ApplyCommand(string move, bool thinkingMode)
    {
        bool indicateExit = false;
        string[] replayCommand = null;
        string[] commandData = move.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        switch (commandData[0])
        {
            /////
            // engine internal commands
            /////
            case "show": ShowBoard = !ShowBoard; break;
            case "win":
                if (WindowScreenPositionManager.GetWindowsCoordinates(out int consoleWinX, out int consoleWinY, out int consoleWinWidth, out int consoleWinHeight))
                    replayCommand = new string[] { $"# Console windows position x={consoleWinX} y={consoleWinY} width={consoleWinWidth} height={consoleWinHeight}" };
                else
                    replayCommand = new string[] { $"# Failed to get command windows coordinates" };
                break;
            case "setwin":
                if (commandData.Length == 5)
                {
                    Span<string> coordinates = new Span<string>(commandData, 1, commandData.Length - 1);
                    if (SeConsoleWindowPosition(coordinates))
                        replayCommand = new string[] { $"# Failed to set windows positions for coordinates {string.Join<string>(',', coordinates.ToArray())}" };
                    else
                        replayCommand = new string[] { $"# Console windows coordinates set to {string.Join<string>(',', coordinates.ToArray())}" };
                }
                else
                {
                    replayCommand = new string[] { $"# Failed to set command windows coordinates" };
                }
                break;
            /////
            /// Chess engine communication protocol
            /////
            case "new": engine.NewGame(); break;
            case "quit": indicateExit = true; break;
            case "xboard": NoOp(); break;
            case "protover": if (commandData.Length == 2 && string.Equals(commandData[1],"2")) replayCommand = new string[] { $"feature setboard=1", $"feature debug=1" }; break;
            case "edit": NoOp(); break;
            case "hint": NoOp(); break;
            case "bk": NoOp(); break;
            case "undo": engine.Undo(); break;
            case "remove": NoOp(); break;
            case "hard": engine.GameDifficulty = Engine.Difficulty.Hard; break;
            case "easy": engine.GameDifficulty = Engine.Difficulty.Easy; break;
            case "accepted": NoOp(); break;
            case "rejected": NoOp(); break;
            case "variant": NoOp(); break;
            case "random": NoOp(); break;
            case "force": NoOp(); break;
            case "go": NoOp(); break;
            case "playother":
                if (engine.WhoseMove == ChessPieceColor.White)
                    engine.HumanPlayer = ChessPieceColor.Black;
                else if (engine.WhoseMove == ChessPieceColor.Black)
                    engine.HumanPlayer = ChessPieceColor.White;
                break;
            case "white":
                engine.HumanPlayer = ChessPieceColor.Black;

                if (engine.WhoseMove != engine.HumanPlayer) replayCommand = MakeEngineMove(engine, thinkingMode);
                break;
            case "black":
                engine.HumanPlayer = ChessPieceColor.White;

                if (engine.WhoseMove != engine.HumanPlayer) replayCommand = MakeEngineMove(engine, thinkingMode);
                break;

            case "level":
                if (commandData.Length > 2) engine.TrySetTimeControl(commandData[1], commandData[2]);
                break;
            case "st": NoOp(); break;
            case "sd": NoOp(); break;
            case "time": NoOp(); break;
            case "otim": NoOp(); break;
            case "?": NoOp(); break;
            case "ping": if (commandData.Length > 1) replayCommand = new string[] { $"pong {commandData[1]}" }; break;
            case "result": NoOp(); break;
            case "post": runInThinkingMode = true; break; // turn on thinking mode
            case "nopost": runInThinkingMode = true; break; // turn off thinking mode
            case "setboard":
                if (commandData.Length > 1)
                {
                    string fen = string.Join(" ", commandData, 1, commandData.Length - 1);
                    engine.InitiateBoard(fen);
                }
                break;

            case "1/2-1/2": engine.NewGame(); break;
            case "0-1": engine.NewGame(); break;
            case "1-0": engine.NewGame(); break;

            default:
                if (move.Length == 4) replayCommand = ApplyMove(move.Substring(0, 2), move.Substring(2, 2), runInThinkingMode);
                break;
        }

        return (indicateExit, replayCommand);
    }

    string[] ApplyMove(string sourcePiecePosition, string destinationPiecePosition, bool thinkingMode)
    {
        string[] result = new string[2];

        byte srcCol;
        byte srcRow;
        byte dstRow;
        byte dstCol;

        try
        {
            srcCol = GetColumn(sourcePiecePosition);
            srcRow = GetRow(sourcePiecePosition);
            dstRow = GetRow(destinationPiecePosition);
            dstCol = GetColumn(destinationPiecePosition);
        }
        catch (Exception ex)
        {
            return new string[] { $"Error(internal engine error): {ex.Message}" };
        }

        if (!engine.IsValidMove(srcCol, srcRow, dstCol, dstRow)) return new string[] { $"Error (invalid move): {sourcePiecePosition}{destinationPiecePosition}" };

        engine.MovePiece(srcCol, srcRow, dstCol, dstRow);

        result = MakeEngineMove(engine, thinkingMode);


        if (engine.StaleMate)
        {
            result = new string[2];

            if (engine.InsufficientMaterial)
            {
                result = new string[] { "1/2-1/2 {Draw by insufficient material}" };
            }
            else if (engine.RepeatedMove)
            {
                result = new string[] { "1/2-1/2 {Draw by repetition}" };
            }
            else if (engine.FiftyMove)
            {
                result = new string[] { "1/2-1/2 {Draw by fifty move rule}" };
            }
            else
            {
                result = new string[] { "1/2-1/2 {Stalemate}" };
            }
            engine.NewGame();
        }
        else if (engine.GetWhiteMate())
        {
            result = new string[] { "0-1 {Black mates}" };
            engine.NewGame();
        }
        else if (engine.GetBlackMate())
        {
            result = new string[] { "1-0 {White mates}"  };
            engine.NewGame();
        }

        return result;
    }

    private void NoOp() { } // empty operation

    private string[] MakeEngineMove(Engine engine, bool thinkingOutput)
	{
        string[] result = new string[2];
		DateTime start = DateTime.Now;

		engine.AiPonderMove();

		MoveContent lastMove = engine.GetMoveHistory().ToArray()[0];

		string tmp = "";

		var sourceColumn = (byte)(lastMove.MovingPiecePrimary.SrcPosition % 8);
		var srcRow = (byte)(8 - (lastMove.MovingPiecePrimary.SrcPosition / 8));
		var destinationColumn = (byte)(lastMove.MovingPiecePrimary.DstPosition % 8);
		var destinationRow = (byte)(8 - (lastMove.MovingPiecePrimary.DstPosition / 8));

		tmp += GetPgnMove(lastMove.MovingPiecePrimary.PieceType);

		if (lastMove.MovingPiecePrimary.PieceType == ChessPieceType.Knight)
		{
			tmp += GetColumnFromInt(sourceColumn + 1);
			tmp += srcRow;
		}
		else if (lastMove.MovingPiecePrimary.PieceType == ChessPieceType.Rook)
		{
			tmp += GetColumnFromInt(sourceColumn + 1);
			tmp += srcRow;
		}
		else if (lastMove.MovingPiecePrimary.PieceType == ChessPieceType.Pawn)
		{
			if (sourceColumn != destinationColumn)
			{
				tmp += GetColumnFromInt(sourceColumn + 1);
			}
		}

		if (lastMove.TakenPiece.PieceType != ChessPieceType.None)
		{
			tmp += "x";
		}

		tmp += GetColumnFromInt(destinationColumn + 1);

		tmp += destinationRow;

		if (lastMove.PawnPromotedTo == ChessPieceType.Queen)
		{
			tmp += "=Q";
		}
		else if (lastMove.PawnPromotedTo == ChessPieceType.Rook)
		{
			tmp += "=R";
		}
		else if (lastMove.PawnPromotedTo == ChessPieceType.Knight)
		{
			tmp += "=K";
		}
		else if (lastMove.PawnPromotedTo == ChessPieceType.Bishop)
		{
			tmp += "=B";
		}

		DateTime end = DateTime.Now;

		TimeSpan ts = end - start;

		int score = engine.GetScore();

		if (score > 0) score = score / 10;

        result = new string[2];

        if (thinkingOutput) result[0] = $"{engine.PlyDepthReached} {score} {ts.Seconds * 100} {engine.NodesSearched} {engine.NodesQuiescence} {engine.PvLine}";

        result[(result[0] == null)? 0 : 1] = $"move {tmp}";

        return result;
	}

	public string GetColumnFromInt(int column)
	{
		string returnColumnt;

		switch (column)
		{
			case 1:
				returnColumnt = "a";
				break;
			case 2:
				returnColumnt = "b";
				break;
			case 3:
				returnColumnt = "c";
				break;
			case 4:
				returnColumnt = "d";
				break;
			case 5:
				returnColumnt = "e";
				break;
			case 6:
				returnColumnt = "f";
				break;
			case 7:
				returnColumnt = "g";
				break;
			case 8:
				returnColumnt = "h";
				break;
			default:
				returnColumnt = "Unknown";
				break;
		}

		return returnColumnt;
	}

	private string GetPgnMove(ChessPieceType pieceType)
	{
		string move = "";

		if (pieceType == ChessPieceType.Bishop)
		{
			move += "B";
		}
		else if (pieceType == ChessPieceType.King)
		{
			move += "K";
		}
		else if (pieceType == ChessPieceType.Knight)
		{
			move += "N";
		}
		else if (pieceType == ChessPieceType.Queen)
		{
			move += "Q";
		}
		else if (pieceType == ChessPieceType.Rook)
		{
			move += "R";
		}

		return move;
	}

	private byte GetRow(string move)
	{
		if (move != null)
		{
			if (move.Length == 2)
			{
				return (byte)(8 - int.Parse(move.Substring(1, 1).ToLower()));
			}
		}

		return 255;
	}

	private byte GetColumn(string move)
	{
		if (move != null)
		{
			if (move.Length == 2)
			{
				string col = move.Substring(0, 1).ToLower();

				switch (col)
				{
					case "a":
						{
							return 0;
						}
					case "b":
						{
							return 1;
						}
					case "c":
						{
							return 2;
						}
					case "d":
						{
							return 3;
						}
					case "e":
						{
							return 4;
						}
					case "f":
						{
							return 5;
						}
					case "g":
						{
							return 6;
						}
					case "h":
						{
							return 7;
						}
					default:
						return 255;
				}
			}
		}

		return 255;
	}

	

    private bool SeConsoleWindowPosition(Span<string> coordinates)
    {
        int consoleWinX = int.Parse(coordinates[0]);
        int consoleWinY = int.Parse(coordinates[1]);
        int consoleWinWidth = int.Parse(coordinates[2]);
        int consoleWinHeight = int.Parse(coordinates[3]);

        return WindowScreenPositionManager.SetWindowCoordinates(consoleWinX, consoleWinY, consoleWinWidth, consoleWinHeight);
    }
}
