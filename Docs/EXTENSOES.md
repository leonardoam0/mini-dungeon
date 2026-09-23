# Extensões planejadas (não implementadas)

A especificação lista cooperação de até quatro jogadores, várias missões e geração modular como expansão
posterior. Nenhuma delas está implementada. Abaixo, os limites de responsabilidade já existentes que permitem
adicioná-las sem reescrever o código local, e o que faltaria.

## Cooperação (até 4 jogadores)

Pontos de apoio atuais:

- Toda entrada chega ao `PlayerController` por `IPlayerCommandSource` (entrada ao vivo, roteiro, piloto
  automático). Um jogador remoto seria outra fonte de comandos, sem alterar o controlador.
- Dano passa por um único fluxo (`CombatResolver.Apply`) com identificador de evento e registro evento×alvo;
  em rede, a autoridade aplicaria o dano e replicaria o `DamageResult`.
- Atores registrados em `ActorRegistry`; alvos de IA escolhidos pelo registro, não por referência fixa ao jogador.
- Encontros usam uma máquina de estados pura (`EncounterStateMachine`) que pode rodar só na autoridade.

Faltaria: camada de transporte e sessão, `LevelContext` com vários jogadores (hoje há um `Player`), câmera
compartilhada ou dividida, HUD por jogador, loot por jogador e escalonamento de dificuldade por número de
participantes. O save é por máquina e não trata progresso compartilhado.

## Várias missões

Pontos de apoio atuais:

- Níveis são descritos em código (`LevelLayouts`) e montados por `LevelAssembler` em cenas independentes.
- O save separa progresso do personagem (`ProgressData`, `InventoryData`) do estado da missão (`MissionData`).
- `SceneFlow` carrega cenas por pedido (`LoadRequest`), com modo novo/continuar/voltar ao acampamento.

Faltaria: seleção de missão no acampamento/menu, `MissionData` indexado por missão, tabela de dificuldade e de
loot por missão e migração do save para o novo formato (o save já é versionado e tem migração).

## Geração modular

Pontos de apoio atuais:

- `LevelBuilder` já monta a arena como módulo parametrizado (`LevelLayouts.Arena`) e oferece primitivas de
  plataforma, escada, muro, pilar, zona, ponto de retorno, baú, portão e encontro.
- O mapa sobreposto é construído a partir de `MapData` gerado pelo próprio montador.

Faltaria: catálogo de módulos com conectores (portas nas bordas), montagem por semente (`GameRandom` já é
determinístico), validação de caminho (NavMesh após a montagem) e regras de ritmo (combate, respiro,
recompensa). A arena de referência continuaria autoral e fixa, fora da geração.
