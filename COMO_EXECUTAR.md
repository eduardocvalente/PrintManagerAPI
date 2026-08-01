# Como Executar o PrintManagerAPI

## Pré-requisitos

- .NET 8 SDK ou superior
- Windows (dependência do spooler de impressão / `System.Drawing.Printing`)
- Pelo menos uma impressora instalada (ex.: "Microsoft Print to PDF")

## Executando

```bash
dotnet run --project src/PrintManagerAPI
```

- API: `http://localhost:5094`
- Swagger (somente em Development): `http://localhost:5094/swagger`

Para portas personalizadas:

```bash
dotnet run --project src/PrintManagerAPI --urls "http://localhost:5000"
```

> Atenção: o `dotnet run` usa o perfil do `launchSettings.json`, que força
> `ASPNETCORE_ENVIRONMENT=Development`. Para simular produção use
> `dotnet run --no-launch-profile` (Swagger desabilitado, erros sem stack trace).

## Executando os testes

```bash
dotnet test
```

## Fluxo típico

### 1. Listar impressoras

```bash
curl http://localhost:5094/printers
```

```json
{ "success": true, "count": 2, "printers": ["OneNote (Desktop)", "Microsoft Print to PDF"] }
```

### 2. Enviar um job

```bash
curl -X POST http://localhost:5094/print -H "Content-Type: application/json" -d "{\"printerName\":\"Microsoft Print to PDF\",\"text\":\"Olá, fila!\"}"
```

Resposta **202 Accepted**:

```json
{
  "success": true,
  "message": "Trabalho de impressão adicionado à fila com sucesso",
  "jobId": "123e4567-e89b-12d3-a456-426614174000",
  "printerName": "Microsoft Print to PDF",
  "statusUrl": "/print/123e4567-e89b-12d3-a456-426614174000",
  "queueStatus": { "pendingJobs": 1, "isProcessing": false, "currentJobId": null }
}
```

### 3. Acompanhar o job

```bash
curl http://localhost:5094/print/123e4567-e89b-12d3-a456-426614174000
```

```json
{
  "success": true,
  "job": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "printerName": "Microsoft Print to PDF",
    "state": "Completed",
    "enqueuedAt": "2026-07-30T18:00:00Z",
    "startedAt": "2026-07-30T18:00:01Z",
    "finishedAt": "2026-07-30T18:00:03Z",
    "error": null
  }
}
```

### 4. Status da fila e saúde

```bash
curl http://localhost:5094/queue/status
curl http://localhost:5094/health
```

## Respostas de erro (Problem Details — RFC 9457)

| Situação | Status |
|---|---|
| Validação (texto vazio, fonte fora de 4–200, RGB inválido…) | 400 |
| Impressora ou job inexistente | 404 |
| Mais de 30 submissões/minuto | 429 |
| Fila cheia (`MaxQueueLength`) | 503 |
| `PrintQueue:ApiKey` configurada e header `X-Api-Key` ausente/errado | 401 |

## Habilitando autenticação por chave de API

```powershell
$env:PrintQueue__ApiKey = "minha-chave"
dotnet run --project src/PrintManagerAPI
```

Todas as rotas (exceto `/health*`) passam a exigir o header `X-Api-Key: minha-chave`.
