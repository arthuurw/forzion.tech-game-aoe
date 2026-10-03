# Jogo RTS — História do Brasil

Jogo de estratégia em tempo real ambientado na história do Brasil, com progressão por Eras e cenários baseados em conflitos históricos.

Cada termo traz entre parênteses o nome em inglês usado no código.

## Partida

**Partida** (`Match`):
Disputa entre Jogadores num mapa, do início até a vitória de um deles. Partida livre e Cenário são modos de jogar uma Partida.
_Avoid_: Jogo, sessão, rodada

**Tick** (`Tick`):
Passo fixo em que a Partida avança, vinte por segundo de jogo. As ordens dadas pelos Jogadores valem a partir do Tick seguinte.
_Avoid_: Turno

**Quadro** (`Frame`):
Uma imagem da Partida desenhada na tela. Quadros não seguem o ritmo dos Ticks: entre dois Ticks podem ser desenhados vários Quadros ou nenhum.
_Avoid_: Tick

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
Povo ou potência jogável, com unidades, construções e nomes de Era próprios. Povos indígenas e quilombolas são Facções com a mesma agência das europeias. As Eras de uma Facção, com o nome de cada uma e o que cada uma libera, são dados da Facção, e não regras.
_Avoid_: Civilização, raça, nação, tribo

