namespace Template.GrpcServer.TestClient.Commands;

public static class Progress
{
    // 同じ行を書き換えて進捗を表示する。標準出力がリダイレクトされているときは出さない
    public static void Write(long current, long total)
    {
        if (Console.IsOutputRedirected || (total <= 0))
        {
            return;
        }

        Console.Write($"\r{current * 100 / total,3}% {current}/{total}");
        if (current >= total)
        {
            Console.WriteLine();
        }
    }
}
