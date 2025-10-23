# Exemplos de Uso da API de Impressão Avançada

## Endpoint para Impressão Simples
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

## Endpoint para Impressão Avançada
```http
POST /print/advanced
Content-Type: application/json

{
  "printerName": "Microsoft Print to PDF",
  "text": "Este é um exemplo de impressão com configurações avançadas!\n\nTexto centralizado com fonte maior e negrito.",
  "settings": {
    "fontName": "Times New Roman",
    "fontSize": 16,
    "fontStyle": "Regular",
    "alignment": "Center",
    "bold": true,
    "italic": false,
    "underline": false,
    "paperSize": "A4",
    "orientation": "Portrait",
    "margins": {
      "top": 100,
      "bottom": 100,
      "left": 100,
      "right": 100
    },
    "textColor": {
      "r": 0,
      "g": 0,
      "b": 0,
      "a": 255
    },
    "lineSpacing": 1.2,
    "fitToPage": true,
    "wrapText": true,
    "maxLinesPerPage": 50
  }
}
```

## Configurações Disponíveis

### Alinhamento de Texto
- `"Left"` - Alinhado à esquerda
- `"Center"` - Centralizado
- `"Right"` - Alinhado à direita
- `"Justify"` - Justificado

### Tamanhos de Papel
- `"A4"` - Padrão A4
- `"A3"` - Formato A3
- `"A5"` - Formato A5
- `"Letter"` - Carta americana
- `"Legal"` - Legal americano

### Orientação
- `"Portrait"` - Retrato
- `"Landscape"` - Paisagem

### Fontes Recomendadas
- `"Arial"`
- `"Times New Roman"`
- `"Courier New"`
- `"Calibri"`
- `"Verdana"`

## Exemplo de Impressão Centralizada
```http
POST /print/advanced
Content-Type: application/json

{
  "printerName": "Microsoft Print to PDF",
  "text": "TÍTULO CENTRALIZADO\n\nEste texto aparecerá no centro da página\nCom uma fonte maior e em negrito",
  "settings": {
    "fontName": "Arial",
    "fontSize": 20,
    "alignment": "Center",
    "bold": true,
    "paperSize": "A4",
    "orientation": "Portrait",
    "margins": {
      "top": 50,
      "bottom": 50,
      "left": 50,
      "right": 50
    },
    "lineSpacing": 1.5,
    "fitToPage": false
  }
}
```

## Exemplo de Impressão com Formatação Customizada
```http
POST /print/advanced
Content-Type: application/json

{
  "printerName": "Microsoft Print to PDF",
  "text": "RELATÓRIO IMPORTANTE\n\nEste é um exemplo de texto formatado com configurações personalizadas.\n\nO texto será impresso com fonte Times New Roman, tamanho 14, centralizado e com espaçamento entre linhas de 1.8.",
  "settings": {
    "fontName": "Times New Roman",
    "fontSize": 14,
    "alignment": "Center",
    "bold": false,
    "italic": true,
    "underline": false,
    "paperSize": "A4",
    "orientation": "Portrait",
    "margins": {
      "top": 80,
      "bottom": 80,
      "left": 60,
      "right": 60
    },
    "lineSpacing": 1.8,
    "fitToPage": true,
    "wrapText": true
  }
}
```

## Obter Informações Detalhadas das Impressoras
```http
GET /printers/detailed
```

## Obter Informações de uma Impressora Específica
```http
GET /printers/{nome-da-impressora}/info
```

## Status da Fila
```http
GET /queue/status
```
