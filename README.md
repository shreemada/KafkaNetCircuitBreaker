### **Kafka Circuit Breaker with .NET 8 and Polly**

---

#### **Overview**
This project demonstrates a resilient Kafka consumer-producer system using **.NET 8** and **Polly** to implement the Circuit Breaker pattern. It ensures fault tolerance when interacting with Kafka brokers during transient failures (e.g., broker downtime). The Docker Compose setup includes Kafka and Zookeeper with guaranteed startup reliability.

---

### **Features**
- **Circuit Breaker**: Trips after 3 consecutive failures, pauses consumption for 30 seconds, and auto-retries.
- **Resilient Kafka Client**: Uses `Confluent.Kafka` with Polly integration.
- **Dockerized Kafka**: Preconfigured Kafka and Zookeeper containers with health checks and data persistence.
- **Consumer Pausing**: Automatically pauses/resumes Kafka consumption based on circuit state.

---

### **Prerequisites**
- Docker Desktop (v20.10+)
- .NET SDK 8.0+
- IDE (VS Code, Visual Studio, or Rider)

---

### **Getting Started**

#### **1. Clone the Repository**
```bash
git clone https://github.com/yourusername/kafka-circuit-breaker-demo.git
cd kafka-circuit-breaker-demo
```

#### **2. Start Kafka and Zookeeper**
```bash
docker-compose up -d
```
**Verify Containers**:
```bash
docker ps --format "table {{.Names}}\t{{.Status}}"
```
Expected Output:
```
NAMES       STATUS
zookeeper   Up (healthy)
kafka       Up (healthy)
```

#### **3. Build and Run the .NET Application**
```bash
dotnet run
```

---

### **Configuration**
#### **appsettings.json**
```json
{
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "BreakDurationSeconds": 30
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "SourceTopic": "source-topic",
    "TargetTopic": "target-topic",
    "ConsumerGroup": "circuit-breaker-group"
  }
}
```

#### **docker-compose.yml**
- Preconfigured Kafka and Zookeeper with health checks.
- Uses Docker volumes for data persistence.

---

### **Testing the Circuit Breaker**

#### **1. Produce Test Messages**
```bash
docker exec -it kafka kafka-console-producer \
  --broker-list kafka:9092 \
  --topic source-topic
```

#### **2. Consume Messages from Target Topic**
```bash
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server kafka:9092 \
  --topic target-topic \
  --from-beginning
```

#### **3. Simulate Kafka Downtime**
1. **Stop Kafka**:
   ```bash
   docker stop kafka
   ```
2. **Observe Circuit Breaker Tripping**:
   ```
   Circuit OPEN: Broker not available. Breaking for 30s.
   Consumer PAUSED.
   ```
3. **Restart Kafka**:
   ```bash
   docker start kafka
   ```
4. **Verify Auto-Recovery**:
   ```
   Circuit HALF-OPEN.
   Forwarded: Test Message
   Circuit CLOSED.
   Consumer RESUMED.
   ```

---

### **Troubleshooting**
| Issue                          | Solution                                                                 |
|--------------------------------|--------------------------------------------------------------------------|
| Containers not starting        | Run `docker-compose down -v && docker-compose up -d`                     |
| Zookeeper/Kafka health checks  | Check logs: `docker-compose logs zookeeper` or `docker-compose logs kafka` |
| Port conflicts (2181/9092)     | Use `lsof -i :2181` or `lsof -i :9092` to find and kill blocking processes |
| Circuit breaker not triggering | Verify `appsettings.json` thresholds and Kafka broker connectivity       |

---

### **Project Structure**
```
.
├── KafkaCircuitBreaker/
│   ├── Program.cs             # Circuit breaker and Kafka logic
│   ├── appsettings.json       # Configuration
│   └── KafkaCircuitBreaker.csproj
├── docker-compose.yml         # Kafka + Zookeeper setup
└── README.md
```

---
