# Simulação em biblioteca C# pura, separada da engine

Toda a lógica do jogo (economia, produção, combate, pathfinding, IA) vive numa class library .NET sem nenhuma referência ao Godot; a engine apenas renderiza o estado e traduz input em comandos. Escolhemos isso em vez de lógica nos scripts da engine (o caminho tradicional) porque o projeto é portfólio para vagas de software geral: um núcleo headless e testável com xUnit demonstra arquitetura, permite rodar partidas sem gráficos (testes de IA em lote) e prepara o multiplayer da fase 2.

## Consequences

- Existe uma camada de ponte (estado da simulação → nodes do Godot; input → comandos) que precisa ser mantida.
- Nada no núcleo pode usar tipos do Godot (`Vector3`, `Node`, etc.).
