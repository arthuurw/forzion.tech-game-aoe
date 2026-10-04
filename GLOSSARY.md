# Jogo RTS — História do Brasil

Jogo de estratégia em tempo real ambientado na história do Brasil, com progressão por Eras e cenários baseados em conflitos históricos.

Cada termo traz entre parênteses o nome em inglês usado no código.

## Partida

**Partida** (`Match`):
Disputa entre Jogadores num mapa, do início até a vitória de um deles. Partida livre e Cenário são modos de jogar uma Partida.
_Avoid_: Jogo, sessão, rodada

**Tick** (`Tick`):
Passo fixo em que a Partida avança, vinte por segundo de jogo. Os Comandos dos Jogadores valem a partir do Tick seguinte.
_Avoid_: Turno

**Quadro** (`Frame`):
Uma imagem da Partida desenhada na tela. Quadros não seguem o ritmo dos Ticks: entre dois Ticks podem ser desenhados vários Quadros ou nenhum.
_Avoid_: Tick

**Ordem** (`Order`):
O que o Jogador manda fazer pela interface: um clique, um botão. Cada Ordem vira um Comando enviado à Partida; é a Partida que decide se ele vale.
_Avoid_: Comando, ação

**Comando** (`Command`):
Pedido que a Partida recebe de um Jogador, humano ou IA, e aplica no Tick seguinte ou recusa com um motivo. É a única forma de mudar a Partida de fora.
_Avoid_: Ordem, instrução

**Seleção** (`Selection`):
As unidades ou a construção do Jogador escolhidas com o mouse, que recebem as suas próximas Ordens. Existe só na tela: a Partida não a conhece.
_Avoid_: Grupo

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

**IA** (`AI`):
Jogador controlado pelo próprio jogo. Lê o estado da Partida e envia os mesmos Comandos que as Ordens de um Jogador humano enviariam, seguindo um roteiro fixo (`AI Script`): coletar, erguer Casas e um Quartel, treinar, fazer o Avanço de Era e atacar o Centro inimigo quando o Exército chega a um tamanho. Joga com as mesmas regras e Custos e não vê nada que um humano não veja. Quais Jogadores são IA é dado da configuração da Partida.
_Avoid_: Bot, computador, CPU

**Facção** (`Faction`):
Povo ou potência jogável, com unidades, construções e nomes de Era próprios. Povos indígenas e quilombolas são Facções com a mesma agência das europeias. As Eras de uma Facção, com o nome de cada uma e o que cada uma libera, são dados da Facção, e não regras.
_Avoid_: Civilização, raça, nação, tribo

