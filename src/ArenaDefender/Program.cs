using ArenaDefender;

// Entry point. Any exception that escapes the game is logged to a file and shown in the
// console instead of disappearing silently, so a crash can be diagnosed afterwards.
try
{
    using var game = new ArenaDefenderGame();
    game.Run();
}
catch (Exception ex)
{
    GameLog.Error("Fatal error – the game has to close.", ex);
    Console.Error.WriteLine("Arena Defender crashed: " + ex.Message);
    Console.Error.WriteLine("Details were written to: " + GameLog.LogFilePath);
    Environment.ExitCode = 1;
}
