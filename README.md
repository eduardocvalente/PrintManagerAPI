# PrintQueueAPI – Fila de Impressão em .NET 8

API REST desenvolvida em **C# .NET 8 (Minimal API)** para gerenciar filas de impressão no Windows, com **injeção de dependência**, **interfaces** e **configuração por lambdas**.

---

## Funcionalidades
- Listagem de impressoras instaladas  
- Fila de impressão FIFO (First In, First Out)  
- Controle de concorrência (1 job por vez)  
- Logs estruturados e detalhados  
- Documentação Swagger  
- Health check e monitoramento  

---

## Interfaces e Serviços
- `IPrintService` – Gerencia a fila de impressão  
- `IPrintJobProcessor` – Processa jobs individuais  
- `IPrinterDiscoveryService` – Descobre e valida impressoras  

---

## Endpoints

| Método | Rota | Descrição |
|--------|------|------------|
| GET | `/printers` | Lista impressoras |
| GET | `/printers/{name}/info` | Detalhes da impressora |
| POST | `/print` | Envia job de impressão |
| GET | `/queue/status` | Status da fila |
| GET | `/health` | Verifica saúde da aplicação |
| GET | `/swagger` | Documentação interativa |

---

## Arquitetura e Padrões
- **Dependency Injection** – Serviços registrados via container  
- **Configuration Pattern (Lambdas)** – Configuração fluente e tipada  
- **Repository/Service Pattern** – Separação clara de responsabilidades  
- **Builder Pattern** – Organização fluente dos endpoints  

---

---

## Execução
- **Requisitos:** .NET 8 SDK, Windows e impressoras instaladas  
- **API:** `http://localhost:5000`  
- **Swagger:** `https://localhost:7000/swagger`

---

## Benefícios
- Testável e modular  
- Manutenível e extensível  
- Configuração simples e flexível  
- Monitoramento e logs integrados  
- Performance otimizada (execução assíncrona)
