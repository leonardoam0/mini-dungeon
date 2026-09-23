# Ruínas do Obelisco

Dungeon crawler 3D em **Unity 6 (6000.6.0f1) + URP**, com:

- **Entrega A — arena de referência jogável**: reprodução da cena do vídeo de referência (câmera elevada em
  três quartos, arena de templo com obelisco, contorno luminoso, escadas, portão violeta, braseiros, vegetação
  vermelha, nuvens verdes, pulso magenta, HUD e mapa sobreposto), com modo de comparação determinístico.
- **Entrega B — percurso completo** (~10–15 min): acampamento → corredor → sala das colunas (+ desvio opcional)
  → arena do obelisco → área de respiro com mercador → salão do guardião → cofre e saída; inventário,
  equipamentos, artefatos, encantamentos, status, progressão, menus, salvamento versionado e recuperação após
  derrota.

Todo o conteúdo (texturas, malhas, personagens, animações, efeitos, fonte, sons e cenas) é gerado por código
dentro do projeto — ver [Docs/INVENTARIO_ASSETS.md](Docs/INVENTARIO_ASSETS.md). Não há assets de terceiros.
O jogo é uma obra independente que usa o trecho de Minecraft Dungeons apenas como referência visual.

## Requisitos

| Item | Versão / observação |
| --- | --- |
| Sistema | Windows 10/11 x64, GPU com DirectX 12 (a build usa D3D12) |
| Editor (para abrir o projeto) | Unity **6000.6.0f1** |
| Pacotes fixados | URP 17.6.0 · Input System 1.20.0 · AI Navigation 2.0.14 · uGUI 2.6.0 · Test Framework 1.8.0 (ver `Packages/manifest.json`) |
| Ferramentas opcionais | Python 3 com numpy, Pillow e OpenCV (comparação com o vídeo); ffmpeg (gravação em MP4) |

O jogo é offline, sem cadastro nem serviços externos.

## Executar a build

```
Build\Windows\RuinasDoObelisco.exe
```

No menu: **Continuar** / **Voltar ao acampamento** (quando houver save), **Novo jogo** (percurso completo),
**Cena de referência**, **Configurações**, **Controles**, **Sair**. O save fica em `%USERPROFILE%\AppData\LocalLow\Projeto Autoral\Ruínas do Obelisco\save`.

### Opções de linha de comando

| Opção | Efeito |
| --- | --- |
| `-reference` | abre direto a arena de referência com o roteiro de 14,6 s |
| `-mission` / `-continue` | abre o percurso (novo / continuar do save) |
| `-capture -captureDir <pasta>` | modo de comparação determinístico: tempo fixo de 1/30 s, sementes das partículas e relógio dos efeitos fixos; captura dos instantes do perfil (`ref_XX.Xs_16x9.png` e o recorte `_9x16.png`) |
| `-quitWhenDone` | encerra ao fim da captura/gravação/percurso automático |
| `-autoplay -timescale <n>` | piloto automático percorre a missão inteira pelos sistemas reais (validação) |
| `-perf -perfFile <arquivo>` | relatório de tempo de quadro/CPU/GPU (média, p50, p95, p99, picos) |
| `-testSaveLoad` | grava, relê e compara o save da missão, registrando `[SaveTest]` no log |
| `-recordDir <pasta> -recordFps 30 -recordSeconds <s>` | grava quadros JPG em passos fixos (ver `Tools/record-video.ps1`) |
| `-seed <n>`, `-saveDir <pasta>`, `-noHud`, `-noMap`, `-captureProfile`, `-refFrames <pasta>`, `-windowedTest`, `-quitAfter <s>`, `-logFile <arquivo>` | utilidades de teste |

## Controles

Remapeáveis em **Configurações → Controles** (teclado/mouse e controle têm mapas próprios; o HUD mostra os
ícones do dispositivo em uso).

| Ação | Teclado e mouse | Controle |
| --- | --- | --- |
| Mover | WASD / setas | analógico esquerdo |
| Mirar | mouse | analógico direito |
| Ataque corpo a corpo | botão esquerdo | A |
| Ataque à distância | botão direito | RT |
| Esquiva | Espaço | RB |
| Artefatos 1 / 2 / 3 | 1 / 2 / 3 | X / Y / B |
| Poção | Q | LB |
| Interagir / pegar | E | LT, ou A quando há algo ao alcance |
| Inventário | I | View |
| Mapa sobreposto | Tab | direcional para baixo |
| Pausa | Esc | Start |
| Atacar parado | Shift esquerdo | — |

Perfil alternativo "clicar para mover" em **Configurações** (clique no chão para ir, clique em inimigo para atacar).

### Arena de referência (atalhos de comparação)

