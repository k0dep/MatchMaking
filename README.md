# MatchMaking Service

This repository contains a matchmaking service built with ASP.NET Core, using Redis for data storage and Kafka for messaging.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop) and Docker Compose
- [HTTP request client](https://marketplace.visualstudio.com/items?itemName=vscode-restclient.restclient) for VS Code (optional, for .http files)

## Project Structure

- **MatchMaking.Service** - ASP.NET Core web service
- **MatchMaking.Worker** - Background processing worker
- **Tests** - Integration tests and E2E test flows

## Running the Solution

### Option 1: Complete Solution with Docker Compose

To start the entire solution including all services and their environment:

```bash
docker-compose up
```

This will start:
- Redis cache
- Kafka message broker
- MatchMaking.Service web API
- MatchMaking.Worker instances (2 replicas)

The service will be available at http://localhost:8080

### Option 2: Development Environment Only

If you want to run just the infrastructure components and develop/debug the services locally:

```bash
# Start only Redis and Kafka
docker-compose up redis kafka
```

Then run the services from your IDE or command line:

```bash
# Run the service
dotnet run --project MatchMaking.Service

# Run the worker (in a separate terminal)
dotnet run --project MatchMaking.Worker
```

### Option 3: Local Development

To run the applications without Docker:

1. Make sure Redis is available at localhost:6379
2. Make sure Kafka is available at localhost:9092
3. Start the service and worker projects:

```bash
dotnet run --project MatchMaking.Service
dotnet run --project MatchMaking.Worker
```

## Testing

### Running Tests

To run all tests in the solution:

```bash
dotnet test
```

Note: The solution contains integration tests and E2E test flows only.

### Using HTTP Files

The repository includes .http files for testing the API endpoints. If you're using Visual Studio or VS Code with the REST Client extension, you can execute these requests directly from the editor:

1. Open any .http file in the repository
2. Click on "Send Request" above each request
3. View the response in the editor

Example endpoints:

- POST to create a matchmaking request
- GET to check matchmaking status

## Configuration

The main configuration settings are stored in environment variables and appsettings.json files:

- Redis connection: `Redis__ConnectionString`
- Kafka connection: `Kafka__BootstrapServers`

## Troubleshooting

- If you encounter connection issues, ensure Redis and Kafka are running and accessible
- Check the logs of each service for error messages
- Verify that all health checks are passing