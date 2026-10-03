# Jogo RTS — História do Brasil

Jogo de estratégia em tempo real ambientado na história do Brasil, com progressão por Eras e cenários baseados em conflitos históricos.

Cada termo traz entre parênteses o nome em inglês usado no código.

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

**Aldeão** (`Villager`):
Unidade civil que coleta Recursos e ergue construções.
_Avoid_: Trabalhador, camponês, operário

**Depósito** (`Storehouse`):
Construção onde Aldeões entregam os Recursos coletados.
_Avoid_: Armazém, celeiro

**Fonte de Recurso** (`Resource Source`):
Lugar do mapa, de uma Célula, de onde se coleta um único Recurso. Guarda uma quantidade finita e sai do mapa ao se esgotar.
_Avoid_: Nó, jazida, mina

**Coleta** (`Gather`):
Ciclo do Aldeão numa Fonte de Recurso: tirar Recurso até encher a Carga, entregá-la no Ponto de entrega mais próximo e voltar, sem nova ordem. Quando a Fonte se esgota, o Aldeão segue para outra Fonte próxima do mesmo Recurso ou fica Ocioso.
_Avoid_: Colheita, extração, mineração

**Carga** (`Load`):
O Recurso que um Aldeão leva consigo, de um só tipo e até a capacidade de carga. Só passa ao Jogador quando é entregue.
_Avoid_: Inventário, mochila

**Ponto de entrega** (`Drop-off Point`):
Construção onde o Aldeão entrega a Carga: o Centro e o Depósito.
_Avoid_: Base, armazém

**Ocioso** (`Idle`):
Aldeão parado, sem ordem nem Coleta em andamento.
_Avoid_: Livre, desocupado

## Construções

**Centro** (`Town Center`):
Construção principal do Jogador: produz Aldeões, recebe Recursos e executa o Avanço de Era. Perder o Centro é perder a partida.
_Avoid_: Base, sede, prefeitura

**Casa** (`House`):
Construção que aumenta o limite de população do Jogador.

**Quartel** (`Barracks`):
Construção que produz unidades militares.

## Combate

**Soldado corpo a corpo** (`Melee Soldier`):
Unidade militar que precisa encostar no alvo para atacar. Nome provisório até a escolha do nome histórico.

**Soldado à distância** (`Ranged Soldier`):
Unidade militar que ataca de longe, sem projétil simulado. Nome provisório até a escolha do nome histórico.

**Pontos de vida** (`Hit Points`):
Quanto dano uma unidade ou construção ainda aguenta. Ao chegar a zero, ela sai do mapa no mesmo tick.
_Avoid_: HP, saúde, energia

**Alvo** (`Target`):
A unidade ou construção inimiga que uma unidade militar está atacando.

**Alcance** (`Range`):
Distância máxima, em Células, entre a unidade e o alvo para que o golpe acerte. Fora dele, a unidade persegue o alvo.

**Intervalo de ataque** (`Attack Interval`):
Ticks que a unidade passa com o alvo ao alcance para cada golpe; o dano entra ao fim deles.

**Raio de percepção** (`Perception Radius`):
Distância, em Células, em que uma unidade militar parada e sem alvo nota uma unidade inimiga e passa a atacá-la.
_Avoid_: Visão, campo de visão

**Derrota** (`Defeat`):
Jogador sem Centro é derrotado e não dá mais ordens. Quando resta no máximo um Jogador não derrotado, a partida termina; esse Jogador, se houver, é o vencedor (`Winner`).

## Mapa

**Célula** (`Cell`):
Menor unidade do mapa, um quadrado do grid. Obstáculos e construções ocupam Células inteiras.
_Avoid_: Tile, casa, quadrado
