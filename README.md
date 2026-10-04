# forzion.tech-game-aoe

[Português](#português) · [English](#english)

## Português

Jogo de estratégia em tempo real (RTS) em 3D sobre a história do Brasil. O jogador coleta recursos, ergue construções, treina um exército, avança de Era e vence ao destruir o Centro inimigo.

É um projeto de portfólio de engenharia de software em C#. O foco está na arquitetura: toda a regra do jogo vive numa simulação determinística, escrita em C# puro e separada da engine.

> **Estado:** em desenvolvimento. O jogo ainda não é jogável: ele abre o mapa inicial com formas simples no lugar da arte, e dá para selecionar os Aldeões e mandá-los andar e coletar. O primeiro marco jogável está descrito na [spec do vertical slice](https://github.com/arthuurw/forzion.tech-game-aoe/issues/1).

### Arquitetura

- **Núcleo de simulação** (`src/Forzion.Simulation`): biblioteca .NET sem nenhuma referência ao Godot. Contém economia, construção, produção, combate, IA e movimento.
- **Apresentação** (`game/` e `src/Forzion.Presentation`): projeto Godot 4 .NET que mostra o estado em 3D e transforma o input do jogador em comandos. Avança a simulação a 20 ticks por segundo e interpola as posições entre os ticks. A parte que não depende da engine (relógio de ticks, interpolação, o que um clique ou um retângulo seleciona, que comando o botão direito dá conforme o alvo, que barras de vida mostrar e que aviso mostrar para uma ordem recusada) fica numa biblioteca C# pura testada sem o Godot.
- **Determinismo**: ticks fixos, aritmética de ponto fixo e gerador aleatório com seed. A mesma seed com os mesmos comandos produz o mesmo estado em Windows, Linux e macOS.

As decisões e os motivos estão em [`docs/adr/`](docs/adr/). O vocabulário do domínio está em [`GLOSSARY.md`](GLOSSARY.md).

### Como compilar e testar

Requer o [.NET SDK 8](https://dotnet.microsoft.com/download) ou mais novo.

```bash
dotnet build
dotnet test
```

A integração contínua roda os dois comandos em Windows, Linux e macOS. O `dotnet build` também compila o projeto Godot; o SDK do Godot vem do NuGet, sem precisar da engine instalada.

### Como rodar o jogo

Requer o [Godot 4.7 .NET](https://godotengine.org/download). Compile e abra o projeto pela linha de comando:

```bash
dotnet build game/Forzion.Game.csproj
godot --path game
```

`godot` é o executável do Godot .NET. Também dá para abrir `game/project.godot` no editor e apertar Play.

Câmera: WASD, setas ou mouse na borda da tela deslocam; roda do mouse ou Page Up e Page Down aproximam e afastam.

Seleção e ordens: clique com o botão esquerdo seleciona uma unidade ou construção sua; arrastar com o botão esquerdo seleciona suas unidades dentro do retângulo; clique em terreno vazio limpa a seleção. O botão direito manda as unidades selecionadas coletar numa Fonte de Recurso, atacar uma unidade ou construção inimiga, construir uma Obra sua ou andar até o ponto clicado; Aldeões sem soldado junto andam até o inimigo em vez de atacar. Unidades mortas saem da seleção. Unidades e construções selecionadas ou feridas mostram uma barra de vida. Uma ordem recusada aparece como aviso no topo da tela.

Os textos do jogo ficam em `game/translations/pt_BR.po` (gettext); outro idioma entra como outro arquivo `.po` com as mesmas chaves.

### Estrutura do repositório

| Caminho | Conteúdo |
|---|---|
| `src/Forzion.Simulation` | Núcleo de simulação |
| `src/Forzion.Presentation` | Lógica de apresentação sem engine: relógio de ticks, interpolação, seleção, ordens e avisos |
| `game` | Projeto Godot: cenas, câmera, renderização, input e textos (`translations`) |
| `tests/Forzion.Simulation.Tests` | Testes do núcleo (xUnit e FsCheck) |
| `tests/Forzion.Presentation.Tests` | Testes da lógica de apresentação (xUnit) |
| `docs/adr` | Decisões de arquitetura |
| `GLOSSARY.md` | Glossário do domínio |
| `CODING_STANDARDS.md` | Padrões de código |
| `AGENTS.md` | Regras de trabalho para agentes de código |

### Plano

O trabalho está dividido em tickets ligados à spec. Acompanhe pelas [issues](https://github.com/arthuurw/forzion.tech-game-aoe/issues).

Depois do vertical slice vêm, nesta ordem: névoa de guerra, Facção Tupi, colisão e formações, Eras III e IV, Cenários e Campanha, e multiplayer.

### Licença e aviso

Código sob a [licença MIT](LICENSE).

Este projeto não tem vínculo com Age of Empires nem com a Microsoft, e não usa nome, arte, som ou ícones desse jogo. Apenas as mecânicas do gênero servem de referência.

## English

A 3D real-time strategy (RTS) game about the history of Brazil. The player gathers resources, raises buildings, trains an army, advances through Ages and wins by destroying the enemy Town Center.

This is a software engineering portfolio project in C#. The focus is the architecture: every game rule lives in a deterministic simulation written in plain C#, separate from the engine.

> **Status:** in development. The game is not playable yet: it opens the starting map with simple shapes in place of the art, and the Villagers can be selected and sent to walk and gather. The first playable milestone is described in the [vertical slice spec](https://github.com/arthuurw/forzion.tech-game-aoe/issues/1) (in Portuguese).

### Architecture

- **Simulation core** (`src/Forzion.Simulation`): a .NET library with no reference to Godot. It holds economy, construction, production, combat, AI and movement.
- **Presentation** (`game/` and `src/Forzion.Presentation`): a Godot 4 .NET project that shows the state in 3D and turns player input into commands. It advances the simulation at 20 ticks per second and interpolates positions between ticks. The part that needs no engine (tick clock, interpolation, what a click or a box selects, which command a right-click gives on each target, which hit point bars to show and which notice a refused order shows) lives in a plain C# library tested without Godot.
- **Determinism**: fixed ticks, fixed-point arithmetic and a seeded random generator. The same seed with the same commands produces the same state on Windows, Linux and macOS.

The decisions and their reasons are in [`docs/adr/`](docs/adr/). The domain vocabulary is in [`GLOSSARY.md`](GLOSSARY.md). Both are written in Portuguese; code, comments and commits are in English.

### Build and test

Requires the [.NET SDK 8](https://dotnet.microsoft.com/download) or newer.

```bash
dotnet build
dotnet test
```

Continuous integration runs both commands on Windows, Linux and macOS. `dotnet build` also compiles the Godot project; the Godot SDK comes from NuGet, so the engine need not be installed.

### Run the game

Requires [Godot 4.7 .NET](https://godotengine.org/download). Build and open the project from the command line:

```bash
dotnet build game/Forzion.Game.csproj
godot --path game
```

`godot` is the Godot .NET executable. You can also open `game/project.godot` in the editor and press Play.

Camera: WASD, the arrow keys or the mouse at the screen edge pan; the mouse wheel or Page Up and Page Down zoom in and out.

Selection and orders: a left click selects one of your units or buildings; dragging with the left button selects your units inside the box; clicking bare ground clears the selection. The right button sends the selected units to gather from a resource source, attack an enemy unit or building, build one of your construction sites or walk to the clicked point; Villagers with no soldier among them walk up to an enemy instead of attacking. Units that die leave the selection. Selected or wounded units and buildings show a hit point bar. A refused order shows as a notice at the top of the screen.

The game texts live in `game/translations/pt_BR.po` (gettext); another language comes in as another `.po` file with the same keys.

### Repository layout

| Path | Contents |
|---|---|
| `src/Forzion.Simulation` | Simulation core |
| `src/Forzion.Presentation` | Engine-free presentation logic: tick clock, interpolation, selection, orders and notices |
| `game` | Godot project: scenes, camera, rendering, input and texts (`translations`) |
| `tests/Forzion.Simulation.Tests` | Core tests (xUnit and FsCheck) |
| `tests/Forzion.Presentation.Tests` | Presentation logic tests (xUnit) |
| `docs/adr` | Architecture decision records |
| `GLOSSARY.md` | Domain glossary |
| `CODING_STANDARDS.md` | Coding standards (in Portuguese) |
| `AGENTS.md` | Working rules for coding agents |

### Roadmap

The work is split into tickets linked to the spec. Follow it in the [issues](https://github.com/arthuurw/forzion.tech-game-aoe/issues).

After the vertical slice come, in this order: fog of war, the Tupi Faction, unit collision and formations, Ages III and IV, Scenarios and Campaign, and multiplayer.

### License and disclaimer

Code under the [MIT license](LICENSE).

This project is not affiliated with Age of Empires or Microsoft, and uses no name, art, sound or icons from that game. Only the genre's mechanics serve as reference.
