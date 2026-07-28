# Direção ativa — base cooperativa 3D (26/07/2026)

O objetivo atual é consolidar uma **base cooperativa 3D divertida, estável e
extensível**. Ela deve funcionar em teste solo, mas já respeitar as regras de
uma sessão compartilhada que receberá multiplayer posteriormente.

- O jogo continua 3D; não haverá conversão para 2D.
- O foco imediato é terminar e testar o core loop: explorar, preparar,
  plantar, regar, colher, organizar o inventário, vender e dormir.
- A fazenda é projetada para 1–4 jogadores. Steam P2P, lobby e sincronização
  entram depois, mas as regras de gameplay já devem ser compatíveis com coop.
- O tempo da fazenda não pausa ao abrir inventário, lojas, menus ou diálogos:
  em uma sessão compartilhada outros jogadores podem continuar agindo.
- Economia global continua uma visão de longo prazo, fora do escopo imediato.
- O código deve evitar atalhos que impeçam uma futura migração para serviços
  remotos, sem deixar essa integração bloquear a experiência cooperativa.

O documento `Docs/Authoritative-Core-Loop.md` registra uma arquitetura futura
para backend autoritativo. Ele é uma referência técnica, não a prioridade de
produção desta fase.

# GDD — Projeto Farm

> Documento vivo de pré-produção. A prioridade é um jogo cooperativo agradável, produzido e publicado por duas pessoas, não uma simulação agrícola completa.

## 1. Visão

**Elevator pitch:** um jogo de fazenda cooperativo e acolhedor para até quatro pessoas. Amigos transformam uma pequena propriedade esquecida em uma fazenda próspera, plantando, colhendo, atendendo pedidos da vila e expandindo juntos.

**Fantasia do jogador:** pegar uma terra simples, organizar uma operação eficiente com amigos e ver a fazenda mudar visualmente a cada sessão.

**Plataforma inicial:** PC (Steam), teclado/mouse e controle.

**Câmera:** terceira pessoa isométrica suave, acompanhando o personagem.

**Modo principal:** solo ou co-op online para 1–4 jogadores, com um anfitrião que conserva o save da fazenda.

## 2. Pilares de design

1. **Cooperação sem obrigação.** Qualquer tarefa pode ser realizada por qualquer pessoa; os amigos aceleram e tornam o trabalho divertido, mas jogar solo continua completo.
2. **Progresso visível.** Novos terrenos, plantações, construções e máquinas tornam a evolução da fazenda concreta e legível.
3. **Ritmo confortável.** Sem punições severas por perder um dia; o jogador escolhe entre relaxar, otimizar ou completar pedidos.
4. **Escopo disciplinado.** Cada sistema precisa fortalecer o ciclo de cultivar e vender. Complexidade que não melhora esse ciclo fica para depois.

## 3. Público e referência de experiência

- Jogadores que gostam de fazenda, organização e jogos sociais leves.
- Sessões curtas de 20–45 minutos e sessões longas de progresso com amigos.
- Referências de sensação: a rotina acolhedora de jogos de fazenda, a colaboração espontânea de jogos co-op e a satisfação visual de construir uma base.

## 4. Loop de jogo

```text
Planejar o dia → preparar o solo → plantar → cuidar → colher
       ↑                                           ↓
Comprar melhorias ← receber dinheiro ← entregar/vender
```

O grupo escolhe sementes, prepara canteiros, planta, rega e colhe. A produção é entregue ao quadro de pedidos da vila ou vendida no entreposto. O dinheiro compra sementes, novos terrenos e melhorias que abrem novas possibilidades.

## 5. Vertical slice e MVP

### Vertical slice (primeira versão jogável)

- Uma pequena área de fazenda, personagem controlável e câmera em terceira pessoa.
- Campo em grade com preparar, plantar, regar, crescer e colher.
- Três culturas: batata, abóbora e morango.
- Inventário simples, dinheiro e ponto de venda.
- Mercado local diário com cotações determinísticas, previsão de amanhã e incentivo para armazenar colheitas.
- Um pedido da vila com recompensa.
- Save local do anfitrião.
- Primeira sessão para dois jogadores; arquitetura pronta para chegar a quatro.

