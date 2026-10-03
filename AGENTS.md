# forzion.tech-game-aoe

RTS 3D sobre a história do Brasil. Simulação determinística em C# puro, renderizada com Godot 4 .NET.

## Fluxo de trabalho

- **Specs**: escritas com o skill `/to-spec` e publicadas como issue no GitHub. Quando outro fluxo (por exemplo `superpowers:brainstorming`) chegar na etapa de escrever a spec, essa etapa é feita pelo `/to-spec`.
- **Implementação**: feita com `/implement-spec` quando a spec tem tickets, ou com `/implement` para um trabalho único sem grafo de tickets.
- **Tickets**: issues do GitHub com o rótulo `ready-for-agent`; dependências escritas no corpo como `Blocked by #N`.

## Convenções

- **Linguagem do domínio**: `GLOSSARY.md` define os termos e o nome em inglês de cada um. Código, comentários e commits usam o nome em inglês; documentação e textos do jogo usam o termo em português.
- **Decisões de arquitetura**: `docs/adr/`. Ler antes de mexer no núcleo de simulação ou na fronteira dele com o Godot.
- **Núcleo de simulação**: usa só `Fix64`, o gerador aleatório da partida e iteração em ordem de ID. Tipos do Godot, `float`/`double`, relógio do sistema e threads ficam na camada de apresentação.
