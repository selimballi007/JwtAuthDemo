## Running the Project with Docker

This project is containerized using Docker and Docker Compose for easy setup and deployment. Below are the instructions and requirements specific to this project.

### Requirements
- **.NET Version:** The Dockerfile uses .NET 8.0 (`mcr.microsoft.com/dotnet/sdk:8.0` and `mcr.microsoft.com/dotnet/aspnet:8.0`).
- **Docker & Docker Compose:** Ensure you have Docker and Docker Compose installed on your system.

### Environment Variables
- No required environment variables are specified in the provided Dockerfile or `docker-compose.yml`. If you need to use environment variables, you can create a `.env` file and uncomment the `env_file` line in the compose file.

### Build and Run Instructions
1. **Build and start the application:**
   ```sh
   docker compose up --build
   ```
   This will build the Docker image and start the `csharp-app` service.

2. **Accessing the Application:**
   - The application will be available on [http://localhost:80](http://localhost:80).

### Ports
- **csharp-app:** Exposes port `80` (default ASP.NET Core port) to the host.

### Special Configuration
- The application runs as a non-root user inside the container for improved security.
- A custom Docker network (`app-network`) is defined for inter-service communication. If you add more services (e.g., a database), connect them to this network.
- If you need to add external dependencies (like a database), use the `depends_on` section in the compose file.

---

*Update this section if you add environment variables, external services, or change exposed ports in the future.*
