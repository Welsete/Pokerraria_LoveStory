# Masmorras do Brock e da Misty

Decisão do grill-me (2026-11-03): os Ginásios do Brock e da Misty ganham masmorras curtas (3 ou 4 salas) antes do
líder, cada uma com uma página do diário num baú. Brock: só uma alavanca abre, as outras soltam pedras. Misty: comportas
que mudam o nível da água. Este documento é a **proposta** de como fazer; nada foi construído ainda.

Pontos marcados com **(?)** são escolhas do Welsete.

## Como as duas são feitas (técnico)

- Geradas junto com o Ginásio, entre a entrada e o salão do líder: quem quer a insígnia passa pela masmorra.
- Tudo com a fiação vermelha do próprio Terraria (alavancas, placas de pressão, atuadores, armadilhas de pedra), igual
  às armadilhas que o Ginásio do Brock já tem. Nada de mecânica nova em C#: fica estável e funciona no multiplayer.
- Blocos protegidos (`ProtectedAreas`), para ninguém atravessar a masmorra cavando.
- Mundos antigos: a masmorra só aparece em mundos novos; nos antigos, o baú com a página vai direto para dentro do
  Ginásio (para ninguém ficar sem a página).
- Construção pelo Blueprint Builder (como o Ginásio da Misty) ou por código (como o do Brock). **(?)** Se o Welsete quiser
  desenhar as salas no Blueprint Builder, eu ligo a fiação e as armadilhas por código depois.

## Brock: "Pedra por pedra" (dentro da caverna, entre a corda e o salão)

```
 corda
   |
 [1 Entrada] -> [2 Sala das alavancas] -> [3 Corredor das pedras] -> [4 Baú da página] -> salão do Brock
```

1. **Entrada:** túnel curto com tochas e uma placa: "Nem toda alavanca diz a verdade." (eco da página do Brock).
2. **Sala das alavancas:** uma porta de pedra (blocos com atuador) e **3 alavancas**. Só uma abre a porta; as outras duas
   soltam pedregulhos do teto (armadilha de pedra do Terraria). A certa muda de lugar a cada mundo.
   **(?)** Uma pista visual para a certa: a única com uma tocha ao lado, igual à ilustração do diário (a alavanca que
   brilha). Assim quem leu a página tem vantagem.
3. **Corredor das pedras:** placas de pressão escondidas no chão que soltam pedras do teto; dá para pular por cima.
4. **Câmara do baú:** um baú de pedra com a **página do Brock** e alguns itens úteis (poções, Poké Bolas). **(?)** Os itens.

Tamanho: umas 60 x 20 tiles a mais. A entrada do salão do Brock passa a ser no fim da masmorra.

## Misty: "A água sobe e desce" (embaixo do deck da praia)

```
 deck do Ginásio
   |  alçapão
 [1 Câmara alagada] -> [2 Comportas] -> [3 Poço fundo] -> [4 Baú da página]
```

1. **Câmara alagada:** desce por um alçapão no deck. A sala está cheia de água até o teto: precisa nadar (ou usar
   poção de guelras) até a primeira alavanca.
2. **Comportas:** duas salas lado a lado separadas por paredes com atuador. A alavanca abre a comporta e a água passa de
   uma sala para a outra: o nível baixa onde você está e sobe na vizinha. O jogo é abrir na ordem certa para secar o
   caminho.
3. **Poço fundo:** um poço onde a água sobe e desce (comporta aberta/fechada) e as plataformas só ficam ao alcance em
   um dos níveis.
4. **Câmara do baú:** baú de coral com a **página da Misty**, debaixo do aquário. **(?)** Uma janela de vidro para o
   aquário do Ginásio, para dar o clima.

Cuidado: no Terraria a água se espalha sozinha; a masmorra é fechada (paredes protegidas) para a água não vazar para o
oceano nem para o mundo.

## Ordem sugerida

1. Brock primeiro (a fiação de armadilhas já existe no Ginásio dele).
2. Misty depois (a água precisa de mais testes).
3. As páginas do Brock e da Misty saem do painel F8 e passam a vir só dos baús (o botão continua para testes).

## Respostas do Welsete (2026-11-04)

- **Sem pistas** para a alavanca certa (as três são iguais; a certa muda a cada mundo).
- **Baú:** a página, uma pedra de evolução aleatória, Ultra Balls, Potions e spray (Repelente, item novo).
- **Construção:** o Claude constrói por código, inspirado nos mapas A Link to Terraria (Earth Temple, Water Dungeon) e
  The Story of Red Cloud (portões de alavanca, placas narrando).
- **Obrigatória:** a masmorra fica entre a entrada e o líder.

## Estado

- Brock: **feita, versão 2** (2026-11-04, depois do primeiro teste do Welsete: mais difícil e maior). Seis salas: chegada;
  salão das alavancas com 24 de altura (pedras caem do escuro); fosso de espinhos com pilares; corredor alto com pedras
  escondidas; fosso de lava com pilares de 1 bloco; baú. Morcegos nascem mais lá dentro. O portão não abria porque o
  Terraria não aciona atuador embaixo de bloco protegido; `ProtectedActuators` libera isso.
- Brock (versão 1): (`Content/WorldGeneration/BrockDungeon.cs`), em mundos novos ou com "Construir Ginásio do Brock aqui"
  no painel F8. Botão "TP masmorra do Brock" para testar. Em mundos antigos a página aparece ao entrar no salão.
- Misty: falta. Até lá, a página da Misty aparece ao entrar no Ginásio dela.
