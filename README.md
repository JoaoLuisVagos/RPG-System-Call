# Brasa — mesa virtual de RPG

Uma mesa virtual para organizar campanhas, movimentar tokens, importar mapas, manter fichas de personagens e rolar dados.

## Requisitos

- .NET 8 SDK
- Node.js 22 ou mais recente para instalar e servir a interface

## Rodar localmente

1. Inicie a API:

   ```bash
   cd backend/RpgTabletop.Api
   dotnet run --urls http://localhost:5080
   ```

2. Em outro terminal, sirva o front-end:

   ```bash
   cd frontend
   npm install
   npm run dev
   ```

3. Abra `http://localhost:5173`.

A primeira execução cria uma campanha de demonstração. O estado fica em `backend/RpgTabletop.Api/data/campaign.json`.

## Organização

```text
backend/RpgTabletop.Api/
  Endpoints/    Rotas separadas para campanhas, mapas, personagens, tokens, dados, chat e iniciativa
  Validation/   Validações das fichas de personagem
  GameStore.cs  Persistência local da campanha
  Models.cs     Modelos da API
  SessionHub.cs Eventos de colaboração em tempo real
  Program.cs    Configuração e inicialização
frontend/
  app.js
  index.html
  package.json
  styles.css
```

## O que já funciona

- Mesa responsiva com mapa inicial, grade alternável e tokens arrastáveis.
- Importação de mapas em imagem, até 7 MB.
- Criação, edição, exclusão e importação de fichas JSON.
- Dados d4, d6, d8, d10, d12, d20 e d100, com animação física 3D pela biblioteca Dice Box.
- Resultados de dados e mensagens compartilhados na sessão em tempo real.
- Chat da sessão ao vivo via SignalR, com histórico persistido.
- Neblina de guerra editável com pincel de revelar/ocultar áreas.
- Iniciativa com avanço de turno e contagem de rodadas.
- Atualizações em tempo real de mapas, tokens e fichas entre pessoas conectadas.
- API REST .NET com validações e salvamento local em JSON.
- Front-end Vite com proxy local para API e SignalR.

O Dice Box e o tema padrão usam licença MIT; as licenças e créditos estão em `frontend/public/assets/dice-box/`.
Esta etapa não inclui contas ou permissões de mestre, vídeo/voz, compêndio de regras ou múltiplas campanhas independentes.

## Importar uma ficha

Escolha um arquivo JSON com pelo menos um nome. Campos reconhecidos incluem `name`, `role`, `species`, `level`, `hitPoints`, `maxHitPoints`, `armorClass`, `notes` e `attributes`. Também são aceitos nomes comuns em português e formatos simples de D&D, como `nome`, `classe`, `raca`, `nivel`, `hp`, `hpMax`, `ac` e `atributos`.