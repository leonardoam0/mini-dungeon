# Decisões técnicas e de reprodução

Classificação usada (não aparece no jogo):

| Etiqueta | Significado |
| --- | --- |
| **OBSERVADO** | visto diretamente nos quadros do vídeo de referência (540×960, 30 qps, 14,6 s) |
| **CONFIRMADO_POR_FONTE** | afirmado nas fontes públicas listadas no documento de especificação (fundamentos do jogo original) |
| **ESTIMADO** | valor derivado de medições no vídeo com hipóteses explícitas; não é valor oficial de Minecraft Dungeons |
| **PROPOSTO** | decisão de projeto sem evidência direta no vídeo |

Nenhum valor de câmera, velocidade, dano, recarga, hitbox ou iluminação deste projeto é apresentado como valor
oficial do jogo original.

## 1. Plataforma

| Decisão | Classe |
| --- | --- |
| Unity 6000.6.0f1, URP 17.6 (Forward+, SSAO, sombras suaves), Input System 1.20, AI Navigation 2.0.14, uGUI 2.6; Windows x64; offline | PROPOSTO (stack recomendada pela especificação para projeto novo) |
| Conteúdo gerado por código (pipeline "Forge"): texturas pixeladas, atlas, malhas voxel com AO por vértice, personagens por peças rígidas, poses, efeitos, sons sintetizados, fonte pixelada, níveis e cenas | PROPOSTO (não havia artista nem assets autorizados) |
| Shader próprio `Ruinas/VoxelLit` (cor por vértice, AO, névoa de altura, recorte pontilhado do cenário entre câmera e jogador) | PROPOSTO |

## 2. Material de referência e método

- Vídeo com 438 quadros; o marcador do jogador no mapa (losango branco) foi detectado em 415 quadros sempre em
  (269,5; 479,4) ± 1 px. **OBSERVADO** — a câmera mantém o jogador centralizado.
- Quadros de referência `Docs/Referencia/quadros/f_NNN.png` = quadro 15·(NNN−1), isto é, 0,0 s, 0,5 s, …, 14,5 s.
  Uma primeira extração com o filtro `fps` do ffmpeg estava deslocada em +0,23 s; foi refeita pelo índice exato.
- Deslocamento da câmera medido por fluxo óptico denso (Farneback) em regiões fora do mapa sobreposto e do HUD,
  conferido por SIFT+RANSAC em intervalos longos, e ancorado na posição do topo do obelisco detectado em ~60
  quadros (topo branco e, depois de ~11,7 s, verde). **ESTIMADO**; erro residual típico de ~1 u.
- Linha do tempo de efeitos lida em folhas de quadros a cada 0,1 s. **OBSERVADO**.

## 3. Câmera e projeção

| Decisão | Valor | Classe |
| --- | --- | --- |
| Perspectiva elevada em três quartos, orientação fixa | — | OBSERVADO |
| Guinada | 45° | ESTIMADO (bordas do contorno com inclinações simétricas) |
| Inclinação | 45° | ESTIMADO (inclinação das bordas 0,57–0,80 no quadro, com perspectiva) |
| Distância / FOV | 46 u / 28° | ESTIMADO: escala de ~42,3 px/u no recorte 540×960 (≈47,6 px/u em 1080p), obtida da diagonal do contorno (508 px) com arena de 12 u |
| Alvo da câmera | pés do jogador, sem antecipação | ESTIMADO a partir do marcador fixo no centro |
| Suavização | 0,06 s (plano), 0,08 s (altura) | PROPOSTO (sem atraso perceptível no vídeo) |
| Recorte de comparação | 608×1080 central de 1920×1080 → 540×960 | OBSERVADO (vídeo é recorte vertical) + PROPOSTO (a apresentação normal do jogo é 16:9) |

## 4. Escala e arena

| Decisão | Valor | Classe |
| --- | --- | --- |
| Plataforma quadrada com contorno luminoso, escadas, portão, pilares, obelisco | — | OBSERVADO |
| Interior do contorno | 12 × 12 u | ESTIMADO |
| Ladrilhos do piso | ~0,4 u (5 por 2 blocos) | ESTIMADO: período das juntas de ~13 px (FFT de trechos do piso) |
| Obelisco | 1,2 u de largura, ~5,1 u até a face superior, corpo de tijolos avermelhados, faixa escura com dois sinais por face, bloco luminoso no topo | ESTIMADO (larguras de 65–75 px; altura pela posição do topo em quadros ancorados) |
| Posição do obelisco | 1,5 u a nordeste do centro da arena | ESTIMADO (canto NE a ~(4–5, 4–5) u do obelisco) |
| Escada oeste | largura 6, descendo para oeste ao sul do canto noroeste | ESTIMADO pelo trajeto (7,0 s e 14,0 s no alto da escada) |
| Pilar alto de tijolos com copa vermelha e braseiro no canto noroeste | pilar 2×2 até 4 blocos acima do piso | ESTIMADO (aparece aos 1,5–2,5 s, 7–8 s e 13,9–14,5 s) |
| Portão de bambu com campo violeta no lado leste; muro norte baixo com pilares; lanternas verdes em pilares | — | OBSERVADO |
| Fundo de ruínas de pedra azul-petróleo com pouca vegetação | — | OBSERVADO (qualitativo) |
| Altura do herói ~1,9 u (modelo por peças de 0,06 u por texel) | — | PROPOSTO (no vídeo ~48 px ⇒ 1,6–1,9 u, conforme a escala estimada) |

