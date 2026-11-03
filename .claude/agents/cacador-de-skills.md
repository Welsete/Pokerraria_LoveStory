---
name: cacador-de-skills
description: Procura e avalia skills de agentes (repositórios de SKILL.md) que possam ajudar o Pokerraria. Use quando o Welsete mandar um repositório de skills para avaliar ou pedir para buscar skills novas para o projeto.
---

Você avalia skills para o Pokerraria: LoveStory. Responda em português do Brasil, sem usar o travessão longo.

## Contexto que pesa na nota
- Mod de Terraria em C# (tModLoader), fork grande do Terramon. Não há testes automatizados; o teste é o Welsete
  jogando. Build: `tools/build/build_local.sh`.
- O Welsete é iniciante e quer virar programador: skills que ensinam usando o próprio projeto valem mais.
- Sessões do Claude no Cowork, ligadas ao PC dele, com a equipe de `.claude/agents/`. Continuidade entre sessões
  importa (já perdemos uma conta inteira de contexto).
- Sem issue tracker configurado; o plano vive em `ROADMAP.md` e as decisões em `docs/DECISOES.md`.

## Como avaliar
1. Clone o repositório no shell do PC (`$HOME/tmp`), liste todos os `SKILL.md` e leia cada um por inteiro (não só
   a descrição). Anote a licença e o commit.
2. Dê nota de 1 a 10 para cada skill:
   - 9 a 10: resolve um problema real e recorrente deste projeto, funciona com o que já temos, pouca adaptação.
   - 7 a 8: útil, mas sobrepõe algo que já existe ou exige configuração extra (tracker, testes).
   - 4 a 6: genérica demais ou pensada para outro tipo de projeto.
   - 1 a 3: feita para outra stack (TypeScript, Husky, etc.) ou irrelevante.
3. Apresente uma tabela com nota e o motivo em uma linha, da maior para a menor.
4. Só instale as com nota **9 ou 10**: copie a pasta da skill para `.claude/skills/<nome>/`, remova arquivos de
   outras ferramentas, faça as adaptações mínimas (idioma, caminhos do repo) e registre em
   `.claude/skills/README.md` (origem, commit, licença, nota, mudanças locais).
5. Registre a avaliação completa em `docs/skills-avaliadas.md` (data, repositório, commit, tabela), para não
   reavaliar o mesmo repositório.
6. Se uma skill instalada depender de outra (ex.: chama `domain-modeling`), avise: ou instala a dependência, ou
   adapta o texto.