| Tecla | Função |
| --- | --- |
| F1 | ajuda |
| R / Ctrl+R | reinicia a arena (sem / com o roteiro) |
| F10 | reproduz o roteiro ou a última gravação |
| F9 | grava / encerra a gravação das entradas |
| F5 | congela a simulação |
| F6 | câmera livre (J L I K U O) |
| F2 / F3 | liga/desliga HUD / mapa |
| F7 | sobrepõe o quadro correspondente do vídeo (pasta `Docs/Referencia/quadros`) |
| F11 | máscara do recorte 9:16 |
| F8 | captura o quadro atual |

## Abrir e regenerar o projeto

1. Abra a pasta `RuinasDoObelisco` no Unity Hub com a versão **6000.6.0f1**.
2. Cenas: `Assets/_Project/Scenes/Boot`, `Menu`, `ReferenceArena`, `Mission` (já geradas e incluídas).
3. Para regenerar todo o conteúdo a partir do código: menu **Ruinas → Regenerar tudo** (e **Ruinas → Build Windows x64**), ou em lote:

```
powershell -File Tools\unity-batch.ps1 -Log forge.log -Method Ruinas.EditorTools.Forge.All -NoQuit
```

`Ruinas.EditorTools.Forge.BuildWindows` gera a build; `Forge.AllAndBuild` faz as duas coisas.

## Testes

```
powershell -File Tools\unity-batch.ps1 -Log tests-editmode.log -Tests EditMode
powershell -File Tools\unity-batch.ps1 -Log tests-playmode.log -Tests PlayMode
```

Resultados em `Logs\tests-EditMode.xml` e `Logs\tests-PlayMode.xml` (última execução, sobre o código entregue: EditMode 20/20 e PlayMode 9/9 aprovados). Cobertura: dano único por evento,
bloqueio por parede, projéteis, recargas, políticas de status, transições do encontro e recompensa única,
reinício sem acúmulo, estados do jogo, projeção do mapa, integridade do save (ida e volta, cópia de segurança,
migração, validação), inventário, equipamento alterando atributos e derrota → ponto de retorno.

Validação na build (save, continuar, percurso completo pelo piloto automático e desempenho):

```
powershell -File Tools\validate-build.ps1
```

## Ferramentas

| Arquivo | Uso |
| --- | --- |
| `Tools/unity-batch.ps1` | Unity em lote (métodos do editor, testes) com resumo do log |
| `Tools/compare_reference.py` | lado a lado vídeo × jogo × diferença para cada captura, com indicadores por região |
| `Tools/record-video.ps1` | grava a build e gera MP4 em `Docs/Gravacoes` |
| `Tools/validate-build.ps1` | save, continuar, percurso completo (sem janela) e medições de desempenho |

Execuções automatizadas (`-autoplay`, `-capture`, gravação, `-testSaveLoad`) ignoram teclado, mouse e controle
reais, para que teclas digitadas em outra janela não interfiram no resultado. Duas capturas da mesma build
dão as mesmas posições e imagens praticamente iguais (ver repetibilidade em
[Docs/MATRIZ_VALIDACAO.md](Docs/MATRIZ_VALIDACAO.md)).

Exemplo de comparação:

```
Build\Windows\RuinasDoObelisco.exe -reference -capture -captureDir Logs\capturas -quitWhenDone -windowedTest
python Tools\compare_reference.py Logs\capturas Docs\Referencia\quadros Logs\comparacao <video.mp4>
```

## Documentação

- [Docs/DECISOES.md](Docs/DECISOES.md) — decisões com a classificação OBSERVADO / CONFIRMADO_POR_FONTE / ESTIMADO / PROPOSTO
- [Docs/MATRIZ_VALIDACAO.md](Docs/MATRIZ_VALIDACAO.md) — validação visual (seção 29) com capturas reais
- [Docs/RELATORIO_VERIFICACAO.md](Docs/RELATORIO_VERIFICACAO.md) — verificação funcional (seção 30), testes, desempenho e limitações
- [Docs/INVENTARIO_ASSETS.md](Docs/INVENTARIO_ASSETS.md) — origem de todos os assets
- [Docs/EXTENSOES.md](Docs/EXTENSOES.md) — cooperação, várias missões e geração modular (não implementadas)
- `Docs/Capturas/` — capturas reais da build, comparações lado a lado e tabela de marcos
- `Docs/Gravacoes/` — `referencia.mp4` (trecho de referência completo) e `percurso_trecho.mp4` (75 s do percurso com o piloto automático)

## Estrutura

```
Assets/_Project/
  Scripts/Runtime/   jogo (Core, Actors, Combat, AI, Encounters, Items, Artifacts, Player, UI, VFX, World, Reference)
  Scripts/Editor/    pipeline "Forge": texturas, malhas, personagens, poses, efeitos, sons, dados, níveis, cenas, build
  Art/Shaders/       VoxelLit (cor por vértice + AO, névoa de altura, recorte próximo ao jogador) e efeitos
  Data/ Prefabs/ Scenes/ Settings/   gerados pelo Forge
  Tests/             EditMode e PlayMode
Docs/                documentação, capturas, comparações e gravações
Tools/               scripts de lote, comparação e gravação
```