## 5. Materiais e cor

| Decisão | Classe |
| --- | --- |
| Piso terracota com juntas escuras (base #a86b55): medianas do piso no vídeo ≈ (175–195, 113–122, 82–93) | ESTIMADO |
| Paredes e degraus de pedra azul-petróleo, musgo concentrado embaixo, bordas mais claras | OBSERVADO (qualitativo) |
| 32 px por bloco, macro-tiles de 2×2 blocos no piso, atlas 1024² com margem contra sangramento | PROPOSTO |
| Personagens com leve emissão do próprio albedo (destacam-se da noite azulada, como no vídeo) | PROPOSTO (efeito observado, técnica proposta) |

## 6. Iluminação e pós-processamento

| Decisão | Classe |
| --- | --- |
| Luz direcional quase neutra (0,93; 0,90; 0,86), intensidade 1, sombras suaves; ambiente trilight azulado; névoa azul-petróleo | ESTIMADO (comparação de médias por região entre capturas e vídeo) |
| Braseiros com luz quente e cintilante; contorno com luz ciano; lanternas verdes fracas | OBSERVADO (fontes) + ESTIMADO (intensidades) |
| Bloom, tonemapping neutro, SSAO, *split toning* suave, vinheta leve | PROPOSTO |
| Exposição +0,5 EV e balanço de branco −7 (mais frio) | ESTIMADO (capturas ~17% mais escuras e mais quentes que o vídeo no centro da arena antes do ajuste) |
| Profundidade de campo gaussiana apenas no fundo (50–68 u da câmera) | OBSERVADO (o alto do quadro no vídeo é desfocado) / PROPOSTO (técnica) |
| Registro de calibração: um *split toning* laranja forte deslocava o ciano do contorno para verde-limão; foi suavizado | ESTIMADO (medido nas capturas) |
| SSAO pelo método de ruído fixo por pixel (*Interleaved Gradient*) em vez do *Blue Noise*, que troca o ruído a cada quadro | PROPOSTO (sem TAA a troca por quadro cintila; também deixa as capturas repetíveis) |

## 7. Trecho de referência (roteiro de 14,6 s)

Trajetória do jogador: 147 pontos (0,1 s) medidos no vídeo e seguidos pelo motor normal do jogador (velocidade
prevista + correção proporcional do erro; sem teleporte). Velocidade de demonstração 9,2 u/s (ESTIMADO: 5–8 u/s
nos trechos de caminhada, picos de 9–10 u/s e saltos de 13–23 u/s em 1,1–1,4 s e 6,4–6,8 s; a folga permite
recuperar o atraso após golpes). O pulso trava o herói só por 0,08 s e o salto da pena alcança 6,5 u
(PROPOSTO, coerente com o herói que continua andando no vídeo).

| Instante | Evento no roteiro | Classe |
| --- | --- | --- |
| 0,0 s | arena já ativa; invocação ativa (slot Y destacado o trecho todo) | OBSERVADO |
| 0,28–0,6 s | colunas de fogo; orbe violeta no topo do obelisco | OBSERVADO |
| 0,5–1,1 s | feixe magenta do obelisco | OBSERVADO |
| 0,45 s → ~0,7 s | pulso rosado centrado no jogador (sino) que se desfaz em bolhas lima/amarelas; nuvens verdes a seguir | OBSERVADO (efeito) / PROPOSTO (autoria: o artefato do jogador) |
| ~1,1 s | pena: salto de ~5 u para noroeste (slot B em recarga até ~4,2 s ⇒ recarga ≈ 3 s) | OBSERVADO / ESTIMADO |
| 2,15–2,65 s | golpes corpo a corpo junto ao pilar vermelho | OBSERVADO |
| 5,55–6,4 s | nova coluna de fogo, orbe violeta e feixe magenta | OBSERVADO |
| 6,4 s | esquiva rápida para oeste | ESTIMADO (arranque de velocidade) |
| 6,85–7,2 s | explosão de fogo com anel | OBSERVADO |
| 8,3 s | contorno muda de ciano para verde-limão | OBSERVADO |
| 10,95 s / 11,85 s | obelisco passa a resolvido (topo verde) com faíscas ciano subindo | OBSERVADO |
| 11,9 s | clarão branco ascendente além do canto nordeste | OBSERVADO |
| 13,0 s | poção de rapidez no alto da escada; dica "Pegar [A]" visível em 13,9–14,1 s | OBSERVADO (texto próprio em português) |

Não afirmado: que o obelisco ataque, que o feixe cause dano, que o portão abra por algum gatilho específico, a
identidade exata das criaturas e equipamentos. No jogo, o feixe é apenas visual (PROPOSTO).

Captura determinística (PROPOSTO): no modo `-capture`, as partículas recebem sementes fixas (pela posição na
cena ou pela ordem de criação no roteiro), as animações visuais e os shaders de efeito usam um relógio contado
em passos desde o início do roteiro, o gerador global é reiniciado e a interface anima com passo fixo. Fora da
captura as sementes continuam automáticas, para haver variedade no jogo.

## 8. HUD e mapa

| Decisão | Classe |
| --- | --- |
| Slots de artefato X/Y/B à esquerda (X cortado no recorte), coração grande no centro, poção (LB) e mapa (direcional) à direita, vidas "3", "LV 88" com barra roxa | OBSERVADO |
| Geometria em 1080p: slots de 92 px com base a 54 px do rodapé; centros −340/−242/−132 e +133/+236; coração 152×132 | ESTIMADO (medição do recorte, ×1,125) |
| Coração anguloso (lóbulos de topo plano, entalhe em V) | OBSERVADO |
| Ícones de controle durante o roteiro; ícones do dispositivo em uso fora dele | OBSERVADO / PROPOSTO |
| Números de dano: brancos para golpes (147, 400 no vídeo), vermelhos para dano de área/artefato (800) | OBSERVADO (estilo) / ESTIMADO (magnitudes da demonstração) |
| Mapa sobreposto: contornos creme finos, escala 0,38 do mundo, centrado no jogador, ícones de baú e portão | OBSERVADO (aparência) / ESTIMADO (escala) |
| Nível 88 e vida 420 são estado de demonstração do modo de referência, não da campanha | OBSERVADO (nível) / PROPOSTO (separação) |

## 9. Jogabilidade (Entrega B e sistemas)

Todos **PROPOSTO**, com fundamentos gerais do gênero **CONFIRMADO_POR_FONTE** (combate corpo a corpo e à
distância, equipamentos, encantamentos, artefatos com efeitos próprios):

- Combate: fluxo único de dano (`CombatResolver`) com registro evento×alvo (um dano por evento), varredura da
  arma no tempo, linha de visão contra o cenário, crítico, armadura, empurrão, pausa de impacto.
- Esquiva com recarga, poção com recarga de 25 s e cura de 60%, flechas como munição.
- Equipamentos: 4 armas corpo a corpo, 2 à distância, 2 armaduras, 3 artefatos; 9 encantamentos; 8 status
  (veneno, queimadura, lentidão, atordoamento, vulnerável, pressa, força, regeneração) com políticas de acúmulo.
- Inimigos: Carniçal Musgoso (perseguidor), Arqueiro Ossudo (distância), Vinha Esporífera (área), Guardião
  Musgoso (elite); vagas de ataque para não cercar o jogador de uma vez; companheiro lhama (invocação).
- Encontros com máquina de estados (dormente → ativando → ativo → resolvendo → concluído), portões, recompensa
  única e restauração pelo save.
- Percurso de 8 áreas com 5 pontos de retorno, 4 baús, desvio opcional, mercador, encontro final e portal de saída.
- Consumíveis no chão pegos com o botão de interação (no controle, o botão de ataque é contextual).
- Save JSON versionado (v2) com migração da v1, validação/reparo, gravação atômica e cópia de segurança.
- Encontros escalonados em ondas (corredor 2, sala 3, desvio 2, arena 4, salão 4 com o guardião): 58 inimigos no
  percurso; o piloto automático conclui em ~4,6 min de tempo de jogo; a duração humana não foi medida.
- Execuções automatizadas bloqueiam a entrada real (mapas de ação e navegação da interface), para resultados
  repetíveis mesmo com a janela em primeiro plano.

## 10. Áudio

Efeitos, ambiência e músicas sintetizados em código (`AudioForge`). **PROPOSTO**: a especificação observa que os
sons individuais do vídeo não foram identificados de forma confiável.
