# Inventário de assets e origens

**Nenhum asset externo** (modelo, textura, som, fonte, animação ou código de terceiros) foi incorporado ao jogo.
Tudo o que a build usa é produzido por código deste projeto, na pipeline reproduzível `Assets/_Project/Scripts/Editor/Forge`
(menu **Ruinas → Regenerar tudo**). Pacotes Unity usados são os oficiais listados em `Packages/manifest.json`.

| Categoria | Gerador (Scripts/Editor/Forge) | Saída | Origem |
| --- | --- | --- | --- |
| Texturas do mundo (32 px por bloco, atlas 1024² com margem) | `WorldTexturePainter`, `TextureForge`, `PixelCanvas` | `Art/Textures/World` | autoral, procedural |
| Peles dos personagens (layout de caixas estilo "box UV") | `SkinPainter` | `Art/Textures/Characters` | autoral, procedural |
| Ícones de itens, artefatos, HUD, coração, mapa | `IconPainter` (grades ASCII e formas) | `Art/Textures/UI` | autoral |
| Fonte pixelada (acentos do português) | `FontForge` | `Data/UI/PixelFont` | autoral |
| Malhas de cenário (voxel com AO por vértice) | `LevelBuilder`, `VoxelMesher` (runtime) | geradas ao montar as cenas | autoral |
| Adereços (obelisco, braseiros, portão, baús, lanternas, flora, acampamento) | `PropForge`, `MeshForge` | `Prefabs/Props`, `Art/Meshes/Props` | autoral |
| Itens 3D (sprites extrudados) | `MeshForge.ExtrudeSprite`, `DataForge` | `Art/Meshes/Items` | autoral |
| Personagens (peças rígidas) e poses/animações procedurais | `CharacterForge`, `PoseForge`, `ProceduralAnimator` | `Prefabs/Characters`, `Data/Poses` | autoral |
| Efeitos visuais (partículas cúbicas, feixes, esferas, anéis) | `VfxForge` + comportamentos em `Runtime/VFX` | `Prefabs/VFX` | autoral |
| Shaders | escritos à mão | `Art/Shaders` (VoxelLit, GlowAdditive, GlowOpaque, FresnelBubble, GroundRing, ParticleCube) | autoral |
| Sons, ambiência e músicas (WAV sintetizado) | `Synth`, `AudioForge` | `Audio` | autoral, síntese |
| Materiais e perfis URP (3 níveis de qualidade) | `MaterialForge`, `ProjectSetup` | `Settings/URP`, `Art/Materials` | autoral |
| Dados (itens, inimigos, status, encantamentos, encontros, roteiro de referência) | `DataForge` | `Data` | autoral |
| Cenas e NavMesh | `LevelLayouts`, `LevelAssembler`, `SceneForge` | `Scenes` | autoral |
| Ações de entrada | gerado por script | `Settings/Input/RuinasControls.inputactions` | autoral |

## Material de referência (não distribuído no jogo)

| Arquivo | Origem | Uso |
| --- | --- | --- |
| `Docs/Referencia/quadros/f_001.png` … `f_030.png` | quadros do vídeo `e35e8c0df830f548e9eb01ee26b402f6_t1.mp4` fornecido pelo usuário (índices 0, 15, …, 435) | comparação visual e sobreposição F7 no modo de referência |

Os quadros ficam fora de `Assets` e não entram na build; a sobreposição F7 lê a pasta em disco quando existe.
Os textos da interface são próprios, em português (por exemplo, a dica "Poção de Rapidez — Pegar [A]" reproduz
a função da dica vista no vídeo, não o texto original).

## Provisórios declarados

- Animações são poses procedurais sobre peças rígidas (sem captura de movimento nem rig de ossos).
- Áudio sintetizado: timbres simples, sem gravações.
- Número e variedade de modelos de inimigos menores que os do jogo de referência.
