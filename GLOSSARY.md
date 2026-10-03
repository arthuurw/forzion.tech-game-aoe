# Jogo RTS — História do Brasil

Jogo de estratégia em tempo real ambientado na história do Brasil, com progressão por Eras e cenários baseados em conflitos históricos.

Cada termo traz entre parênteses o nome em inglês usado no código.

## Partida

**Partida** (`Match`):
Disputa entre Jogadores num mapa, do início até a vitória de um deles. Partida livre e Cenário são modos de jogar uma Partida.
_Avoid_: Jogo, sessão, rodada

**Tick** (`Tick`):
Passo fixo em que a Partida avança, vinte por segundo de jogo. As ordens dadas pelos Jogadores valem a partir do Tick seguinte.
_Avoid_: Quadro, frame, turno

## Modos de jogo

**Partida livre** (`Skirmish`):
Partida sem roteiro, em mapa escolhido, contra IA (ou outros jogadores no futuro), em que os jogadores avançam entre Eras.
_Avoid_: Jogo rápido, mata-mata

**Cenário** (`Scenario`):
Partida com mapa, objetivos e roteiro pré-definidos, baseada num conflito histórico e ambientada numa Era fixa.
_Avoid_: Missão, fase, nível

**Campanha** (`Campaign`):
Sequência ordenada de Cenários sobre um mesmo conflito histórico.
_Avoid_: História, modo história

## Jogadores e facções

**Jogador** (`Player`):
Participante de uma partida, humano ou IA, que controla uma Facção.
_Avoid_: Time, lado

**Facção** (`Faction`):
Povo ou potência jogável, com unidades, construções e nomes de Era próprios. Povos indígenas e quilombolas são Facções com a mesma agência das europeias.
_Avoid_: Civilização, raça, nação, tribo

## Progressão

**Era** (`Age`):
Estágio numerado (I a IV) que determina quais unidades, construções e tecnologias estão disponíveis. Cada Facção dá um nome próprio a cada estágio. Na Partida livre o jogador avança entre Eras; num Cenário a Era é fixa.
_Avoid_: Idade, época, tier

**Avanço de Era** (`Age Advance`):
Ato de um Jogador pagar Recursos para passar à Era seguinte.
_Avoid_: Evolução, upgrade de era

## Economia

**Recurso** (`Resource`):
Bem acumulado pelo Jogador e gasto em unidades, construções e Avanço de Era. São três: Alimento (`Food`), Madeira (`Wood`) e Ouro (`Gold`).
_Avoid_: Material, moeda

**Fonte de Recurso** (`Resource Source`):
Lugar do mapa onde os Aldeões coletam um Recurso. Ocupa uma Célula inteira e guarda uma quantidade finita, que se esgota.
_Avoid_: Mina, jazida, nó de recurso

**Aldeão** (`Villager`):
Unidade civil que coleta Recursos e ergue construções.
_Avoid_: Trabalhador, camponês, operário

**Depósito** (`Storehouse`):
Construção onde Aldeões entregam os Recursos coletados.
_Avoid_: Armazém, celeiro

## Unidades

**Unidade** (`Unit`):
Peça móvel de um Jogador, civil como o Aldeão ou militar. Unidades andam pelas Células livres e não bloqueiam umas às outras.
_Avoid_: Personagem, tropa, boneco

## Construções

**Construção** (`Building`):
Edificação de um Jogador, como o Centro ou a Casa. Ocupa um retângulo de Células inteiras e bloqueia a passagem.
_Avoid_: Prédio, estrutura, edifício

**Centro** (`Town Center`):
Construção principal do Jogador: produz Aldeões, recebe Recursos e executa o Avanço de Era. Perder o Centro é perder a partida.
_Avoid_: Base, sede, prefeitura

**Casa** (`House`):
Construção que aumenta o limite de população do Jogador.

**Quartel** (`Barracks`):
Construção que produz unidades militares.

## Mapa

**Célula** (`Cell`):
Menor unidade do mapa, um quadrado do grid. Obstáculos e construções ocupam Células inteiras.
_Avoid_: Tile, casa, quadrado

**Floresta** (`Forest`):
Obstáculo que ocupa Células inteiras: Unidades não a atravessam e Construções não são erguidas sobre ela. Não é coletada; a Madeira vem das Fontes de Recurso.
_Avoid_: Mata, bosque, árvores

**Água** (`Water`):
Obstáculo que ocupa Células inteiras: Unidades não a atravessam e Construções não são erguidas sobre ela.