**Portugueses** (`Portuguese`):
Facção da Coroa portuguesa e dos colonos e moradores luso-brasileiros. O nome exibido fica sob a chave `FACTION_PORTUGUESE`; suas Eras e unidades estão em [Nomes dos Portugueses](#nomes-dos-portugueses).

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

**Espera** (`Waiting`):
Aldeão sem caminho até a Fonte, o Ponto de entrega ou a Obra do seu trabalho: primeiro anda até a Célula alcançável mais próxima dela e então espera ali, parado. Não é Ocioso: guarda o trabalho e a Carga e segue sozinho quando o caminho pode ter se aberto: uma Construção destruída, uma Fonte de Recurso esgotada ou um Ponto de entrega concluído.

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
Construção principal do Jogador: treina Aldeões, recebe Recursos e executa o Avanço de Era. Perder o Centro é perder a partida.
_Avoid_: Base, sede, prefeitura

**Casa** (`House`):
Construção que aumenta o limite de população do Jogador.

**Quartel** (`Barracks`):
Construção que treina unidades militares.

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
Unidade militar corpo a corpo mais forte e mais rápida, liberada pela Era II. É um tipo de unidade do motor: cada Facção lhe dá o seu nome.

**Pontos de vida** (`Hit Points`):
Quanto dano uma unidade ou construção ainda aguenta. Ao chegar a zero, ela sai do mapa no mesmo tick.
_Avoid_: HP, saúde, energia

**Exército** (`Army`):
As unidades militares de um Jogador. Aldeões não fazem parte dele.
_Avoid_: Tropa, força

**Dano** (`Damage`):
Pontos de vida que um golpe tira do Alvo.

**Alvo** (`Target`):
A unidade ou construção inimiga que uma unidade militar está atacando.

**Alcance** (`Range`):
Distância máxima, em Células, entre a unidade e o alvo para que o golpe acerte. Fora dele, a unidade persegue o alvo.

**Intervalo de ataque** (`Attack Interval`):
Ticks que a unidade passa com o alvo ao alcance para cada golpe; o dano entra ao fim deles.

**Raio de percepção** (`Perception Radius`):
Distância, em Células, em que uma unidade militar parada e sem alvo nota um inimigo e passa a atacá-lo. Unidades inimigas vêm primeiro; sem nenhuma no raio, a construção inimiga mais próxima, Obras incluídas.
_Avoid_: Visão, campo de visão

**Derrota** (`Defeat`):
Jogador sem Centro é derrotado e não dá mais Comandos; suas unidades e construções que restam ficam no mapa e agem sozinhas. Quando resta no máximo um Jogador não derrotado, a partida termina; esse Jogador, se houver, é o vencedor (`Winner`).

## Mapa

**Célula** (`Cell`):
Menor unidade do mapa, um quadrado do grid. Obstáculos e construções ocupam Células inteiras.
_Avoid_: Tile, casa, quadrado

**Floresta** (`Forest`):
Obstáculo que ocupa Células inteiras: Unidades não a atravessam e Construções não são erguidas sobre ela. Não é coletada; a Madeira vem das Fontes de Recurso.
_Avoid_: Mata, bosque, árvores

**Água** (`Water`):
Obstáculo que ocupa Células inteiras: Unidades não a atravessam e Construções não são erguidas sobre ela.

## Tela

**HUD** (`Hud`):
Os controles desenhados sobre a Partida: a barra do topo, com os Recursos, a População sobre o Limite de população, a Era e o Avanço de Era em andamento do Jogador, e embaixo o Painel de seleção. Existe só na tela: os botões da HUD dão ordens, e a Partida as aceita ou recusa.
_Avoid_: Interface, UI

**Painel de seleção** (`Selection Panel`):
Parte de baixo da HUD que mostra a Seleção: as unidades com seus Pontos de vida, ou a construção com seus Pontos de vida, sua Obra, sua Fila de treino e as ordens que aceita. Com Aldeões selecionados, oferece as construções que eles podem posicionar.

**Prévia de posicionamento** (`Placement Preview`):
A construção escolhida no Painel de seleção, desenhada sob o mouse onde seria posicionada: verde onde o local está livre e vermelha onde não está. Mostra só se o local está livre, e não se o Jogador pode pagar o Custo.

**Barra sobre o mapa** (`World Bar`):
Barra desenhada sobre uma unidade ou construção, acima de onde ela está no mapa e do mesmo tamanho em qualquer zoom. Há dois tipos (`WorldBarKind`): a Barra de vida e a Barra de obra.

**Barra de vida** (`Hit Point Bar`):
Barra sobre o mapa de uma unidade ou construção selecionada ou ferida, preenchida na proporção dos Pontos de vida que restam.

**Barra de obra** (`Construction Bar`):
Barra sobre o mapa de cada Obra, preenchida na proporção do trabalho de construção já feito.

## Nomes dos Portugueses

Nomes que a Facção Portugueses dá às suas Eras e unidades, escolhidos na história do Brasil colonial dos séculos XVI e XVII. As fontes de cada verbete estão em [docs/fontes-historicas.md](docs/fontes-historicas.md). São textos do jogo: ficam em `game/translations/pt_BR.po`, sob as chaves entre parênteses, e os dados da Facção guardam só a chave. O código continua usando os nomes genéricos (`Age` 1, `MeleeSoldier` e assim por diante).

**Era das Feitorias** (Era I, `FACTION_PORTUGUESE_AGE_1`):
Os primeiros anos da presença portuguesa, até a década de 1530, quando o pau-brasil, monopólio da Coroa arrendado a mercadores, era trocado com os indígenas e embarcado em feitorias no litoral.

**Era dos Engenhos** (Era II, `FACTION_PORTUGUESE_AGE_2`):
A colônia do açúcar. Os primeiros engenhos são da década de 1530; da segunda metade do século XVI em diante, sobretudo a partir da década de 1570, a colônia se organiza em torno deles.

**Rodeleiro** (`MeleeSoldier`, `FACTION_PORTUGUESE_MELEE_SOLDIER`):
Combatente de espada e rodela, o escudo redondo, das tropas portuguesas dos séculos XVI e XVII. No Brasil, os rodeleiros cobriam os arcabuzeiros contra as flechas na conquista da Paraíba, entre 1585 e 1587; na guerra holandesa, a espada e rodela seguiu em uso.

**Arcabuzeiro** (`RangedSoldier`, `FACTION_PORTUGUESE_RANGED_SOLDIER`):
Combatente armado de arcabuz. O Regimento de 1548 obrigava cada capitão a ter arcabuzes, e arcabuzeiros combateram na conquista da Paraíba, em 1585.

**Cavaleiro** (`HeavySoldier`, `FACTION_PORTUGUESE_HEAVY_SOLDIER`):
Combatente a cavalo. No Brasil quinhentista, os de cavalo do ouvidor-geral Martim Leitão combateram na conquista da Paraíba, em 1585.

**Colono** (`Villager`, `FACTION_PORTUGUESE_VILLAGER`):
O povoador, que as fontes de época chamam de morador. Na colônia, o trabalho nos canaviais e engenhos era feito sobretudo por indígenas e africanos escravizados.
