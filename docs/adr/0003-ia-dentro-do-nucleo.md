# IA dentro do núcleo, decidindo no início de cada tick

A IA vive no núcleo de simulação, e não num componente que quem conduz a partida chama antes de cada tick. Quais Jogadores são IA é dado da configuração da partida (`PlayerConfig.IsAi`). No início de cada `Match.Tick`, antes de aplicar os comandos pendentes, cada Jogador IA não derrotado lê o estado que o tick anterior deixou e produz comandos comuns (`GatherCommand`, `PlaceBuildingCommand`, `TrainCommand` e os demais), que entram nos comandos pendentes e são aplicados naquele mesmo tick, na ordem estável de sempre: por Jogador e, dentro do Jogador, por chegada. É o mesmo momento em que um humano decide, entre o fim de um tick e o início do seguinte, olhando o mesmo estado.

Escolhemos isso em vez de um componente externo porque a aleatoriedade da IA tem de vir do gerador da partida (ADR 0002), que é estado interno: fora do núcleo, a IA só o alcançaria por um caminho que muda estado sem ser comando nem sistema. Dentro dele, uma partida com IA fica descrita inteira pela configuração e pelos comandos dos humanos, então o replay e o futuro lockstep não rodam nem sincronizam nada além do `Match`, e a Godot só marca o segundo Jogador como IA na configuração.

## Consequences

- Não existe caminho de regra exclusivo da IA: ela só produz `Command`, validado e recusado pelo mesmo `Execute` que atende o humano. Ela lê só o estado público (o slice não tem névoa de guerra); o único acesso interno é o gerador da partida.
- `IsAi` entra no hash (SIM-5), o que mudou uma vez todos os hashes gravados. O roteiro não guarda memória: cada decisão sai do estado do momento, e assim não há estado de IA a mais para o hash.
- As ordens da IA geram `CommandRejected` como as do humano; a apresentação já mostra só as recusas do Jogador humano.
- Tirar a IA do núcleo depois mudaria a configuração e o formato dos replays, que hoje não contêm os comandos da IA.