**Portugueses** (`Portuguese`):
Facção da Coroa portuguesa e dos colonos que ela mandou ao Brasil. O nome exibido fica sob a chave `FACTION_PORTUGUESE`; suas Eras e unidades estão em [Nomes dos Portugueses](#nomes-dos-portugueses).

## Progressão

**Era** (`Age`):
Estágio numerado (I a IV) que determina quais unidades, construções e tecnologias estão disponíveis. Cada Facção dá um nome próprio a cada estágio. Na Partida livre o jogador avança entre Eras; num Cenário a Era é fixa. Todo Jogador começa a Partida livre na Era I. No código, o número da Era do Jogador é `Age` e os dados de uma Era dentro da Facção (nome, Custo e tempo do Avanço, o que libera) são `FactionAge`.
_Avoid_: Idade, época, tier

**Avanço de Era** (`Age Advance`):
Ato de um Jogador pagar Recursos para passar à Era seguinte. É feito no Centro: o Custo é pago por inteiro na hora da ordem, e o Jogador passa à Era seguinte quando termina o tempo do Avanço. O Custo e o tempo são os da Era de destino.
_Avoid_: Evolução, upgrade de era

**Liberar** (`Unlock`):
O que uma Era faz com unidades e construções da Facção: o Jogador só treina uma unidade ou posiciona uma construção depois de chegar à Era que a libera. O que a Era atual do Jogador ainda não liberou está bloqueado (`Locked`), e a ordem para treiná-lo ou posicioná-lo é recusada.
_Avoid_: Desbloquear, destravar, habilitar

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

## Unidades

**Unidade** (`Unit`):
Peça móvel de um Jogador, civil como o Aldeão ou militar. Unidades andam pelas Células livres e não bloqueiam umas às outras.
_Avoid_: Personagem, tropa, boneco

## Construções

**Construção** (`Building`):
Edificação de um Jogador, como o Centro ou a Casa. Ocupa um retângulo de Células inteiras e bloqueia a passagem desde que é posicionada.
_Avoid_: Prédio, estrutura, edifício

**Obra** (`Construction Site`):
Construção já posicionada e paga que ainda não está pronta. Só avança enquanto Aldeões a constroem; o trabalho de vários Aldeões soma.
_Avoid_: Fundação, alicerce, canteiro

**Construir** (`Build`):
Trabalho do Aldeão ao lado de uma Obra do seu Jogador, até ela ficar pronta.
_Avoid_: Erguer, edificar

**Custo** (`Cost`):
Recursos que o Jogador paga por uma construção, por inteiro e na hora em que posiciona a Obra, ou por uma unidade, por inteiro e na hora em que a põe na Fila de treino.
_Avoid_: Preço

**Limite de população** (`Population Limit`):
Quantas unidades o Jogador pode ter ao mesmo tempo. O Centro dá um valor base e cada Casa pronta soma. Um Treino que faria a População passar do limite é recusado.
_Avoid_: Capacidade, teto

**Centro** (`Town Center`):
Construção principal do Jogador: produz Aldeões, recebe Recursos e executa o Avanço de Era. Perder o Centro é perder a partida.
_Avoid_: Base, sede, prefeitura

**Casa** (`House`):
Construção que aumenta o limite de população do Jogador.

**Quartel** (`Barracks`):
Construção que produz unidades militares.

## Produção

**Treinar** (`Train`):
Produzir uma unidade numa construção pronta: o Centro treina Aldeões e o Quartel, unidades militares. Leva um tempo fixo por tipo de unidade, e a unidade pronta aparece numa Célula livre ao lado da construção.
_Avoid_: Recrutar, criar, produzir

**Fila de treino** (`Training Queue`):
As unidades que uma construção ainda vai treinar, em ordem; só a primeira avança. O Custo é pago ao pôr a unidade na fila e devolvido por inteiro se ela for cancelada antes de ficar pronta.
_Avoid_: Lista de produção, buffer

**População** (`Population`):
Quantas unidades o Jogador tem, somadas às que estão nas Filas de treino das suas construções.
_Avoid_: Contagem de unidades

**Ponto de reunião** (`Rally Point`):
Célula de uma construção que treina unidades para onde as unidades recém-treinadas andam sozinhas.
_Avoid_: Bandeira, ponto de encontro

## Combate

**Soldado corpo a corpo** (`Melee Soldier`):
Unidade militar que precisa encostar no alvo para atacar. É um tipo de unidade do motor: cada Facção lhe dá o seu nome (ver [Nomes dos Portugueses](#nomes-dos-portugueses)).

**Soldado à distância** (`Ranged Soldier`):
Unidade militar que ataca de longe, sem projétil simulado. É um tipo de unidade do motor: cada Facção lhe dá o seu nome.

**Soldado pesado** (`Heavy Soldier`):
Unidade militar corpo a corpo mais forte, liberada pela Era II. É um tipo de unidade do motor: cada Facção lhe dá o seu nome.

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

**Floresta** (`Forest`):
Obstáculo que ocupa Células inteiras: Unidades não a atravessam e Construções não são erguidas sobre ela. Não é coletada; a Madeira vem das Fontes de Recurso.
_Avoid_: Mata, bosque, árvores

**Água** (`Water`):
Obstáculo que ocupa Células inteiras: Unidades não a atravessam e Construções não são erguidas sobre ela.

## Nomes dos Portugueses

Nomes que a Facção Portugueses dá às suas Eras e unidades, escolhidos na história do Brasil colonial dos séculos XVI e XVII. São textos do jogo: ficam em `game/translations/pt_BR.po`, sob as chaves entre parênteses, e os dados da Facção guardam só a chave. O código continua usando os nomes genéricos (`Age` 1, `MeleeSoldier` e assim por diante).

**Era das Feitorias** (Era I, `FACTION_PORTUGUESE_AGE_1`):
Os primeiros anos da colônia, até a década de 1530, quando a Coroa explorava o pau-brasil por meio de feitorias no litoral.

**Era dos Engenhos** (Era II, `FACTION_PORTUGUESE_AGE_2`):
A colônia do açúcar, da segunda metade do século XVI em diante, organizada em torno dos engenhos.

**Rodeleiro** (`MeleeSoldier`, `FACTION_PORTUGUESE_MELEE_SOLDIER`):
Infante de espada e rodela, o escudo redondo, das tropas portuguesas dos séculos XVI e XVII.

**Arcabuzeiro** (`RangedSoldier`, `FACTION_PORTUGUESE_RANGED_SOLDIER`):
Infante armado de arcabuz, a arma de fogo portátil das expedições portuguesas do século XVI.

**Piqueiro** (`HeavySoldier`, `FACTION_PORTUGUESE_HEAVY_SOLDIER`):
Infante de pique e corselete, o núcleo pesado dos terços que defenderam o Brasil no século XVII.

O Aldeão (`Villager`, `FACTION_PORTUGUESE_VILLAGER`) mantém o nome genérico.
