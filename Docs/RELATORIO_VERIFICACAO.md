# Relatório de verificação

Ambiente: Windows 11 Pro, Unity 6000.6.0f1, build Windows x64 (`Build/Windows/RuinasDoObelisco.exe`,
124,3 MB, 0 erros de build). Máquina de teste: AMD Ryzen 9 7900X3D (24 threads), NVIDIA GeForce RTX 5090,
64 GB. Todas as execuções abaixo foram feitas nesta máquina, sem sessão humana de jogo.

## Testes automatizados

| Conjunto | Resultado | Conteúdo |
| --- | --- | --- |
| EditMode | **20/20 aprovados** | registro evento×alvo e expiração; cálculo de dano (armadura, crítico, arredondamento); políticas e agregação de status; transições do encontro, recompensa única, reinício e restauração pelo save; estados do jogo; projeção do mapa; zona morta; recorte 9:16; progressão; save (ida e volta, recuperação pela cópia, ausente/ilegível, versão futura recusada, migração v1→v2, validação de itens e slots); inventário |
| PlayMode (cenas reais) | **9/9 aprovados** | golpe aplica dano uma vez por evento; golpe não atravessa parede; projétil acerta uma vez, gasta uma flecha e volta ao pool; poção respeita recarga e entrada duplicada; artefato respeita recarga; 6 reinícios da arena sem sobras (áreas, projéteis, avisos, atores, objetos raiz); derrota → ponto de retorno com uma vida a menos, vida cheia e save atualizado; equipar armaduras e arma encantada altera atributos; captura e releitura do save equivalentes |

Comandos: `Tools/unity-batch.ps1 -Tests EditMode` e `-Tests PlayMode`; resultados em `Logs/tests-*.xml`.
Última execução em 23/09/2026, sobre o código entregue.

## Execuções da build

