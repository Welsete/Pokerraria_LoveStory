# Catálogo Mestre de VFX Sprite Sheets (Estilo Terraria)

Este repositório contém a especificação técnica e os prompts padronizados para geração de spritesheets de efeitos visuais (VFX) no estilo **Terraria 16-bit / 8-bit arcade** para todos os ataques de Pokémon.

---

## 📐 Diretrizes Universais de Criação

### 1. Duração e Contagem de Frames por Categoria
* **Impactos pequenos:** 5–8 frames.
* **Projéteis elementais:** 8–12 frames em loop, se necessário.
* **Explosões:** 8–14 frames.
* **Raios e feixes:** Animação curta de entrada (1-2f), duração controlada (3-5f) e saída rápida (2-3f).
* **Golpes especiais:** 15–24 frames, divididos em etapas (carga, clímax, impacto, dispersão).

### 2. Estética & Regras de Pixel Art (Terraria Look)
* **Silhuetas Legíveis:** Formas robustas, sem ruído excessivo de partículas. O núcleo do efeito tem prioridade sobre partículas soltas.
* **Contorno:** 1px em tom mais escuro na forma principal para garantir destaque contra qualquer fundo.
* **Sem Gradientes Suaves:** Apenas preenchimentos sólidos (flat fills) em 3 a 4 tons por volume. Sem borrão (blur) e sem luz difusa automática (glow artificial).
* **Fundo Chroma Key:** Sempre sólido `#FF00FF` (Magenta puro), permitindo recorte instantâneo no motor do jogo.
* **Escala de Pixel:** Proporção 1:1 estrita (sem mix de tamanhos de pixel).

---

## 🎨 Paletas Oficiais de Referência (6 Tons por Tipo)

| Tipo | Núcleo (White/Light) | Tom Alto | Tom Médio | Tom Base | Tom Escuro | Contorno (Darkest) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **FOGO** | `#FFF6C8` | `#FFD84A` | `#FF9A1F` | `#E8480F` | `#A3200E` | `#4A1010` |
| **ÁGUA** | `#FFFFFF` | `#C6F5FF` | `#63D2FF` | `#1E88E5` | `#0D47A1` | `#071F4F` |
| **PLANTA** | `#FFFFFF` | `#FFF494` | `#A7F432` | `#46C328` | `#1B7A22` | `#0C3F15` |
| **ELÉTRICO** | `#FFFFFF` | `#FFFF7A` | `#FFD700` | `#FFA500` | `#D97706` | `#592700` |
| **GELO** | `#FFFFFF` | `#E0F7FA` | `#80DEEA` | `#26C6DA` | `#00838F` | `#004D40` |
| **PSÍQUICO** | `#FFFFFF` | `#FFE4F2` | `#F472B6` | `#DB2777` | `#831843` | `#4A044E` |
| **FANTASMA** | `#FFFFFF` | `#E9D5FF` | `#A855F7` | `#7E22CE` | `#4C1D95` | `#240046` |
| **SOMBRIO** | `#F3E8FF` | `#9333EA` | `#4B1058` | `#2D0036` | `#18001E` | `#0A000D` |
| **DRAGÃO** | `#FFFFFF` | `#A5B4FC` | `#6366F1` | `#4338CA` | `#251F72` | `#130D3E` |
| **AÇO** | `#FFFFFF` | `#F1F5F9` | `#CBD5E1` | `#64748B` | `#334155` | `#0F172A` |
| **TERRA** | `#FFFBEB` | `#FDE68A` | `#D97706` | `#92400E` | `#582306` | `#2D1002` |
| **PEDRA** | `#F8FAFC` | `#E2E8F0` | `#94A3B8` | `#64748B` | `#475569` | `#1E293B` |
| **VENENO** | `#FFFFFF` | `#F5D0FE` | `#D946EF` | `#9333EA` | `#581C87` | `#2E0854` |
| **INSETO** | `#FEF08A` | `#BEF264` | `#84CC16` | `#4D7C0F` | `#365314` | `#1A2E05` |
| **VOADOR** | `#FFFFFF` | `#F0FDF4` | `#BBF7D0` | `#6EE7B7` | `#14B8A6` | `#0F766E` |
| **LUTADOR** | `#FFF1F2` | `#FECDD3` | `#FB7185` | `#E11D48` | `#9F1239` | `#4C0519` |
| **FADA** | `#FFFFFF` | `#FDF2F8` | `#F472B6` | `#EC4899` | `#9D174D` | `#500724` |
| **NORMAL** | `#FFFFFF` | `#F5F5F5` | `#D4D4D4` | `#A3A3A3` | `#525252` | `#171717` |

---

## 📁 Arquivos de Ataques por Tipo

1. [Fogo (19 ataques)](./fogo.md)
2. [Água (22 ataques)](./agua.md)
3. [Planta (27 ataques)](./planta.md)
4. [Elétrico (18 ataques)](./eletrico.md)
5. [Gelo (15 ataques)](./gelo.md)
6. [Dragão (9 ataques)](./dragao.md)
7. [Fantasma (12 ataques)](./fantasma.md)
8. [Sombrio (19 ataques)](./sombrio.md)
9. [Aço (11 ataques)](./aco.md)
10. [Terrestre (15 ataques)](./terrestre.md)
11. [Pedra (13 ataques)](./pedra.md)
12. [Venenoso (21 ataques)](./venenoso.md)
13. [Inseto (13 ataques)](./inseto.md)
14. [Voador (17 ataques)](./voador.md)
15. [Lutador (29 ataques)](./lutador.md)
16. [Fada (9 ataques)](./fada.md)
17. [Psíquico (31 ataques)](./psiquico.md)
18. Normal (116 ataques): [físicos](./normal_fisico.md), [especiais](./normal_especial.md) e [de status](./normal_status.md)