### MVP para lançamento de teste

- Co-op Steam 1–4, lobby por convite e entrada/saída de convidados.
- Três a cinco culturas, pedidos rotativos e expansão de terreno.
- Loja de sementes, inventário compartilhado e permissões básicas de convidado.
- Duas construções funcionais: depósito e estufa.
- Progresso, salvamento e recuperação robustos.
- Interface e feedback completos para o ciclo de fazenda.

### Explicitamente fora do MVP

- Animais, pesca, mineração, combate, casamento, NPCs complexos e mundo aberto.
- Veículos dirigíveis e física de tratores.
- Economia entre jogadores, microtransações e Mercado da Comunidade Steam.
- Servidores dedicados, cross-play e mods.

### Visão pós-MVP — cooperação e economia global

Após o core cooperativo estar sólido, o jogo evolui em quatro frentes ligadas:

1. **Especialização estrita.** Cada personagem terá um orçamento limitado de
   pontos de habilidade. Agricultura, ferraria, pesca e outras vocações terão
   picos de eficiência mutuamente exclusivos; uma fazenda de quatro pessoas
   ganha ao dividir papéis. O ciclo básico permanecerá viável solo, mas a
   otimização, os projetos avançados e os contratos premiarão a cooperação.
2. **Mercado Global assíncrono.** Um entreposto no jogo permitirá publicar
   ordens de compra e venda entre jogadores, inclusive quando o vendedor estiver
   offline. Será uma economia interna, liquidada pelo backend — não o Mercado da
   Comunidade Steam e não uma troca por dinheiro real.
3. **Eventos globais.** Pragas, secas, safras excepcionais e outros eventos
   definidos pelo backend afetarão oferta, demanda e previsões de preço para
   todos. O impacto deve criar estratégia econômica sem punir uma fazenda que
   acabou de começar.
4. **Contratos entre jogadores.** Um jogador poderá abrir uma ordem de trabalho
   com recompensa em ouro, por exemplo entregar 1.000 cobres. A recompensa ficará
   em garantia até que o backend valide a entrega e libere o pagamento.

Detalhamento técnico, limites de segurança e ordem de implementação estão em
`Docs/Future-Global-Economy.md`.

## 6. Multiplayer

### Estrutura

- O dono cria a fazenda e atua como anfitrião.
- Convidados entram por convite Steam e jogam como trabalhadores da fazenda.
- A fazenda, dinheiro, plantações e construções pertencem ao save do anfitrião.
- Todo estado que afeta progresso é validado pelo anfitrião; clientes apenas solicitam ações.

### Regras iniciais

- Até quatro jogadores simultâneos.
- Inventário de recursos e dinheiro compartilhados no MVP.
- Convidados podem plantar, cuidar, colher e vender.
- Demolir construções, gastar grandes quantias ou alterar permissões fica reservado ao anfitrião inicialmente.
- Se o anfitrião sair, a sessão termina de forma segura e o save é gravado.

## 7. Economia e progressão

### Recursos

- **Moedas:** dinheiro da fazenda, usado para sementes, lotes e melhorias.
- **Sementes:** insumo para o cultivo.
- **Produtos:** colheitas vendidas ou usadas em pedidos.

### Fontes de receita

- Venda direta ao entreposto da vila.
- Pedidos rotativos, que pagam mais por uma combinação específica de produtos.

### Progressão

1. Pequeno campo e ferramentas básicas.
2. Primeira expansão de 9 para 15 canteiros por $500.
3. Segunda expansão de 15 para 25 canteiros por $1.500.
4. Depósito e estufa.
5. Automação leve e máquinas — apenas após o núcleo de cultivo estar divertido.

O protótipo já implementa as duas compras no caixote de comércio. Os 9 canteiros originais mantêm posição e identidade; os novos são acrescentados nas laterais e depois nas faixas frontal/traseira. O nível do terreno é persistido no save v19 e saves anteriores começam de forma segura com 9 canteiros.

### Mercado Steam (podemos mudar para produtos raros, carros, ferramentas e maquinarios pesados.)

