# Skills avaliadas

Registro do agente `cacador-de-skills`. Entram no projeto só as com nota 9 ou 10.

## 2026-11-03 · [mattpocock/skills](https://github.com/mattpocock/skills) @ 4ae2317 (MIT)

| Nota | Skill | Motivo |
| --- | --- | --- |
| **9** | `handoff` | Resumo de fim de sessão para a próxima continuar. Perdemos uma conta inteira de contexto; isso evita repetir. **Instalada** (salvando em `docs/handoffs/`). |
| **9** | `diagnosing-bugs` | Disciplina para bug: montar um jeito de reproduzir, hipóteses, corrigir, limpar logs. Tem script para quando o teste precisa de humano (o Welsete no jogo). **Instalada**. |
| **9** | `teach` | Aulas curtas em HTML, com registro do que foi aprendido. Encaixa no objetivo do Welsete de virar programador usando o código do mod. **Instalada** (em `docs/aprendizado/`). |
| **9** | `writing-for-agents` | Guia para escrever CLAUDE.md, agentes e skills enxutos. Mantém a equipe do projeto afiada. **Instalada**. |
| 8 | `chief-of-staff` | Coordenar subagentes com uma wiki de memória. Ótima ideia, mas ainda "em progresso" e o `CLAUDE.md` + Projeto já cobrem boa parte. |
| 7 | `grilling` | Entrevista em rodadas com várias perguntas e resposta recomendada. Já temos o `grill-me-pokerraria`, e o Welsete prefere uma pergunta por vez. Dá para aproveitar a ideia de "resposta recomendada". |
| 7 | `code-review` | Revisão em dois eixos com subagentes. Boa, mas pede tracker configurado; o `qa-release` cobre o básico. |
| 7 | `retro` | Retrospectiva de sessão para melhorar o ambiente dos agentes. Útil mais para frente. |
| 7 | `domain-modeling` | Glossário e ADRs. Útil se o vocabulário do mod começar a confundir; `docs/DECISOES.md` cobre por enquanto. |
| 7 | `research` | Pesquisa em fontes primárias salva em Markdown. Pode servir para APIs do tModLoader. |
| 6 | `to-tickets` | Quebra planos em tickets. Exige `setup-matt-pocock-skills` e tracker. |
| 6 | `wayfinder` | Planejar trabalhos enormes por tickets de decisão. Exige tracker. |
| 6 | `grill-me`, `grill-with-docs` | Versões do grilling; mesma observação. |
| 6 | `to-questionnaire` | Questionário para outra pessoa responder (ex.: Isa). Pontual. |
| 6 | `wait-what` | Reexplicar quando a resposta não ficou clara. Pequena. |
| 6 | `prototype` | Protótipo descartável; pouco uso num mod. |
| 5 | `to-spec`, `implement`, `implement-spec`, `triage`, `pr` | Fluxo de tickets/PR; trabalhamos direto no `main`. |
| 5 | `setup-matt-pocock-skills` | Só serve para as skills de tracker acima. |
| 5 | `claude-handoff`, `wizard`, `git-guardrails-claude-code` | Agente em segundo plano; wizard bash; bloquear push (o fluxo precisa de push). |
| 5 | `codebase-design`, `improve-codebase-architecture` | Refatoração de arquitetura; arriscado num fork grande para quem está aprendendo. |
| 4 | `ask-matt`, `loop-me` | Roteador das skills dele; entrevista sobre workflows. |
| 3 | `tdd` | Não há como testar o mod automaticamente hoje. |
| 3 | `software-factory`, `scaffold-exercises`, `writing-beats`, `writing-fragments`, `writing-shape` | Outro tipo de trabalho (fábrica de software, cursos em TS, artigos). |
| 1 | `setup-pre-commit`, `migrate-to-shoehorn`, `setup-ts-deep-modules` | Ferramentas de TypeScript/JavaScript. |

## 2026-11-03 · [0x0funky/agent-sprite-forge](https://github.com/0x0funky/agent-sprite-forge) @ 5324342 (MIT)

Cinco skills de sprites 2D. Gerar imagem exige chave de API (OpenAI, Gemini, xAI, BytePlus ou fal.ai) ou o Codex/Grok
CLI logado; sem isso, sobra o processamento de sheets que já existem e a arte por código.

| Nota | Skill | Motivo |
| --- | --- | --- |
| **9** | `generate2dsprite` | Recorta, limpa e registra sheets de FX: keyer "soft matte" sem franja rosa, fundo verde/azul para artes roxas, QC de sheet, paleta. Substitui e melhora o nosso `convert.py`. **Pendente**: precisa de permissão do Welsete para rodar código de terceiros. |
| 8 | `generate2dmedia` | Gera a imagem direto pela API (com chave do Gemini, o Claude geraria as sheets sozinho, sem o Welsete copiar e colar). Dependência da anterior. Custa por imagem. |
| 7 | `codeart2d` | Arte por código com paleta exata e QA. Sobrepõe o nosso `draw_shadow_ball.py`, mas é mais completa. |
| 6 | `video2dsprite` | Anima personagens a partir de um still via vídeo (xAI/Grok). Útil se um dia voltarmos às caminhadas dos treinadores. |
| 3 | `generate2dmap` | Mapas para Tiled, Godot e LDtk; no Terraria usamos o Blueprint Builder. |

Aproveitado já, sem instalar: a regra de usar fundo verde em artes roxas e rosas. O teste deu 72% da arte de
Fantasma perdida no fundo magenta contra 7% (só a borda do JPEG) no verde. `convert.py` ganhou `GREEN_KEY` e os
prompts de Fantasma, Psíquico, Venenoso, Sombrio e Fada passaram a pedir `#00FF00`.
