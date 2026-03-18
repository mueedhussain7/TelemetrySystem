using TelemetryServer;

var server = new TcpServer(port: 9000);
await server.StartAsync();