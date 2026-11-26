# Brasa — mesa virtual de RPG

Uma mesa virtual para organizar campanhas, movimentar tokens, importar mapas, manter fichas de personagens e rolar dados.

## Requisitos

- .NET 8 SDK
- Python 3 para servir a interface localmente

## Rodar localmente

1. Inicie a API:

   ```bash
   cd backend/RpgTabletop.Api
   dotnet run --urls http://localhost:5080
   ```

2. Em outro terminal, sirva o front-end:

   ```bash
   cd frontend
   python3 -m http.server 5173
   ```

3. Abra `http://localhost:5173`.

A primeira execução cria uma campanha de demonstração. O estado fica em `backend/RpgTabletop.Api/data/campaign.json`.

## O que já funciona

- Mesa responsiva com mapa inicial, grade alternável e tokens arrastáveis.
- Importação de mapas em imagem, até 7 MB.
- Criação, edição, exclusão e importação de fichas JSON.
- Dados d4, d6, d8, d10, d12, d20 e d100, com rolagem segura no servidor.
- Iniciativa rápida para os personagens da mesa.
- API REST .NET com validações e salvamento local em JSON.

## Importar uma ficha

Escolha um arquivo JSON com pelo menos um nome. Campos reconhecidos incluem `name`, `role`, `species`, `level`, `hitPoints`, `maxHitPoints`, `armorClass`, `notes` e `attributes`. Também são aceitos nomes comuns em português e formatos simples de D&D, como `nome`, `classe`, `raca`, `nivel`, `hp`, `hpMax`, `ac` e `atributos`.