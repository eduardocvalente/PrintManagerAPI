# Exemplos de Uso da API

Os mesmos exemplos, executáveis direto do VS Code/Rider, estão em
[src/PrintManagerAPI/PrintManagerAPI.http](src/PrintManagerAPI/PrintManagerAPI.http).

## Impressão simples

```http
POST /print
Content-Type: application/json

{
  "printerName": "Microsoft Print to PDF",
  "text": "Olá, este é um teste de impressão!",
  "settings": {
    "fontSize": 12,
    "fontName": "Arial",
    "alignment": "Left"
  }
}
```

## Impressão com formatação completa

```http
POST /print
Content-Type: application/json

{
  "printerName": "Microsoft Print to PDF",
  "text": "Este é um exemplo com configurações avançadas!\n\nTexto centralizado, fonte maior, negrito.",
  "settings": {
    "fontName": "Times New Roman",
    "fontSize": 16,
    "alignment": "Center",
    "bold": true,
    "italic": false,
    "underline": false,
    "paperSize": "A4",
    "orientation": "Portrait",
    "margins": { "top": 100, "bottom": 100, "left": 100, "right": 100 },
    "textColor": { "r": 0, "g": 0, "b": 0 },
    "fitToPage": true
  }
}
```

> `POST /print/advanced` continua aceito como alias por compatibilidade,
> mas o payload é idêntico ao de `POST /print` — prefira `/print`.

## Configurações disponíveis

### Alinhamento (`alignment`)
`"Left"` | `"Center"` | `"Right"`

### Papel (`paperSize`)
`"A4"` | `"A3"` | `"A5"` | `"Letter"` | `"Legal"` | `"Thermal58mm"` | `"Thermal80mm"`

### Orientação (`orientation`)
`"Portrait"` | `"Landscape"`

### Demais campos

| Campo | Faixa | Padrão |
|---|---|---|
| `fontName` | até 64 caracteres | `"Arial"` |
| `fontSize` | 4–200 | 12 |
| `bold` / `italic` / `underline` | booleano | `false` |
| `margins.*` | 0–200 (centésimos de polegada) | 50 |
| `textColor.r/g/b` | 0–255 | 0 (preto) |
| `fitToPage` | booleano | `true` |

### Impressoras térmicas

Para `Thermal58mm`/`Thermal80mm`, o texto usa quebra de palavra e a redução
automática de fonte (`fitToPage`) respeita o tamanho mínimo 6.

## Erros

Todos os erros retornam Problem Details. Exemplo (validação):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "settings.fontSize": ["Tamanho da fonte deve estar entre 4 e 200."]
  }
}
```
