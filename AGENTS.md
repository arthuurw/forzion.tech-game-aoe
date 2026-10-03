# forzion.tech-game-aoe

RTS 3D sobre a história do Brasil. Simulação determinística em C# puro, renderizada com Godot 4 .NET.

## Fluxo de trabalho

- **Specs**: escritas com o skill `/to-spec` e publicadas como issue no GitHub. Quando outro fluxo (por exemplo `superpowers:brainstorming`) chegar na etapa de escrever a spec, essa etapa é feita pelo `/to-spec`.
- **Implementação**: feita com `/implement-spec` quando a spec tem tickets, ou com `/implement` para um trabalho único sem grafo de tickets.
- **Commits**: pequenos e atômicos, um por mudança lógica. Cada commit compila e passa nos testes sozinho.
- **Revisão e PR**: terminada a implementação, rodar `/code-review` no branch. É o skill de dois eixos, Standards e Spec, descrito em https://www.aihero.dev/skills-code-review; nenhum outro revisor o substitui. Com a revisão limpa e os testes passando, abrir o pull request; com problemas, corrigir e revisar de novo antes de abrir.
- **Merge**: o agente mescla o pull request na `main` quando o `/code-review` está limpo e o CI do pull request passou. Depois do merge, exclui os branches já mesclados, no repositório local e no GitHub.
- **Branches**: cada tarefa é implementada em branch próprio. A `main` só recebe trabalho por merge de pull request.
- **Tickets**: issues do GitHub com o rótulo `ready-for-agent`; dependências escritas no corpo como `Blocked by #N`.

## Convenções

- **Linguagem do domínio**: `GLOSSARY.md` define os termos e o nome em inglês de cada um. Código, comentários e commits usam o nome em inglês; documentação e textos do jogo usam o termo em português.
- **Decisões de arquitetura**: `docs/adr/`. Ler antes de mexer no núcleo de simulação ou na fronteira dele com o Godot.
- **Núcleo de simulação**: usa só `Fix64`, o gerador aleatório da partida e iteração em ordem de ID. Tipos do Godot, `float`/`double`, relógio do sistema e threads ficam na camada de apresentação.