O jogo terá um mercado interno de vila no lançamento. O Steam Inventory/Community Market será avaliado apenas depois do núcleo estar estável e, se usado, será para cosméticos negociáveis (roupas, chapéus, decoração ou skins), nunca para dinheiro ou itens que definem poder no jogo.

O protótipo local já usa quatro faixas diárias por cultura (`Baixa`, `Estável`, `Alta` e `Pico`). A previsão do dia seguinte fica visível no caixote, criando uma primeira camada de oferta e demanda que continuará válida em uma sessão cooperativa.

## 8. Mundo, arte e apresentação

### Direção visual

Low-poly colorido, legível e acolhedor. O pacote **POLYGON – Farm Pack (Synty)** é a base visual: personagens, terrenos cultivados, celeiros, estufas, silos, veículos e props.

### Estrutura do mapa inicial

- Casa simples e área de spawn.
- Campo inicial em frente à casa.
- Depósito/caixa de envio.
- Estrada até o entreposto da vila (inicialmente uma área compacta, não mundo aberto).
- Lotes adjacentes bloqueados por placas de compra.

### Áudio (posterior ao slice)

- Sons claros de solo, água, colheita, venda e compra.
- Música ambiente calma, com variação dia/noite.

## 9. Interface e controles

### HUD mínimo

- Ferramenta equipada.
- Espaços rápidos de inventário.
- Dinheiro compartilhado.
- Objetivo/pedido ativo.
- Indicador de interação e progresso de ações.

### Controles iniciais

- Movimento: WASD/analógico.
- Câmera: mouse/stick direito.
- Interagir/usar ferramenta: botão primário.
- Trocar ferramenta: roda do mouse/botões de ombro.
- Inventário e quadro de pedidos: teclas/telas dedicadas.

## 10. Direção técnica

| Área | Decisão inicial |
| --- | --- |
| Engine | Unity 6 + URP |
| Entrada | Unity Input System |
| Câmera | Cinemachine |
| Navegação futura | AI Navigation / NavMesh |
| Online | Host-authoritative via Steam; solução de transporte será validada no vertical slice |
| Salvamento | Arquivo local versionado, pertencente ao anfitrião |
| Dados de jogo | ScriptableObjects para culturas, itens, receitas e progressão |

**Princípio de arquitetura:** sistemas de cultivo e inventário são criados desde o início para que a autoridade possa ficar no anfitrião. O protótipo pode rodar localmente, mas não deve esconder estado importante apenas no cliente.

## 11. Riscos e respostas

| Risco | Resposta |
| --- | --- |
| Multiplayer consome o projeto | Fazer o ciclo de cultivo divertido antes de ampliar conteúdo; validar duas pessoas cedo. |
| Sincronização de estado e saves | Anfitrião é a fonte de verdade e grava em pontos seguros. |
| Muitos sistemas de simulador | Usar a lista “fora do MVP” como contrato de escopo. |
| Arte sem animações adequadas | Validar personagem, controle e câmera antes de fazer conteúdo. |
| Mercado Steam gera fraude/economia complexa | Adiar completamente; manter recursos de gameplay no jogo. |

## 12. Marcos de produção

1. **Pré-produção:** GDD, identidade visual, estrutura de pastas e protótipo de personagem/câmera.
2. **Vertical slice:** ciclo completo de uma cultura, uma venda e um pedido em uma fazenda pequena.
3. **Co-op de prova:** duas pessoas podem realizar o mesmo ciclo sem dessincronização.
4. **MVP de teste:** quatro jogadores, expansão, saves e conteúdo suficiente para feedback externo.
5. **Polimento:** interface, áudio, acessibilidade, desempenho e playtests.

## 13. Decisões abertas

- Nome definitivo e identidade da fazenda/vila.
- Duração de um dia e ritmo de crescimento das culturas.
- Inventário: totalmente compartilhado ou mochilas individuais com depósito comum.
- Estilo exato de câmera: mais livre ou mais próximo do isométrico.
- Solução final de rede Steam e estratégia de teste em múltiplas máquinas.
- Direção de monetização: jogo premium, DLC cosmético ou nenhuma camada adicional.

---

**Regra deste documento:** qualquer funcionalidade nova deve declarar qual pilar fortalece, quanto custa em escopo e se pertence ao MVP ou ao pós-lançamento.
