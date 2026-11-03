# Pokerraria · instruções para o Antigravity

Esta pasta guarda a arte do mod Pokerraria: LoveStory. O Antigravity gera as spritesheets de VFX dos golpes; o Claude
(no repositório github.com/Welsete/Pokerraria_LoveStory) recorta, anima e coloca no jogo.

- Regras de toda imagem de golpe: `.agent/rules/vfx-pokerraria.md` (mesmo texto de `INSTRUCAO_GEMINI.md`).
- Para gerar um lote: workflow `/gerar-lote-vfx` (`.agent/workflows/gerar-lote-vfx.md`).
- Fila com os prompts, em ordem: `FILA_GERADOR.md`. Só o Claude edita esse arquivo.
- Imagens prontas: `vfx_spritesheets/vfx_spritesheets_loteN/<golpe_em_snake_case>.png`.
- Não mexa em `github_token.txt`, `build/` nem `painel/`.
