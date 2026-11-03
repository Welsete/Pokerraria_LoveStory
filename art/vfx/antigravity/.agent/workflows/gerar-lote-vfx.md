---
description: Gera o próximo lote de spritesheets de golpes do Pokerraria a partir da fila
---

1. Leia as regras em `.agent/rules/vfx-pokerraria.md` (ou `INSTRUCAO_GEMINI.md`) e siga em todas as imagens.
2. Abra `FILA_GERADOR.md`. Os golpes estão em ordem: comece pelas seções "Prioridade" e siga de cima para baixo.
   Pegue os próximos 10 golpes (ou quantos o Welsete pedir) que ainda não têm imagem em `vfx_spritesheets/`
   (procure o nome do golpe em snake_case em qualquer subpasta, ex.: `mega_punch.png`).
3. Para cada golpe, gere UMA imagem usando o prompt dele (o bloco `text` logo abaixo do título `### Nome`), sem mudar
   paleta, grade nem cor de fundo. Os tipos marcados "fundo verde #00FF00" usam fundo verde.
4. Confira cada imagem antes de salvar: grade 4x2 com quadros do mesmo tamanho, fundo de uma cor só (magenta ou
   verde, como pedido), nenhuma pessoa inteira desenhada, nenhum texto, efeito sem encostar nas bordas dos quadros,
   projéteis com o impacto nos 3 últimos quadros. Se falhar, gere de novo.
5. Salve em `vfx_spritesheets/vfx_spritesheets_loteN/` (N = próximo número de lote que ainda não existe), com o nome
   do golpe em inglês em snake_case: `mega_punch.png`, `vine_whip.png`, `conversion_2.png`.
6. Não edite `FILA_GERADOR.md`: o Claude atualiza a fila quando recebe o lote.
7. No fim, liste os golpes gerados e os que falharam, para o Welsete avisar o Claude.
