# Matriz de validação visual (seção 29)

Método: a build final roda a arena de referência com `-reference -capture` (passo fixo de 1/30 s; semente,
roteiro, sementes das partículas e relógio dos efeitos fixos) e captura 17 instantes. Cada captura (recorte
central 608×1080 → 540×960) é comparada com o quadro exato do vídeo (índice = instante × 30) por
`Tools/compare_reference.py`, que gera o lado a lado vídeo × jogo × diferença e indicadores por região. A projeção exata do topo do obelisco e a posição do herói
são registradas em cada captura (`Docs/Capturas/captura_log.txt`).

Material:

- `Docs/Capturas/pares_video_jogo.jpg` — todos os instantes (vídeo acima, jogo abaixo)
- `Docs/Capturas/comparacao/cmp_XX.Xs.jpg` — vídeo | jogo | diferença ×2, por instante
- `Docs/Capturas/comparacao/comparacao.md` — tabela de marcos e indicadores por região
- `Docs/Capturas/jogo/` — capturas originais (9:16 e algumas 16:9)
- `Docs/Gravacoes/referencia.mp4` — o trecho completo na build (16:9)

Os números abaixo orientam a calibração; **não são porcentagem de fidelidade**.

## Resultado por critério

| Critério | Como foi avaliado | Situação medida | Desvios restantes |
| --- | --- | --- | --- |
| Projeção | inclinação das bordas do contorno por detecção de linhas; convergência | câmera 45°/45°, FOV 28°; inclinações das bordas no jogo 0,63–0,87 × vídeo 0,57–0,80, com a mesma assimetria de perspectiva | diferença pequena de convergência nas bordas mais próximas |
| Enquadramento | topo do obelisco: posição no vídeo (detectada e conferida) × projeção exata no jogo, 7 instantes | **mediana 2,1%** da altura do quadro; ≤2,9% em 5 de 7 instantes; máximo 5,1% (4,0 s) e 4,0% (2,5 s) | atrasos do herói no trajeto após golpes |
| Escala | largura do topo do obelisco; período das juntas do piso; altura do herói | obelisco 70 × 75 px (−7%); ladrilho ~0,4 u (período de ~13 px no vídeo); herói ~50 × ~48 px | herói ~5% maior; obelisco ligeiramente mais estreito |
| Luz | luminância e cor médias por região (quadro, centro, topo, faixa do HUD) | razão de luminância jogo/vídeo (mediana): quadro 1,07; centro 1,08; topo 0,91; balanço do centro B/R 0,73 × 0,80 e G/R 0,93 × 0,95; correlação das massas claras/escuras 0,51–0,79 (mediana 0,63) | topo mais escuro em alguns instantes (razão 0,61 aos 7,0 s; 0,63 aos 0,3 s, quando a coluna de fogo do vídeo ilumina o alto); centro mais claro na explosão aos 7,0 s (1,46) |
| Materiais | inspeção lado a lado ampliada | piso terracota com juntas escuras e ladrilhos de ~0,4 u; pedra azul-petróleo; AO por vértice; pixels nítidos a 32 px por bloco | o vídeo é mais suave (compressão e desfoque); o topo desfocado foi aproximado com profundidade de campo gaussiana |
| Movimento | posição do herói em cada captura × trajetória medida no vídeo | erro **mediano 0,42 u**; p90 1,54 u; máximo 4,08 u aos 7,0 s (a esquiva cobre 3,2 u, o arranque do vídeo ~6 u; recuperado aos 8,0 s com 0,1 u) | arranques mais rápidos que a esquiva; golpes prendem o herói por ~0,5 s |
| Efeitos | início e duração por quadro (0,1 s); tamanho por inspeção | colunas de fogo 0,28 s e 5,55 s; orbe violeta 0,3 s e 5,58 s; feixe 0,5–1,1 s e 5,8–6,4 s; pulso no auge em ~0,7 s; nuvens; explosão 6,85 s; troca do contorno 8,3 s; topo verde 10,95–11,85 s; faíscas ciano; clarão 11,9 s; poção e dica 13,0/13,9 s | pulso rosa mais saturado e opaco que o do vídeo; formas das chamas e da explosão aproximadas |
| HUD | posições medidas no recorte e convertidas para 1080p; inspeção ampliada | slots, coração anguloso, poção, mapa, vidas, nível e barra roxa nas posições medidas; ícones de controle X/Y/B/LB e direcional; números de dano brancos e vermelhos | faixa do HUD ~23% mais clara (fundo do vídeo mais escuro); tipografia própria |
| Mapa | inspeção lado a lado durante o acompanhamento | contornos creme finos em escala 0,38, centrados no herói, com baú e portão; preenchimento quase transparente | contorno depende da área explorada no instante, que difere do vídeo |

## Três maiores desvios corrigidos nesta rodada (ordem de impacto)

1. **Enquadramento e trajeto**: câmera centrada nos pés (o marcador do jogador no vídeo fica fixo no centro),
   trajetória do herói medida por fluxo óptico e ancorada no obelisco, layout ajustado (escada oeste mais ao
   norte, pilar vermelho no canto noroeste, obelisco alto). Na primeira captura o herói e o obelisco estavam em
   outra região do quadro; agora a mediana do desvio do topo do obelisco é 2,1% da altura.
2. **Luz e cor**: piso terracota em vez de verde-azulado, luz principal quase neutra, névoa e ambiente
   azul-petróleo, tonalização suave (o ciano do contorno deixou de virar verde-limão), exposição e balanço de
   branco. No centro da arena, G/R passou de 1,5–1,9 (verde dominante na primeira captura) para 0,93 × 0,95 do
   vídeo.
3. **Tempo dos eventos**: quadros de referência reextraídos no instante exato (a extração anterior estava
   deslocada em +0,23 s) e linha do tempo de efeitos refeita a cada 0,1 s.

## Repetibilidade

Medida com duas execuções de `-reference -capture` da build final nesta máquina (`Logs/det3_025026_1` e `_2`;
a primeira é a que está em `Docs/Capturas`):

- posição do herói e projeção do topo do obelisco **idênticas** nos 17 instantes (também iguais às de 7
  execuções de 4 builds anteriores);
- **22 das 36 imagens idênticas bit a bit**; nas outras 14, no máximo 11 pixels diferentes por imagem, com
  diferença de 1 a 2 níveis (de 255) — 60 pixels no total, em cerca de 46,7 milhões.

Na primeira medição as posições já coincidiam, mas 0,4–9,3% dos pixels diferiam mais de 8 níveis entre
execuções. Causas encontradas e corrigidas no modo de captura:

- sementes automáticas das partículas (nuvens, faíscas, vaga-lumes) e dos arcos do pulso;
- oscilação das luzes, pulsos de brilho e rolagem dos shaders de efeito atrelados ao relógio absoluto, que
  inclui o carregamento;
- animações de interface, revelação do mapa e altura da névoa com passo de tempo real;
- ruído do SSAO trocado a cada quadro renderizado (passou ao método de ruído fixo por pixel, também fora da
  captura).

Limite: a igualdade foi medida nesta máquina e nesta build. Outra GPU ou outro driver podem arredondar de
forma diferente, e a física e a navegação do Unity não garantem resultados idênticos entre máquinas.