| Verificação | Comando | Resultado |
| --- | --- | --- |
| Save grava/relê/compara | `-mission -testSaveLoad -quitWhenDone` (sem janela) | `[SaveTest] resultado=OK status=Ok` |
| Continuar do save | `-continue` | carregou o save do passo anterior, sem erros no log |
| Percurso completo | `-mission -autoplay -timescale 3 -quitWhenDone` (sem janela) | 13/13 etapas, 0 etapas com falha, 58 abates, 0 mortes, nível 7, **275,1 s de tempo de jogo** |
| Sequência de referência | `-reference -capture -captureDir … -quitWhenDone` | 17 instantes + quadro final capturados; posições registradas em `Docs/Capturas/captura_log.txt` |
| Repetibilidade da captura | duas execuções de `-reference -capture`, comparadas pixel a pixel | posições idênticas nos 17 instantes; 22 de 36 imagens idênticas bit a bit, nas demais no máximo 11 pixels com 1–2 níveis de diferença ([matriz](MATRIZ_VALIDACAO.md#repetibilidade)) |
| Gravações | `Tools/record-video.ps1` | `referencia.mp4` (15 s, 450 quadros) e `percurso_trecho.mp4` (75 s, 2 250 quadros), 1280×720 a 30 qps; conferidas por amostragem de quadros |
| Isolamento da entrada | `-autoplay`, `-capture`, gravação, `-testSaveLoad` | teclado, mouse e controle reais ignorados (mapas de ação e navegação da interface desativados); as gravações refeitas não mostram menus sobrepostos. Não houve teste com entrada real proposital durante a execução |
| Logs | todas as execuções | nenhum erro, exceção ou referência ausente |

Durante a validação o piloto automático ficou preso diante do cofre final: um pilar decorativo do muro norte
do salão era criado dentro da abertura do portão. O layout foi corrigido (pilares não são criados dentro de
aberturas) e o piloto automático passou a registrar estado periódico, desistir de alvos inalcançáveis e
reportar **FALHA** explícita se uma etapa exceder 150 s. O loot passou a cair apenas em pontos alcançáveis do
mesmo nível.

No corredor, o piloto automático fica ~40 s sem alvo: a heurística marca um arqueiro a 8–10 m como
inalcançável após 8 s sem reduzir a distância (enquanto atira, o piloto não se aproxima) e o ignora por 20 s.
A etapa conclui em 77–79 s nas três execuções observadas. Isso afeta o tempo do piloto; não foi verificado se
uma pessoa teria dificuldade nesse ponto.

A primeira gravação do percurso foi descartada: teclas digitadas enquanto a janela do jogo tinha o foco
abriram o menu de configurações no meio do vídeo. As execuções automatizadas passaram a ignorar a entrada
real, e a gravação foi refeita.

As capturas repetidas mostraram imagens diferentes entre execuções (posições iguais, mas partículas, brilhos,
mapa e interface variando). As causas e correções estão na seção de repetibilidade da
[matriz de validação](MATRIZ_VALIDACAO.md#repetibilidade).

## Verificação funcional (seção 30)

| # | Cenário | Como foi verificado | Resultado |
| --- | --- | --- | --- |
| 1 | Iniciar a cena sem referências ausentes | build executada em todas as validações; logs sem erros | aprovado (instalação em outra máquina não testada) |
| 2 | Piso, escadas, paredes, contornar o obelisco | piloto automático atravessa 8 áreas com escadas e desníveis; roteiro sobe e desce a escada oeste e contorna o obelisco | aprovado (indireto) |
| 3 | Movimento direto × por destino | entrada direta cancela o caminho por destino (código); perfil "clicar para mover" | implementado; não testado por pessoa |
| 4 | Dano único por evento; sem acerto através de parede | PlayMode + EditMode | aprovado |
| 5 | Tiro: impacto, munição, colisão | PlayMode | aprovado |
| 6 | Três artefatos, recarga, limpeza | PlayMode (slot 1), roteiro usa os três, piloto automático usa os três; limpeza no reinício | aprovado; recarga automatizada só do slot 1 (demais observados no HUD das capturas) |
| 7 | Dano, cura, morte e recuperação | PlayMode (poção; derrota → ponto de retorno) | aprovado |
| 8 | Concluir a arena, liberar progressão, recompensa única | EditMode + percurso completo (portões e baús) | aprovado |
| 9 | Reinício repetido sem acúmulo | PlayMode (6 reinícios) | aprovado |
| 10 | Equipar e alterar atributos | PlayMode | aprovado |
| 11 | Salvar, fechar, reabrir, recuperar | EditMode + build (`-testSaveLoad`, `-continue`) | aprovado |
| 12 | Mapa, inventário e pausa sem comandos involuntários | estados do jogo bloqueiam a entrada de jogabilidade (EditMode); clique que fecha menu é ignorado até soltar (código) | implementado; não testado por pessoa |
| 13 | Teclado/mouse ↔ controle com HUD correspondente | troca de ícones por dispositivo detectado; o roteiro força ícones de controle (visíveis nas capturas: Y, B, LB) | parcial: sem controle físico nesta máquina |
| 14 | Percurso de início a fim na build final | piloto automático | aprovado |
| 15 | Sequência de comparação e capturas | modo `-capture`; duas execuções comparadas pixel a pixel | aprovado |

## Desempenho

Relatórios `Logs/perf_referencia.json` e `Logs/perf_missao.json` da build final (1920×1080, qualidade Alta,
VSync ligado na configuração):

| Cena | Amostras | Quadro médio | p95 | p99 | Máximo | > 33 ms |
| --- | --- | --- | --- | --- | --- | --- |
| Arena de referência (roteiro, 20 s) | 6 521 | 2,79 ms | 2,82 ms | 2,84 ms | 72,2 ms (pico isolado, causa não identificada) | 1 |
| Percurso (piloto automático, 150 s) | 53 264 | 2,78 ms | 2,82 ms | 2,83 ms | 77,8 ms (pico isolado, causa não identificada) | 1 |

Ressalvas: a máquina é de topo, não intermediária; a taxa medida (~360 qps) mostra que o VSync não limitou o
quadro nestas execuções; o tempo de GPU informado pelo `FrameTimingManager` (≈0,33 ms) é baixo demais para ser confiável e não
deve ser usado como medida. A meta de 60 qps em hardware intermediário **não foi medida**.

## Limitações conhecidas

- **Fidelidade visual**: a reprodução é aproximada. Pendências medidas estão em
  [MATRIZ_VALIDACAO.md](MATRIZ_VALIDACAO.md) (enquadramento em alguns instantes, saturação do pulso rosa,
  fundo mais escuro em alguns quadros, modelos e animações mais simples que os do jogo de referência).
- **Duração do percurso**: o piloto automático conclui em ~4,6 min de tempo de jogo, jogando de forma direta.
  A duração para uma pessoa (meta de 10–15 min) não foi medida.
- **Sem teste humano** de sensação, controles físicos ou legibilidade; toda a verificação foi automatizada.
- **Determinismo**: na captura, passo fixo de 1/30 s, sementes e relógio dos efeitos fixos. Duas execuções
  nesta máquina deram posições idênticas e imagens iguais ou com diferenças de 1–2 níveis em poucos pixels;
  outra GPU ou outro driver podem arredondar de forma diferente, e física e navegação não garantem igualdade
  entre máquinas.
- **Não implementado**: cooperação, várias missões e geração modular ([EXTENSOES.md](EXTENSOES.md)).
