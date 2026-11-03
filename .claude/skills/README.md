# Skills do projeto

Skills que o Claude usa ao trabalhar no Pokerraria. Avaliadas pelo agente `cacador-de-skills`; só entram as com nota
9 ou 10. Avaliações completas em `docs/skills-avaliadas.md`.

| Skill | Origem | Nota | Para quê |
| --- | --- | --- | --- |
| `handoff` | [mattpocock/skills](https://github.com/mattpocock/skills) @ 4ae2317 (MIT) | 9 | Fechar uma sessão deixando um resumo em `docs/handoffs/` para a próxima continuar |
| `diagnosing-bugs` | mattpocock/skills @ 4ae2317 (MIT) | 9 | Bug relatado no teste: reproduzir, levantar hipóteses, corrigir e limpar |
| `teach` | mattpocock/skills @ 4ae2317 (MIT) | 9 | Ensinar programação ao Welsete usando o código do próprio mod, em `docs/aprendizado/` |
| `writing-for-agents` | mattpocock/skills @ 4ae2317 (MIT) | 9 | Escrever ou revisar CLAUDE.md, agentes e skills sem inchar o contexto |
| `generate2dsprite` | [0x0funky/agent-sprite-forge](https://github.com/0x0funky/agent-sprite-forge) @ 5324342 (MIT) | 9 | Recorte e limpeza de spritesheets. O `tools/vfx/convert.py` usa o keyer dele (`forge_matte.py`) |
| `generate2dmedia` | agent-sprite-forge @ 5324342 (MIT) | 8 | Dependência da anterior; gera imagem por API se um dia houver chave (instalada só por isso) |

Mudanças locais: `handoff` salva no repo (não na pasta temporária); `teach` usa `docs/aprendizado/` e as regras
didáticas do Welsete. A pasta `agents/` (config do Codex) foi removida.

Licença MIT, Copyright (c) 2026 Matt Pocock.
