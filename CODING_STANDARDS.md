# Padrões de código

Regras de como o código deste repositório é escrito. O `/code-review` usa este arquivo no eixo Standards: cada regra tem um identificador (por exemplo `SIM-2`) para ser citada num achado.

O que o compilador e o `.editorconfig` já garantem (formatação, namespaces de arquivo, nulabilidade, avisos como erro) fica fora daqui.

## Linguagem e nomes

- **NOM-1**: código, comentários e mensagens de commit em inglês. Documentação e textos do jogo em português.
- **NOM-2**: todo conceito do domínio usa no código o nome em inglês registrado no `GLOSSARY.md`. Um conceito novo entra no glossário antes de ganhar nome no código.
- **NOM-3**: os termos listados como _Avoid_ no glossário ficam fora de nomes de tipos, membros e testes.

## Núcleo de simulação: determinismo

Valem para tudo em `src/Forzion.Simulation`. A origem é o ADR 0002: a mesma seed com os mesmos comandos produz o mesmo estado em qualquer sistema.

- **SIM-1**: números fracionários são `Fix64`. `float` e `double` aparecem só em `Fix64.ToFloat` e `Fix64.ToDouble`, que existem para a apresentação.
- **SIM-2**: aleatoriedade vem só do gerador da partida (`MatchRandom`), que faz parte do estado.
- **SIM-3**: toda iteração que influencia o estado tem ordem estável: por ID crescente ou por índice de Célula. Coleções sem ordem definida (`Dictionary`, `HashSet`) só entram quando o resultado independe da ordem de percurso.
- **SIM-4**: o núcleo roda numa única thread e não lê relógio, ambiente, cultura nem arquivos.
- **SIM-5**: todo estado que influencia ticks futuros entra no hash, no `WriteTo` da classe que o guarda. Coleções são escritas como contagem seguida dos elementos em ordem de ID.
- **SIM-6**: desempates são explícitos. Onde dois candidatos podem empatar (fila do A*, alvo mais próximo), a comparação é uma ordem total.

## Núcleo de simulação: estrutura

A origem é o ADR 0001: as regras do jogo vivem no núcleo, sem referência à engine.

- **EST-1**: o núcleo não referencia o Godot nem pacote externo algum.
- **EST-2**: `Match` é a única porta de entrada. Quem usa o núcleo cria a partida, enfileira comandos, avança ticks e lê estado, hash e eventos. Sistemas, gerador de mapa, `Pathfinder` e `StateHasher` são `internal`.
- **EST-3**: o estado só muda por comando ou por sistema, dentro de um tick. As classes de estado expõem leitura pública e escrita `internal`.
- **EST-4**: cada comando é um `sealed record` derivado de `Command`, com a regra no próprio `Execute`. Um comando inválido é recusado inteiro com `context.Reject` e um `RejectionReason`, sem alterar o estado.
- **EST-5**: cada regra contínua é um sistema (`ISystem`) registrado em `Match.Systems`. A ordem desse array é parte das regras.
- **EST-6**: o que a apresentação precisa saber que aconteceu é um evento (`MatchEvent`). Eventos informam e nunca alteram estado.
- **EST-7**: valores de balanceamento (custos, vida, dano, velocidades, tempos, tamanhos) ficam em `Balance`, separados das regras.
- **EST-8**: a grade de ocupação do mapa e as listas de entidades andam juntas. Quem cria ou remove uma entidade que ocupa Células atualiza as duas no mesmo passo.
- **EST-9**: o estado é dado simples identificado por ID, com sistemas escritos à mão. Sem biblioteca ECS e sem hierarquia de herança para unidades e construções.

## Apresentação

- **APR-1**: a camada Godot mostra o estado, conduz os ticks e transforma input em comandos. Nenhuma regra do jogo é decidida nela.
- **APR-2**: a conversão de `Fix64` para ponto flutuante acontece só na apresentação.
- **APR-3**: lógica de apresentação que não depende da engine (acumulador de ticks, fator de interpolação) fica em C# puro, coberta pelo projeto de testes.

## Testes

- **TST-1**: os testes das regras passam só pela interface pública de `Match`: criam a partida, enfileiram comandos, avançam ticks e verificam estado, eventos ou hash. Nenhum teste alcança tipos `internal`.
- **TST-2**: `Fix64` é o segundo ponto de teste e é testado direto, de preferência com propriedades FsCheck.
- **TST-3**: o nome do teste é uma frase em inglês que descreve o comportamento observado, com sublinhados entre as palavras.
- **TST-4**: um hash gravado como literal é validado por um meio independente da implementação. Quem altera o estado coberto pelo hash regrava os literais e diz como validou os novos valores.
- **TST-5**: montagem repetida de cenário fica em auxiliares do projeto de testes (como `TestMatches` e `Walk`), que também usam só a interface pública.
- **TST-6**: a camada Godot não tem teste automatizado; é verificada rodando o jogo.

## C#

- **CS-1**: tipos e membros públicos têm comentário XML que explica o que o leitor não vê na assinatura: unidade, arredondamento, ordem, motivo.
- **CS-2**: comentário de linha explica o porquê de uma escolha. O que o código faz fica a cargo dos nomes.
- **CS-3**: um `switch` sobre um enum trata todos os valores e lança `ArgumentOutOfRangeException` no caso padrão.
- **CS-4**: tipos pequenos de valor do domínio (`PlayerId`, `EntityId`, `CellPosition`, `MapPosition`) são usados no lugar de `int` e tuplas soltas.
