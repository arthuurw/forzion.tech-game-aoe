# forzion.tech-game-aoe

[Português](#português) · [English](#english)

## Português

Jogo de estratégia em tempo real (RTS) em 3D sobre a história do Brasil. O jogador coleta recursos, ergue construções, treina um exército, avança de Era e vence ao destruir o Centro inimigo.

É um projeto de portfólio de engenharia de software em C#. O foco está na arquitetura: toda a regra do jogo vive numa simulação determinística, escrita em C# puro e separada da engine.

> **Estado:** em desenvolvimento. O jogo ainda não é jogável. O que existe hoje é a estrutura da solução, os testes e a integração contínua. O primeiro marco jogável está descrito na [spec do vertical slice](https://github.com/arthuurw/forzion.tech-game-aoe/issues/1).

### Arquitetura

- **Núcleo de simulação** (`src/Forzion.Simulation`): biblioteca .NET sem nenhuma referência ao Godot. Contém economia, construção, produção, combate, IA e movimento.
- **Apresentação**: projeto Godot 4 .NET que mostra o estado em 3D e transforma o input do jogador em comandos. Ainda não está no repositório.
- **Determinismo**: ticks fixos, aritmética de ponto fixo e gerador aleatório com seed. A mesma seed com os mesmos comandos produz o mesmo estado em Windows, Linux e macOS.

As decisões e os motivos estão em [`docs/adr/`](docs/adr/). O vocabulário do domínio está em [`GLOSSARY.md`](GLOSSARY.md).

### Como compilar e testar

Requer o [.NET SDK 8](https://dotnet.microsoft.com/download) ou mais novo.

```bash
dotnet build
dotnet test
```

A integração contínua roda os dois comandos em Windows, Linux e macOS.

### Estrutura do repositório

| Caminho | Conteúdo |
|---|---|
| `src/Forzion.Simulation` | Núcleo de simulação |
| `tests/Forzion.Simulation.Tests` | Testes do núcleo (xUnit e FsCheck) |
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

> **Status:** in development. The game is not playable yet. What exists today is the solution structure, the tests and continuous integration. The first playable milestone is described in the [vertical slice spec](https://github.com/arthuurw/forzion.tech-game-aoe/issues/1) (in Portuguese).

### Architecture

- **Simulation core** (`src/Forzion.Simulation`): a .NET library with no reference to Godot. It holds economy, construction, production, combat, AI and movement.
- **Presentation**: a Godot 4 .NET project that shows the state in 3D and turns player input into commands. It is not in the repository yet.
- **Determinism**: fixed ticks, fixed-point arithmetic and a seeded random generator. The same seed with the same commands produces the same state on Windows, Linux and macOS.

The decisions and their reasons are in [`docs/adr/`](docs/adr/). The domain vocabulary is in [`GLOSSARY.md`](GLOSSARY.md). Both are written in Portuguese; code, comments and commits are in English.

### Build and test

Requires the [.NET SDK 8](https://dotnet.microsoft.com/download) or newer.

```bash
dotnet build
dotnet test
```

Continuous integration runs both commands on Windows, Linux and macOS.

### Repository layout

| Path | Contents |
|---|---|
| `src/Forzion.Simulation` | Simulation core |
| `tests/Forzion.Simulation.Tests` | Core tests (xUnit and FsCheck) |
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
