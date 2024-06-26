using Azure.Storage.Queues;
using System.Text;

Console.WriteLine("入力された数字の数だけキューにメッセージを追加します: ");
var input = Console.ReadLine();

if (int.TryParse(input, out var count))
{
    var client = new QueueClient("UseDevelopmentStorage=true", "myqueue-items");
    for (int i = 0; i < count; i++)
    {
       await client.SendMessageAsync(
           Convert.ToBase64String(Encoding.UTF8.GetBytes($"***Message {i}***")));
    }
}
else
{
    Console.WriteLine("数字を入力してください。");
}