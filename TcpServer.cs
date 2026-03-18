using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TelemetryServer;

public class TcpServer
{
    private readonly int _port;
    private TcpListener? _listener;

    // Constructor — like setting up the walkie-talkie to a specific channel
    public TcpServer(int port)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        // Bind to ALL network interfaces on our chosen port
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        Console.WriteLine($"Server listening on port {_port}...");

        // Wait for ONE device to connect
        TcpClient client = await _listener.AcceptTcpClientAsync();

        Console.WriteLine("A device connected!");

        // Get the data stream — like opening your ears on the walkie-talkie
        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024]; // a bucket to hold incoming bytes

        // Keep reading until the device disconnects
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer);

            // bytesRead = 0 means the device hung up
            if (bytesRead == 0) break;

            // Convert the bytes into a readable string
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine($"Received: {message.Trim()}");
        }

        Console.WriteLine("Device disconnected.");
        client.Dispose();
    }
}