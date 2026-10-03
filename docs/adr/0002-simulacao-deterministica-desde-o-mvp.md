# Simulação determinística desde o MVP

O MVP é single-player contra IA, mas a simulação é determinística desde o primeiro dia (tick fixo, RNG com seed, entrada apenas via comandos) para permitir multiplayer lockstep na fase 2 sem reescrever o núcleo. Adaptar determinismo depois custa muito mais do que mantê-lo desde o início; o custo agora é disciplina na matemática e no fluxo de entrada.

## Consequences

- O núcleo usa aritmética de ponto fixo (tipo `Fix64` próprio, coberto por testes de propriedade) em vez de `float`/`double`, porque floats divergem entre CPUs e compiladores e o jogo é multiplataforma. Conversão para `float` acontece só na camada Godot, para renderização.
