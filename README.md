# PrintManagerAPI — Fila de Impressão

API REST em .NET 8 (Minimal API) que gerencia uma fila de impressão no Windows: lista as impressoras instaladas, enfileira trabalhos de impressão de texto e processa **um job por vez, em ordem FIFO**, com rastreamento de status por job.

> **Dependência de plataforma:** o projeto usa `System.Drawing.Printing` e o spooler do Windows. Ele roda **somente em Windows** — por isso não há Dockerfile: containers Linux não têm acesso ao subsistema de impressão. Veja [docs/AUDITORIA-TECNICA.md](docs/AUDITORIA-TECNICA.md) para a análise completa dessa decisão.

## Arquitetura

```
src/PrintManagerAPI/
├── API/
│   ├── Interfaces/          # IPrintService, IPrintJobProcessor, IPrinterDiscoveryService
│   ├── Models/              # PrintJob, PrintRequest, PrintSettings, status de job/fila
│   ├── Services/            # PrintService (fila), PrintQueueWorker, PrintJobProcessor,
│   │                        #   PrinterDiscoveryService
│   └── Validation/          # PrintRequestValidator (validação na borda da API)
├── Configuration/           # Registro de serviços, endpoints, opções, health check,
│                            #   middleware de API key
└── Program.cs
tests/PrintManagerAPI.Tests/ # xUnit: semântica da fila, concorrência, validação
```

**Como a fila funciona:** o `PrintService` publica jobs em um `System.Threading.Channels.Channel` **limitado** (backpressure — fila cheia responde 503). O `PrintQueueWorker` (`BackgroundService`) é o **único consumidor**, o que garante FIFO e no máximo um job em impressão por vez sem flags de controle manuais. Cada job tem status consultável (`Queued → Printing → Completed | Failed`), timeout configurável e desligamento gracioso.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/printers` | Lista as impressoras instaladas |
| GET | `/printers/detailed` | Impressoras com capacidades (duplex, cor, papéis) |
| GET | `/printers/{name}/info` | Detalhes de uma impressora |
| POST | `/print` | Enfileira um job — responde **202 Accepted** com `jobId` e `statusUrl` |
| GET | `/print/{jobId}` | Status do job (`Queued`, `Printing`, `Completed`, `Failed`) |
| GET | `/queue/status` | Jobs pendentes e job em processamento |
| GET | `/health` | Readiness (inclui verificação do spooler) |
| GET | `/health/live` | Liveness (apenas o processo) |
| GET | `/swagger` | Documentação interativa (somente em Development) |

Erros seguem **Problem Details (RFC 9457)**. Enums são aceitos como string (`"alignment": "Center"`, `"paperSize": "A4"`).

## Como executar

Pré-requisitos: .NET 8+ SDK, Windows com pelo menos uma impressora instalada.

```bash
dotnet run --project src/PrintManagerAPI
```

Swagger em `http://localhost:5094/swagger`. Exemplos prontos em [PrintManagerAPI.http](src/PrintManagerAPI/PrintManagerAPI.http).

### Testes

```bash
dotnet test
```

A suíte cobre a semântica crítica da fila: ordem FIFO, ausência de perda de jobs sob enfileiramento concorrente, processamento estritamente serial, timeout, falhas e validação de entrada.

## Configuração

Seção `PrintQueue` do `appsettings.json` (validada na inicialização):

| Chave | Padrão | Descrição |
|---|---|---|
| `MaxQueueLength` | 100 | Capacidade da fila; excedente recebe 503 |
| `PrintJobTimeoutSeconds` | 300 | Timeout por job; excedido marca o job como `Failed` |
| `MaxTextLength` | 20000 | Tamanho máximo do texto por requisição |
| `MaxTrackedJobs` | 1000 | Jobs finalizados retidos para consulta de status |
| `ApiKey` | *(vazio)* | Se definida, exige o header `X-Api-Key` em todas as rotas (exceto health) |

A chave de API deve ser definida por variável de ambiente, nunca em arquivo versionado:

```bash
set PrintQueue__ApiKey=sua-chave-secreta
```

Submissões de impressão têm rate limit de 30 requisições/minuto (429 quando excedido).

## Limitações conhecidas

- **Fila em memória:** jobs pendentes são perdidos se o processo encerrar (persistência em SQLite é evolução planejada — ver auditoria).
- **Uma página por job:** texto que exceda a área útil da página não gera páginas adicionais.
- **Status "online" de impressora:** a API do spooler não expõe o estado físico real; `IsValid` reflete a validade do driver.
- **Instância única:** o estado da fila vive no processo; não há suporte a scale-out horizontal (por design, para o escopo atual).

## Qualidade

- Build com `TreatWarningsAsErrors` + analisadores .NET (`latest-recommended`).
- CI no GitHub Actions (build + testes + auditoria de dependências) — [.github/workflows/ci.yml](.github/workflows/ci.yml).
- Dependabot para NuGet e Actions.
- Auditoria técnica completa com achados, severidades e roadmap: [docs/AUDITORIA-TECNICA.md](docs/AUDITORIA-TECNICA.md).

## Licença e propriedade intelectual

**© 2026 Eduardo Costa Valente. Todos os direitos reservados.**

Este projeto é de propriedade exclusiva do autor e está publicado **apenas para
avaliação técnica** (recrutamento e portfólio). **Não é software livre nem open
source**: copiar, modificar, redistribuir ou reutilizar este código — no todo ou
em parte — **exige autorização prévia e por escrito do autor**. Apresentar este
trabalho como de autoria própria é expressamente proibido.

Para solicitar permissão de uso: [github.com/eduardocvalente](https://github.com/eduardocvalente).
Detalhes completos em [LICENSE](LICENSE).
