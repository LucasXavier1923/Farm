# Direção de arquitetura ativa — base cooperativa 3D (26/07/2026)

Os sistemas abaixo são a base em uso do protótipo. Todo trabalho novo deve
fortalecer o core loop cooperativo antes de ampliar escopo de rede.

- Manter e melhorar a cena 3D, personagem, câmera, canteiros e assets atuais.
- O save local pode existir apenas para testes, mas regras de sessão não devem
  depender de pausa, estado global de UI ou tempo exclusivo de um jogador.
- Inventário, hotbar, cultivo, venda, sono e feedback visual devem ser claros,
  testáveis e desacoplados o bastante para evoluírem sem retrabalho.
- `IFarmBackend` e o mock existentes ficam preservados como experimento de
  arquitetura futura. Eles não bloqueiam nem substituem a meta cooperativa.
- A evolução futura para Supabase, Steamworks e coop deve reaproveitar contratos
  de dados, mas será planejada depois que o loop local estiver sólido.

Esta revisão mantém o protótipo visual 3D e define a compatibilidade com coop
como fonte de prioridade do desenvolvimento atual.

### Tempo de sessão cooperativa

`FarmSessionTime` é a origem de tempo para a simulação que precisa continuar
enquanto um jogador consulta uma interface. `FarmDayClock`, os temporizadores
de crescimento dos canteiros e a irrigação automática da chuva usam esse tempo
não escalado. `FarmHudController.IsModalOpen` bloqueia somente o input daquele
jogador; ele não pode congelar relógio, crescimento, clima ou automações.

`FarmSleepSession` representa a política local de sono: uma lista de
participantes e seus votos de prontidão. `FarmTestPlot` avança o dia somente
quando todos votaram; em teste solo a lista contém apenas o jogador local. O
adaptador de rede futuro deve alimentar participantes e votos, sem reintroduzir
avanço de dia decidido por um único cliente.

`FarmSessionCommerce` é a fronteira local de compra e venda compartilhada. A
estação envia um `FarmCommerceRequest` com `CommandId`; resultados concluídos
ficam em cache para que uma repetição não compre sementes ou venda colheitas
duas vezes. Hoje a fronteira executa contra `FarmGameState`; o host Steam futuro
deve executar os mesmos pedidos e devolver o resultado confirmado.

# Base de sistemas — Farm

Documento técnico vivo da vertical slice cooperativa. Atualizar este arquivo sempre que um sistema mudar de responsabilidade, formato de save ou controle.

## Estado atual

### Ciclo de fazenda

O protótipo suporta preparar o solo, plantar, regar, aguardar o crescimento, colher, vender a produção e comprar sementes. Morango, abóbora, cenoura e milho compartilham a mesma lógica e usam três estágios visuais do pacote Synty.

As culturas têm papéis iniciais distintos:

- cenoura: crescimento rápido de 7 segundos, rendimento 3, pacote de 8 sementes por $24 e venda a $6;
- morango: crescimento de 10 segundos, rendimento 3, pacote de 6 sementes por $28 e venda a $8;
- abóbora: crescimento médio de 10 segundos, rendimento 2, pacote de 5 sementes por $20 e venda a $15;
- milho: crescimento lento de 15 segundos, rendimento 2, pacote de 4 sementes por $35 e venda a $22.

Esses números ainda são de protótipo. A diferença de ritmo já permite validar decisões de curto prazo, retorno e espaço de inventário antes do balanceamento definitivo por dias e estações.

### Primeira Colheita

O HUD apresenta um guia persistente com seis marcos: preparar, plantar, regar, colher, vender e comprar sementes. As ações podem ser realizadas fora da ordem; cada marco conta apenas uma vez. A conclusão concede um único bônus de $50 e permanece registrada no save.

Esse sistema é deliberadamente leve: ensina jogando, sem travar controles nem impedir experimentação. Futuramente ele pode alimentar um diário maior sem alterar o ciclo agrícola.

### Diário da fazenda

- `J` abre uma janela modal com cinco objetivos permanentes, separados do onboarding Primeira Colheita.
- As categorias atuais são Introdução, Produção, Descoberta, Progressão e Exploração.
- O diário registra solo preparado, plantio, rega, unidades colhidas e vendidas, pacotes comprados, pickups, melhorias e culturas diferentes descobertas.
- Objetivos iniciais: preparar 10 canteiros, colher 12 produtos, colher as 4 culturas, comprar 2 melhorias e encontrar os 3 pickups.
- O objetivo de variedade `Quatro estações` exige morango, milho, abóbora e cenoura e concede $125. Saves antigos preservam os IDs já descobertos e continuam progredindo normalmente.
- Recompensas precisam ser resgatadas manualmente. Isso cria um momento de retorno ao diário e evita alterar a economia silenciosamente.
- Cada recompensa é entregue uma única vez e o estado `Resgatado` permanece no save.
- Estados visuais cobrem `Em progresso`, `Resgatar` e `Resgatado`.
- Abrir o diário fecha mochila ou depósito e bloqueia somente o input local de movimento, câmera e interação. Relógio, clima, crescimento e demais simulações continuam em execução.
- Novos objetivos podem ser adicionados a `FarmJournalDatabase` sem criar campos individuais no HUD ou no save.

### Inventário

- Mochila com capacidade de 20 slots.
- Pilhas respeitam o `MaxStack` do `ItemDefinition`.
- A janela abre com `I` ou `Tab` e fecha com `I`, `Tab`, `Esc` ou o botão Fechar.
- Enquanto a mochila está aberta, movimento, câmera e interação no mundo são bloqueados.
- Cada categoria usa um ícone genérico temporário e uma cor: `S` sementes, `P` produtos, `F` ferramentas e `M` materiais.
- Passar o mouse sobre um item mostra nome, categoria, origem, quantidade, pilha máxima, valor base e uso principal.
- O tooltip acompanha o ponteiro e é limitado às bordas da tela; iniciar um arraste ou fechar a janela o remove imediatamente.
- Os ícones por letra são placeholders deliberados. O sistema está pronto para receber `Sprite` no `ItemDefinition` posteriormente.
- `Todos`, `Sementes`, `Colheitas`, `Materiais` e `Projetos` são filtros somente de visualização; trocar o filtro não move, consome ou duplica itens.
- O cabeçalho informa quantos slots existem na mochila e quantos itens o filtro atual está exibindo.
- `ORGANIZAR` aplica uma ordem persistente e determinística: sementes, colheitas, materiais, projetos e ferramentas/outros; dentro de cada grupo, usa o nome localizado.
- Organizar não altera quantidades, atalhos da hotbar ou descobertas e repetir a ação sobre uma lista já organizada não produz nova mudança.
- Colheitas `Normal`, `Prata` e `Ouro` ocupam pilhas separadas. O mesmo produto pode, portanto, usar até três slots com quantidades e valores diferentes.
- A mochila, depósito, resumo compacto e tooltip usam `◆ Prata` e `★ Ouro`; a ausência de marca significa qualidade Normal.
- Dentro do mesmo produto, `ORGANIZAR` coloca Ouro, Prata e Normal nessa ordem. Transferir pilha, metade ou unidade preserva exatamente a qualidade selecionada.

### Coleções da fazenda

- `L` ou o botão `COLEÇÕES` abre um livro modal separado das recompensas do diário.
- O catálogo é derivado dos `ItemDefinition` carregados e atualmente contém 13 entradas: quatro sementes, quatro colheitas, dois materiais e três projetos.
- As categorias `Todos`, `Sementes`, `Colheitas`, `Materiais` e `Projetos` filtram o mesmo catálogo sem duplicar dados.
- A página comporta 12 cartões; o catálogo completo usa paginação 12+1. Conteúdo futuro entra automaticamente na contagem e na paginação.
- Itens desconhecidos exibem silhueta, `????` e uma pista neutra. Itens conhecidos mostram tipo, estoque combinado entre mochila/depósito, pilha máxima e valor ou finalidade.
- Uma descoberta ocorre quando o item realmente entra na mochila ou no depósito: início do jogo, compra, colheita, pickup, crafting, carta ou carregamento.
- Vender, consumir, construir ou transferir não apaga a descoberta. Abrir loja, crafting ou catálogo de construção não revela itens.
- O sistema é informativo e economicamente neutro: não entrega dinheiro, itens ou bônus.
- Saves v16 inferem descobertas pelo inventário, depósito e culturas registradas como colhidas no diário.

### Hotbar

- Oito espaços rápidos selecionáveis pelas teclas `1` a `8`.
- Configuração inicial: enxada, sementes de abóbora, regador, colher e quatro espaços vazios.
- Arrastar um item da mochila ou do painel de depósito para um espaço da hotbar cria um atalho para aquele item. Ao arrastar do depósito, a pilha é primeiro transferida para a mochila.
- Arrastar uma entrada ocupada da hotbar para outra entrada troca as duas posições. A seleção acompanha o item que foi movido.
- Clique esquerdo seleciona; clique direito limpa apenas atalhos de itens. Enxada, regador e colheita são ferramentas-base permanentes: podem ser arrastadas para reorganizar, mas não apagadas. Se uma versão antiga da sessão estiver sem uma delas, clique direito no slot-padrão vazio restaura a ferramenta sem duplicá-la. O gesto de arrastar só reorganiza após a confirmação assíncrona do backend de protótipo.
- A quantidade exibida vem do inventário em tempo real; a hotbar não duplica itens e remove automaticamente o atalho de item quando a última unidade é consumida.
- Selecionar sementes preserva o `ItemId` exato, evitando usar a semente errada quando novas culturas forem adicionadas.
- Selecionar qualquer semente resolve sua `CropDefinition`; o canteiro registra a cultura realmente plantada e a restaura corretamente pelo save.

### Homestead utility

- `Farm Shed Kit` is crafted from 8 Wood and 8 Stone and placed through the existing build catalog under `UTILITY`.
- Each placed Farm Shed adds 15 shared storage slots. The bonus is derived from placed-object state, not stored as a separately mutable counter.
- A Farm Shed cannot be reclaimed if that would leave the current storage contents above the reduced capacity.
- `Garden Bench Kit` and `Garden Planter Kit` are the first `DECORATION` recipes. They use the same authoritative-ready item, recipe, placement, movement, reclaim, and persistence path as practical structures, but deliberately provide no mechanical advantage.
- `FarmHudController` presents a non-modal Homestead guide derived from shared farm state. Its priority is: collect an available egg, feed chickens, acknowledge active Farm Shed capacity, then encourage continued decorating/routine. It does not pause the day clock or mutate gameplay state.

### Catálogo de sementes

- O caixote de comércio possui navegação anterior/próxima entre todas as culturas válidas carregadas em `Resources/GameData/Crops`.
- A proximidade apenas exibe o convite de interação. A loja nunca abre sozinha: `F` abre a até 2,2 unidades e `Esc` ou o botão `FECHAR` encerram o modal.
- Cada página mostra quantidade e preço do pacote e valor unitário da colheita.
- Comprar usa a cultura exibida no catálogo, sem exigir que o jogador já possua uma semente dela.
- `Vender todos` liquida, em uma ação, todos os produtos agrícolas reconhecidos no inventário, mas não vende sementes, ferramentas ou materiais.
- O painel mostra a cotação de hoje, a previsão de amanhã e os preços atuais de Normal/Prata/Ouro para a cultura selecionada.
- Adicionar uma nova cultura ao catálogo não exige novo código de loja, inventário, hotbar ou canteiro.

### Depósito local

- Um segundo caixote fica no lado oposto ao comércio e usa um marcador azul temporário.
- A interação aparece e funciona a até 2,2 unidades, por clique ou `F`.
- A tela compara os 20 slots da mochila com 30 slots do depósito.
- Clique esquerdo transfere a pilha inteira; `Shift + clique esquerdo` transfere metade, arredondada para cima; clique direito transfere uma unidade.
- A transferência respeita capacidade e `MaxStack` nos dois sentidos.
- Tooltips usam o estoque do lado de origem, portanto a quantidade mostrada permanece coerente entre mochila e depósito.
- O depósito é modal e persistente. Abrir a mochila fecha o depósito e vice-versa.
- `ORGANIZAR MOCHILA` e `ORGANIZAR BAÚ` ordenam os dois estoques independentemente, mantendo a transferência explícita.
- O prop atual reutiliza o caixote Synty. Ele poderá ser trocado por um baú dedicado sem alterar dados ou UI.

### Coleta no mundo

- Três pickups iniciais demonstram sementes e produto usando modelos Synty e um marcador dourado.
- O item flutua e gira enquanto está no chão.
- A até 2,5 unidades, ele é atraído suavemente para o personagem; a coleta acontece a 0,7 unidade.
- A atração pausa enquanto mochila ou depósito estão abertos.
- Inventário cheio mantém o item no mundo e apresenta uma mensagem orientando o jogador a usar o depósito.
- Cada pickup possui ID persistente. Um item coletado não reaparece após salvar ou carregar.
- O HUD mostra uma notificação temporária, por exemplo `+3 Sementes de abóbora`.

### Relógio e iluminação

- Um dia completo dura 10 minutos reais na configuração atual.
- O avanço usa `deltaTime`, portanto não depende da taxa de frames.
- O relógio não pausa durante janelas modais: loja, mochila, depósito, diário, crafting, domínio, opções, pedidos e confirmação de descanso. Esses painéis bloqueiam apenas o input local.
- O HUD mostra dia, hora e fase: noite, amanhecer, manhã, tarde ou entardecer.
- A iluminação direcional e a intensidade ambiente Trilight acompanham uma curva gradual, com cores mais quentes e saturadas durante o dia.
- A névoa cinzenta da cena de demonstração é desativada em runtime.
- Entardecer e noite possuem pisos mínimos de luz direcional e ambiente. A noite continua reconhecível, mas terreno, cercas, canteiros, props e personagem permanecem navegáveis.
- Nublado e chuva reduzem a luz com menos agressividade, preservando a paleta cozy.
- O estado faz checkpoints periódicos a cada 30 segundos reais e também na virada do dia.
- Crescimento de culturas ainda usa segundos reais para testes rápidos; sua futura conversão para dias poderá consumir este serviço sem acoplamento ao HUD.

### Calendário e estações

- O ano de protótipo possui 28 dias: Primavera, Verão, Outono e Inverno, com 7 dias cada.
- Ano, estação e dia da estação são derivados apenas do número global do dia. Não há dados duplicados no save nem risco de calendário incompatível ao carregar.
- O HUD central mostra `ANO N • Estação D/7`, com uma cor própria para cada estação, sem ocultar hora, fase do dia, clima ou previsão.
- A transição da manhã também informa a nova estação. Dormir no Dia 7, por exemplo, desperta no Dia 8 como `Verão 1/7`.
- O calendário oferece funções puras para resolver ano, estação e dia interno. Isso permite que culturas, pedidos e eventos consultem a mesma regra sem depender da interface.
- Sete dias por estação é uma cadência rápida para validar o protótipo. O valor está centralizado em `FarmDayClock.DaysPerSeason` e pode ser ampliado depois do balanceamento.

### Afinidades sazonais das culturas

- Cada `CropDefinition` declara sua estação favorita e o bônus de rendimento; a regra é data-driven e novas culturas não exigem condicionais no canteiro.
- Morango prefere Primavera, milho prefere Verão, abóbora prefere Outono e cenoura prefere Inverno. Cada uma rende +1 unidade ao ser colhida na estação favorita.
- Fora da estação, a cultura continua plantável e entrega seu rendimento base. Não há morte instantânea, semente inválida ou bloqueio de catálogo.
- O bônus é calculado na colheita e a quantidade real alimenta inventário, capacidade, diário e venda. O feedback informa explicitamente quando a afinidade foi aplicada.
- O catálogo do caixote mostra `Afinidade: Estação • +1 na colheita`, permitindo planejar antes de comprar.
- As quatro estações agora possuem pelo menos uma cultura favorita, formando uma matriz sazonal completa sem impedir plantio fora de época.

### Qualidade das colheitas

- A qualidade é determinística e combina afinidade sazonal com domínio de Colheita; não existe sorte escondida nem recarregamento para tentar outro resultado.
- Em nível 1, colher fora da afinidade produz `Normal`; colher na estação favorita produz `Prata`.
- Colheita N2 também produz `Prata` fora da estação. `Ouro` exige simultaneamente Colheita N3 e a estação favorita, premiando especialização com planejamento.
- O valor unitário é arredondado para cima: Normal usa 100% do preço base, Prata 125% e Ouro 160%.
- Qualidade não altera rendimento, crescimento ou energia. Esses eixos continuam independentes e legíveis.
- O feedback da colheita informa a qualidade, e o tooltip mostra o valor unitário real daquela pilha.

### Mercado diário da vila

- Cada produto recebe diariamente uma cotação independente derivada de `WorldSeed`, número do dia e `ItemId` por hash estável. O mesmo mundo sempre produz a mesma sequência, inclusive após reiniciar.
- As quatro faixas são `BAIXA` ×0,85, `ESTÁVEL` ×1,00, `ALTA` ×1,20 e `PICO` ×1,45. A distribuição favorece estabilidade, mas percorre todos os estados ao longo do tempo.
- O preço final combina uma única vez preço base, qualidade e cotação do dia, com arredondamento para cima por unidade.
- O caixote mostra hoje e amanhã antes da venda. Isso transforma o depósito em ferramenta de planejamento, sem exigir uma tela financeira separada.
- `VENDER TODOS` continua afetando apenas a mochila; produtos guardados no depósito permanecem protegidos para uma cotação futura.
- Pedidos diários mantêm recompensa fixa e premium. Eles oferecem certeza imediata, enquanto o mercado oferece risco temporal controlável.
- Cotações são derivadas e não ocupam espaço no save. Alterar o relógio para o dia seguinte basta para reconstruir preços e previsão.

### Quadro de pedidos diários

- Um quadro Synty fica na borda oposta do canteiro, marcado em ciano. O prefab-fonte foi copiado para `Resources/FarmProps/OrderBoard.prefab` sem alterar o pacote original.
- A interação funciona por clique ou `F`, somente a até 2,2 unidades. A janela é modal, não pausa a simulação compartilhada e fecha com `Esc` ou o botão `FECHAR`.
- Cada dia gera três pedidos determinísticos a partir da semente do mundo e do número do dia. O mesmo save produz os mesmos pedidos; o dia seguinte renova IDs, ordem, quantidades e recompensas.
- Três culturas distintas são escolhidas entre as quatro disponíveis em cada quadro, em ordem determinística embaralhada. Quantidades variam de 2 a 4 e a recompensa é sempre superior à venda comum dos mesmos itens.
- Cada cartão mostra quantidade pedida, quantidade disponível, recompensa e estado `FALTAM`, `ENTREGAR` ou `ENTREGUE`.
- Entregar remove exatamente os produtos solicitados, paga na hora, conta como unidades vendidas no diário e salva imediatamente.
- Pedidos aceitam qualquer qualidade e consomem Normal antes de Prata e Ouro. A recompensa do cartão é fixa, permitindo guardar produtos valiosos sem exigir gerenciamento manual de cada entrega.
- Um pedido concluído não pode pagar novamente. Completar os três no mesmo dia concede bônus adicional de $25.
- A migração v10 cria o progresso diário vazio no dia carregado; v11 persiste dia e máscara de entregas concluídas. Na virada da manhã, a máscara é zerada para o novo quadro.

### Sono e encerramento voluntário do dia

- Uma cama Synty fica na borda do canteiro, marcada em violeta. O prefab original foi copiado para `Resources/FarmProps/SleepBed.prefab`, mantendo o pacote-fonte intacto.
- A interação aparece e funciona a até 2,5 unidades, por clique ou `F`. O alcance permite interagir ao lado da cama, sem subir no prop, e ainda impede encerrar o dia à distância.
- Dormir sempre exige uma confirmação modal, que explica o horário de despertar e o efeito sobre as culturas.
- Ao confirmar, o jogo avança para o próximo dia às 06:00, recalcula luz e clima determinístico, mostra uma transição curta e salva imediatamente.
- Apenas culturas já regadas avançam durante a noite. Solo preparado, sementes ainda secas e canteiros vazios não mudam.
- O avanço noturno converte os minutos pulados para o mesmo tempo real usado pelo crescimento atual. Assim, o comportamento continua consistente enquanto o protótipo usa crescimento acelerado.
- O descanso continua voluntário e agora restaura a energia por completo. Chegar a zero não encerra o dia nem bloqueia ações, preservando a autonomia do jogador.

### Clima e previsão

- O clima diário é determinístico a partir da semente do mundo e do número do dia: o mesmo save sempre produz a mesma sequência.
- A distribuição atual é 55% ensolarado, 25% nublado e 20% chuva. A semente padrão garante Dia 1 e Dia 2 ensolarados e a primeira chuva no Dia 3, preservando um onboarding previsível.
- O HUD mostra o clima atual e a previsão do dia seguinte, permitindo planejar o plantio.
- Chuva rega automaticamente apenas canteiros plantados ainda secos. Ela não altera solo vazio ou apenas arado e nunca reinicia o crescimento de um canteiro já regado.
- Nuvens e chuva reduzem suavemente a luz direcional e ambiente; a chuva usa partículas locais que acompanham o personagem para manter baixo o custo visual.
- O save persiste a semente do mundo, não o resultado do clima. Isso mantém os dados pequenos e permite reconstruir qualquer previsão.

### Equipamento visual

`FarmEquipmentController` observa o `FarmTool` ativo de `FarmTestPlot`. Os props na mão acompanham a hotbar configurável sem ler teclas numéricas diretamente.

### Progressão de ferramentas

- Enxada, regador e ferramenta de colheita possuem níveis independentes de 1 a 3.
- L1 atua em um canteiro; L2 atua em uma linha horizontal de até 3; L3 atua em uma área 3×3. Nas bordas, a área é recortada sem acessar índices inválidos.
- Sementes continuam usando exatamente um canteiro, mesmo quando outras ferramentas estão evoluídas. Isso mantém escolha de cultura e consumo deliberados.
- O caixote de comércio melhora a ferramenta atualmente selecionada. L2 custa $150 e L3 custa $500 na economia de protótipo.
- O botão informa ferramenta, nível, área e custo, fica desabilitado sem dinheiro e muda para nível máximo após L3. O atalho `U` executa a mesma compra quando o caixote está ao alcance.
- A hotbar e o texto de seleção mostram o nível e a área atuais. Ao mirar um canteiro, toda a área afetada recebe destaque antes da ação.
- Ações em área contam apenas canteiros realmente alterados; colheitas mistas respeitam rendimento e `ItemId` de cada cultura.

### Energia leve

- A energia máxima é 100 e aparece numa barra abaixo do relógio. A cor muda de verde para laranja e vermelho conforme a reserva diminui.
- Custos por canteiro realmente alterado: enxada 4, sementes 1, regador 3 e colheita 2. Mirar uma ação inválida não cobra energia.
- Ferramentas melhoradas cobram pelo número real de canteiros processados. Exemplo: uma enxada L3 que altera nove canteiros custa 36.
- A ação que esgota a reserva ainda é concluída. A partir de zero, ferramentas de área funcionam em um canteiro por vez; nenhuma ação válida é bloqueada.
- Dormir restaura 100 de energia. Chuva, comércio, depósito, pickups e pedidos não consomem energia.
- A regra cria planejamento e valor para melhorias sem impor desmaio automático ou interromper uma sessão de construção e organização.

### Feedback físico de ações

- `FarmActionFeedback` cria cinco emissores procedurais em runtime, sem alterar os assets Synty e sem depender de prefabs externos.
- Enxada usa terra marrom, sementes usam faíscas verdes, regador usa respingos azuis e colheita usa folhas/fragmentos laranja. O raio do efeito cresce conforme 1, 3 ou 9 canteiros são alterados.
- Ações inválidas não geram partículas, seguindo a mesma regra usada pelo consumo de energia.
- Melhorias de ferramenta e entregas do quadro disparam um estouro dourado; completar o quadro aumenta sua intensidade.
- Os materiais usam o shader de partículas do URP e simulação em espaço de mundo, evitando partículas rosadas ou que acompanhem o emissor depois de nascer.
- A busca no projeto não encontrou `AudioClip`. A camada visual foi mantida isolada para receber sons reais posteriormente sem acoplar áudio às regras agrícolas.

### Configurações locais

- `F10` ou o botão `OPÇÕES` abre uma janela modal; `Esc` ou `FECHAR` retorna ao jogo.
- O menu controla sensibilidade da câmera, velocidade do zoom e inversão do eixo vertical. Os valores padrão preservam a câmera já aprovada: sensibilidade `0,12`, zoom `2,0` e eixo normal.
- Sensibilidade é limitada entre `0,04` e `0,40`; o passo do zoom, entre `0,5` e `10,0` unidades. Os botões não permitem valores inválidos.
- As alterações chegam à câmera imediatamente e são persistidas em `PlayerPrefs`, separadas do save da fazenda. Assim, preferências da máquina não contaminam o progresso do mundo.
- `RESTAURAR PADRÕES` recompõe os três valores de referência e salva a alteração.
- Abrir as opções fecha mochila, depósito, diário, descanso e pedidos. Enquanto o painel está aberto, movimento, câmera e interação local ficam bloqueados; pickups, relógio, clima e cultivo continuam no mundo.
- Os números usam a localidade do sistema, portanto aparecem com vírgula em português sem depender de comparações textuais nos testes.

### Domínio da fazenda

- `K` ou o botão `DOMÍNIO` abre uma janela modal com três trilhas: Cultivo, Colheita e Comércio.
- Cada trilha começa no nível 1, alcança o nível 2 com 12 XP e o nível 3 com 36 XP. O nível máximo é apresentado como `DOMÍNIO MÁXIMO`.
- Cultivo recebe XP por canteiro realmente preparado, plantado ou regado. Colheita recebe 2 XP por canteiro colhido e 1 XP por pickup encontrado. Comércio recebe XP por unidade vendida/entregue e 2 XP por pacote de sementes comprado.
- Ações inválidas não concedem XP, seguindo a mesma fonte de verdade usada por energia e partículas.
- Cultivo N2, `Primeiro fôlego`: a primeira ação manual de cultivo de cada dia não consome energia. O dia utilizado fica no save, impedindo exploração por carregar o jogo.
- Cultivo N3, `Ciclo contínuo`: ao colher, a mesma cultura é replantada automaticamente se houver sua semente; a nova planta ainda precisa ser regada.
- Colheita N2, `Olhar atento`: pickups passam a ser atraídos a 4 unidades, em vez de 2,5.
- Colheita N3, `Cesta de apoio`: se a mochila não comportar a colheita inteira, ela segue para o depósito quando houver espaço. Sem espaço nos dois locais, o canteiro permanece pronto.
- Comércio N2, `Agenda local`: o quadro diário revela as três culturas dos pedidos de amanhã.
- Comércio N3, `Estoque conectado`: entregas podem consumir primeiro a mochila e completar a quantidade com produtos do depósito.
- Os benefícios priorizam conveniência, informação e continuidade de fluxo. Nenhum nível multiplica preço ou rendimento das culturas.
- A janela fecha os demais modais e bloqueia movimento, câmera e interação local. Ela não interrompe pickups, relógio, clima ou crescimento de culturas.

### Fabricação data-driven

- Uma bancada Synty é montada em runtime com mesa, caixa de ferramentas e bigorna, sem alterar os prefabs originais do pacote.
- A proximidade apenas mostra `BANCADA PRÓXIMA [F]`. `F` abre a até 2,2 unidades; somente `Esc` ou o botão `FECHAR` encerram a janela.
- Madeira e pedra são `ItemDefinition` comuns, coletáveis e persistentes pelo mesmo inventário dos demais materiais.
- As receitas são `CraftingRecipe` em `Resources/GameData/Recipes`. Ingredientes, quantidade produzida, descrição e prefab de referência são dados editáveis.
- Receitas iniciais: kit de espantalho (6 madeiras + 2 pedras), kit de aspersor (4 madeiras + 6 pedras) e duas seções de cerca (3 madeiras).
- A fabricação é transacional: materiais só são consumidos se a receita for válida e o resultado couber na mochila. Falta de material ou espaço não causa perda parcial.

### Construção e posicionamento

- Selecione um kit na hotbar e pressione `G` para iniciar o posicionamento.
- O preview fica verde em um local válido e vermelho quando o espaço está obstruído, distante ou sem terreno adequado.
- `R` gira pelo passo definido no `FarmBuildableDefinition`; clique esquerdo confirma e `Esc` ou clique direito cancela sem consumir o kit.
- `X` recolhe uma construção próxima sob a mira e devolve o kit à mochila, desde que haja espaço.
- O kit só é consumido depois da confirmação válida. Sobreposição usa a área configurada do objeto e não depende dos colliders inconsistentes dos prefabs.
- Espantalho, aspersor e cerca pintada são definidos em `Resources/GameData/Buildables`; novos objetos podem ser adicionados sem alterar o fluxo de posicionamento.
- Construções confirmadas recebem ID persistente, posição e rotação. Carregar o jogo reconstrói os prefabs correspondentes.
- Cercas encaixam automaticamente pelas extremidades a até 1,25 unidade. O preview mostra um marcador dourado e `ENCAIXADA` quando encontra uma conexão.
- O encaixe alinha seções retas ou cantos de 90 graus. Depois de confirmar, o posicionamento continua enquanto houver kits; `Esc` encerra sem consumir a próxima peça.
- Uma extremidade compartilhada é permitida, mas centros duplicados e interseções reais continuam bloqueados pela área determinística de ocupação.

### Catálogo de construção

- `B` ou o botão `CONSTRUIR` abre uma janela modal; `B`, `Esc` ou `FECHAR` encerram sem alterar o inventário.
- O catálogo é preenchido pelos `FarmBuildableDefinition` em `Resources/GameData/Buildables`. Nome, descrição, categoria, kit, função e prefab permanecem data-driven.
- A busca considera nome, descrição, kit e categoria, ignora maiúsculas e aceita termos sem acento, como `automacao`.
- Filtros iniciais: todos, lavoura, automação, cercas e decoração. Categorias sem projetos exibem um estado vazio explícito.
- Cada cartão mostra um ícone genérico, função, descrição, custo de um kit e quantidade disponível na mochila.
- Projetos sem kit permanecem visíveis, mas orientam o jogador a fabricar o item na bancada. Um projeto disponível fecha o modal e inicia o preview correto.
- Abrir o catálogo ou o preview não consome itens. O kit continua sendo removido somente após a confirmação espacial válida.
- `G` continua disponível como atalho rápido para o kit selecionado na hotbar.

### Edição espacial de construções

- Mire em uma construção a até 6 unidades e pressione `M` para mover o objeto existente.
- Durante o movimento, o original fica oculto e um preview usa as mesmas regras de distância, terreno, rotação, encaixe e sobreposição da colocação inicial.
- Clique esquerdo confirma a nova posição; `Esc` ou clique direito desfazem e restauram exatamente posição, rotação e objeto originais.
- Mover preserva o ID persistente, o tipo e a quantidade de construções. Nenhum kit é removido ou devolvido.
- A API de estado recusa mudanças de `ItemId` durante uma atualização, impedindo transformar um objeto em outro pelo fluxo de movimento.
- `H` alterna uma malha local de 10×10 metros, com células de 0,5 unidade e transparência discreta. A malha acompanha o preview e desaparece ao sair do modo de construção.
- Um destino inválido mantém o estado anterior e mostra o motivo; save/load reconstrói posição e rotação confirmadas.

### Aspersor automático

- `FarmBuildableDefinition` separa a aparência da função da construção. O aspersor usa `Function = Sprinkler` e raio editável de 5 metros.
- O preview e o objeto colocado apresentam um círculo azul com a área real de cobertura. O círculo fica discreto quando o jogador está próximo e pulsa ao funcionar.
- Ao cruzar 06:00, cada aspersor procura canteiros plantados dentro do próprio raio e os rega automaticamente.
- Dormir até a manhã executa a mesma automação antes do save e informa quantos canteiros foram regados.
- Áreas de vários aspersores podem se sobrepor sem regar ou contabilizar duas vezes o mesmo canteiro.
- Canteiros preparados, vazios, já regados, crescendo ou prontos não são alterados.
- O evento de manhã também atualiza o clima quando o relógio chega naturalmente às 06:00, sem exigir que o jogador durma.

### Pragas previsíveis e espantalho

- Corvos visitam a fazenda nos dias 3, 6, 9 e assim por diante. O calendário é fixo e o HUD avisa `CORVOS AMANHÃ`, permitindo planejamento.
- Uma visita afeta no máximo um cultivo regado ou crescendo e adiciona somente 2 segundos ao crescimento. Plantas e produtos nunca são destruídos.
- O alvo exposto é escolhido de forma determinística pela semente do mundo e pelo dia; salvar e carregar não muda a escolha.
- O espantalho protege cultivos em um raio data-driven de 6,5 metros. Preview e objeto colocado exibem o mesmo círculo amarelo usado pela regra.
- Espantalhos pulsam durante uma visita. Cultivos protegidos são contabilizados no feedback da manhã.
- Se todos os cultivos elegíveis estiverem protegidos, a visita não causa atraso.
- A manhã processada fica registrada no save. Recarregar ou chamar novamente a automação no mesmo dia não repete irrigação ou atraso de praga.

### Caixa postal e agenda sazonal

- A caixa postal usa o prefab Synty `SM_Prop_LetterBox_01`, copiado para `Resources/FarmProps/Mailbox.prefab`, e nunca abre apenas por proximidade.
- A até 2,2 unidades, `F` abre uma janela modal com lista de cartas, estados `NOVO`, `LIDO` e `RESGATADO`, detalhes, anexos e o próximo evento do calendário.
- `Esc` ou o botão `FECHAR` encerra a janela. Enquanto aberta, movimento, ferramentas, relógio e outros atalhos modais permanecem bloqueados.
- Cartas são determinísticas pelo dia global. O dia 1 entrega boas-vindas; o dia 2 de cada estação realiza uma feira com três sementes da cultura de afinidade; o dia 6 entrega o evento comunitário.
- A feira segue a ordem morango, milho, abóbora e cenoura para Primavera, Verão, Outono e Inverno.
- Um marcador dourado sobre a caixa indica cartas não lidas. Abrir a carta remove o marcador correspondente sem resgatar o anexo automaticamente.
- Anexos são transacionais e resgatáveis uma única vez. A validação de item e espaço na mochila acontece antes de dinheiro ou itens serem alterados.
- Saves antigos consideram cartas já entregues como lidas para evitar uma enxurrada de avisos, mas preservam todos os anexos como disponíveis.

### Expansão de terreno

- A fazenda começa com 9 canteiros. O caixote de comércio oferece duas expansões persistentes: 15 canteiros por $500 e 25 canteiros por $1.500.
- Os 9 canteiros originais nunca mudam de posição ou índice. A primeira compra acrescenta três canteiros em cada lateral; a segunda completa as faixas frontal e traseira.
- A compra é transacional: dinheiro e nível do terreno só mudam juntos. O botão fica indisponível sem recursos, fora do alcance ou no nível máximo.
- O painel informa antecipadamente custo e quantidade resultante. No último nível exibe `TERRENO MÁXIMO • 25 CANTEIROS`.
- Canteiros adicionados usam índices estáveis de 9 a 24. Plantio, irrigação, pragas, aspersores, construções e save continuam usando as mesmas regras.
- Saves anteriores à versão 19 iniciam no terreno de 9 canteiros, preservando todos os estados já existentes. Saves v19 reconstroem a quantidade correta antes de restaurar cada canteiro.

### Save

Formato atual: versão 19.

- Cada gravação é escrita primeiro em `farm-prototype-save.json.tmp`, validada e sincronizada no disco antes de substituir o arquivo principal.
- Ao substituir um save existente, a versão válida anterior passa a `farm-prototype-save.json.bak`. A primeira gravação não cria um backup vazio.
- Se o arquivo principal estiver ausente, vazio ou corrompido, o carregamento tenta o backup automaticamente e informa `Save recuperado do backup` no HUD.
- Se principal e backup estiverem inválidos, o jogo mantém o estado atual e apresenta os dois erros sem sobrescrever os arquivos.
- Arquivos temporários são removidos após falha; os testes de recuperação usam somente uma pasta isolada dentro de `Temp` e nunca alteram o save do jogador.

Dados persistidos:

- dinheiro;
- pilhas do inventário e depósito, incluindo a qualidade de cada pilha;
- estados e crescimento dos canteiros;
- oito entradas e seleção da hotbar;
- seis marcos da Primeira Colheita e entrega da recompensa.
- até 30 pilhas do depósito local.
- IDs dos pickups já coletados.
- número do dia e minutos decorridos no dia.
- semente determinística do mundo usada pela previsão do clima.
- as cotações não são persistidas: mundo, dia e `ItemId` reconstroem o mercado sem dados duplicados.
- níveis individuais da enxada, regador e ferramenta de colheita.
- métricas do diário, culturas já colhidas e IDs das recompensas resgatadas.
- dia do quadro de pedidos e máscara das três entregas concluídas.
- energia atual, limitada entre 0 e 100 ao carregar.
- XP das três trilhas de domínio e o dia em que `Primeiro fôlego` foi consumido.
- construções colocadas, com ID persistente, kit de origem, posição e rotação.
- último dia em que as automações da manhã foram processadas.
- IDs das cartas lidas e dos anexos já resgatados.
- IDs dos itens já descobertos para o livro de coleções.
- nível de expansão do terreno, que determina 9, 15 ou 25 canteiros.

Saves v1 e v2 recebem inventário/hotbar compatíveis. Saves v3 iniciam o guia limpo, v4 inicia o depósito vazio, v5 inicia pickups vazios, v6 começa em Dia 1 às 08:00, saves até v7 recebem a semente padrão do mundo, saves até v8 iniciam todas as ferramentas em L1, saves até v9 iniciam o diário vazio, saves até v10 iniciam o quadro diário sem entregas no dia carregado, saves até v11 começam com 100 de energia, saves até v12 iniciam as três trilhas de domínio no nível 1 com zero XP, saves até v13 iniciam sem construções colocadas, saves até v14 começam sem uma manhã de automação registrada, saves até v15 migram cartas entregues para lidas sem consumir anexos, saves até v16 inferem coleções pela posse e pelas culturas já colhidas, saves até v17 convertem todas as pilhas existentes para qualidade Normal e saves até v18 iniciam no terreno básico de 9 canteiros.

## Arquitetura

| Arquivo | Responsabilidade |
| --- | --- |
| `FarmGameState.cs` | Dinheiro, pilhas por qualidade, depósito, ordenação determinística persistente, pickups, hotbar, níveis, energia, expansão de terreno, tutorial, progresso diário, venda multicultura, migração, serialização, gravação atômica e recuperação por backup. |
| `FarmItemQuality.cs` | Regras puras de qualidade, combinação estação/domínio, nomes, marcas e multiplicadores de venda. |
| `FarmMarketRules.cs` | Hash estável, quatro faixas de cotação, previsão e composição do preço diário com qualidade. |
| `FarmTestPlot.cs` | Plantio, colheita com qualidade, grade expansível de índices estáveis, padrões de área, custos e exaustão, melhorias, catálogo, alvos, comércio, depósito, cama, quadro de pedidos, avanço noturno, pickups, marcos e ação ativa. |
| `FarmHudController.cs` | HUD e barra de energia, qualidade visual das pilhas, notificações, guia, filtros somente visuais da mochila, depósito, diário, pedidos, confirmação de descanso, transição de novo dia, hotbar, comércio, compra de terreno e drag visual. |
| `FarmInventoryUiInteractions.cs` | Eventos de ponteiro, tooltip, drag, drop e clique da UI. |
| `FarmStorageUiInteractions.cs` | Tooltip e transferência de pilha, metade ou unidade entre mochila e depósito. |
| `FarmWorldPickup.cs` | Flutuação, atração, coleta, capacidade e feedback dos itens no mundo. |
| `FarmDayClock.cs` | Simulação temporal, calendário anual derivado, conversão minutos/segundos, fases do dia, checkpoints e iluminação gradual. |
| `FarmWeatherSystem.cs` | Clima determinístico, previsão, efeito visual da chuva e integração com irrigação e iluminação. |
| `FarmEquipmentController.cs` | Prop correspondente à ação selecionada. |
| `FarmCrafting.cs` | Bancada, consumo transacional e UI de fabricação. |
| `CraftingRecipe.cs` | Definição persistente de ingredientes, resultado, prefab de referência e catálogo data-driven das receitas. |
| `FarmBuildableDefinition.cs` | Catálogo data-driven dos kits posicionáveis, prefabs, escala e áreas de ocupação. |
| `FarmBuildingSystem.cs` | Preview, validação espacial, rotação, confirmação, recolhimento, reconstrução persistente e execução das funções das construções. |
| `FarmBuildingCatalog.cs` | Janela modal, busca tolerante a acentos, categorias, paginação, estoque e transição segura para o preview. |
| `FarmBuildGridVisual.cs` | Malha procedimental local, transparente e alternável usada durante posicionamento e movimento. |
| `FarmMailboxSystem.cs` | Agenda determinística, caixa postal mundial, marcador de cartas não lidas, janela modal e resgate transacional de anexos. |
| `FarmCollectionBook.cs` | Catálogo data-driven, filtros, paginação, estados conhecido/oculto e janela modal das coleções. |
| `FarmSprinklerEmitter` | Círculo de cobertura e pulso visual do aspersor colocado ou em preview. |
| `FarmPestRules.cs` | Calendário previsível das visitas, atraso máximo e raio padrão do espantalho. |
| `FarmBuildableRadiusIndicator` | Indicador circular reutilizado por aspersores e espantalhos, com cor e raio data-driven. |
| `ItemDefinition.cs` | Identificador, nome, categoria, pilha e valor de um item. |
| `CropDefinition.cs` | Semente, produto, modelos, tempo, rendimento base, afinidade/bônus sazonal e preço de pacote. |
| `FarmContent.cs` | Catálogo central de itens/culturas e resolução de cultura por semente. |
| `FarmJournal.cs` | Métricas, progresso serializável e catálogo extensível de objetivos/recompensas. |
| `FarmDailyOrders.cs` | Geração determinística, dados de pedido, recompensa, bônus do quadro e progresso serializável do dia. |
| `FarmActionFeedback.cs` | Partículas procedurais por ferramenta, escala por área alterada e recompensa dourada para entregas/melhorias. |
| `FarmSettings.cs` | Preferências locais persistentes e janela modal de sensibilidade, zoom, inversão vertical e restauração dos padrões. |
| `FarmMastery.cs` | Dados, níveis, descrições, benefícios horizontais e janela modal das três trilhas de domínio. |

### Chaves da hotbar

- Ferramentas: `tool:hoe`, `tool:watering_can`, `tool:harvest`.
- Itens: `item:<ItemDefinition.Id>`.
- Entrada vazia: string vazia.

Essa representação é pequena, serializável e adequada para futura validação pelo host, sem armazenar referências de objetos da cena.

## Controles atuais

| Ação | Controle |
| --- | --- |
| Movimento | `WASD` ou setas |
| Correr | `Shift esquerdo` |
| Pular | `Espaço` |
| Girar câmera | Botão direito + mouse ou `Q/E` |
| Zoom | Roda do mouse |
| Interagir com estações | `F` próximo da loja, depósito, cama, pedidos, bancada ou caixa postal |
| Abrir/fechar opções | `F10`; `Esc` fecha |
| Abrir/fechar domínio | `K`; `Esc` fecha |
| Selecionar hotbar | `1–8` ou clique esquerdo |
| Abrir/fechar mochila | `I` ou `Tab` |
| Filtrar ou organizar a mochila | Botões `TODOS`/categoria e `ORGANIZAR` na mochila |
| Abrir/fechar diário | `J` |
| Abrir/fechar coleções | `L`, botão `COLEÇÕES`, `Esc` ou botão `FECHAR` |
| Fechar mochila | `Esc` |
| Usar no canteiro | Clique esquerdo |
| Abrir/fechar comércio | `F` próximo; `Esc` ou botão para fechar |
| Vender/comprar/melhorar | Botões da loja aberta |
| Abrir depósito | Clique no baú ou `F`, a até 2,2 unidades |
| Transferir pilha/metade/unidade | Clique esquerdo / `Shift` + clique esquerdo / clique direito no depósito |
| Dormir/encerrar o dia | Clique na cama ou `F`, a até 2,5 unidades; depois confirmar |
| Abrir/fechar pedidos diários | Clique no quadro ou `F` próximo; `Esc` fecha |
| Entregar pedido | Botão `ENTREGAR` no quadro, com os itens necessários |
| Abrir/fechar fabricação | `F` próximo da bancada; `Esc` fecha |
| Abrir/fechar caixa postal | `F` a até 2,2 unidades; `Esc` ou botão `FECHAR` |
| Abrir/fechar catálogo de construção | `B`, botão `CONSTRUIR`, `Esc` ou botão `FECHAR` |
| Posicionar kit selecionado | `G`; clique esquerdo confirma |
| Girar/cancelar posicionamento | `R` / `Esc` ou clique direito |
| Mostrar/ocultar malha de construção | `H` durante um preview |
| Mover construção existente | `M` mirando a até 6 unidades; clique confirma; `Esc` desfaz |
| Recolher construção | `X` mirando um objeto próximo |
| Salvar/carregar | `F5` / `F9` |

## Validação automatizada

Os testes MCP cobrem a hierarquia visual, mochila/depósito/diário/pedidos/opções/domínio/crafting/construção/caixa postal/coleções, alcance, transferência de pilha/metade/unidade, tooltips contextuais e limites da tela, modais, drag-and-drop, hotbar, ordenação determinística e idempotente de mochila/depósito, preservação de quantidades e atalhos, persistência da ordem, cinco filtros não destrutivos da mochila, três qualidades separadas, regras determinísticas estação/domínio, transferência exata, ordenação Ouro→Normal, multiplicadores de venda, consumo de menor qualidade em pedidos, colheita real Ouro, tooltip e migração v17, hash estável de mercado, quatro faixas, extremos Baixa/Pico, previsão de amanhã, composição cotação/qualidade, venda consolidada, proteção do depósito e reconstrução após save/load, gravação atômica com sincronização em disco, backup rotativo, recuperação após corrupção do principal, corrupção dupla e limpeza do temporário, quatro culturas, plantio por `ItemId`, restauração da cultura do canteiro, catálogo, compra contextual, morango S/M/L, rendimento de Primavera e fora da estação, presença anual nos pedidos, níveis L1/L2/L3, áreas 1/3/9, recorte nas bordas, sementes unitárias, colheitas mistas, custos de energia somente em ações válidas, exaustão sem bloqueio, recuperação ao dormir, seis emissores de partículas, ausência de efeito em ação inválida, materiais URP e recompensa visual integrada ao quadro, fundos insuficientes, nível máximo, métricas do diário, objetivo 3/4 e 4/4 culturas, cinco recompensas, resgate único, ciclo agrícola, onboarding, pickups, avanço temporal real, virada do dia, fases, curva de luz, legibilidade noturna, névoa desligada, quatro estações, ano novo, HUD e afinidades sazonais, chuva seletiva, previsão, sono, geração determinística de três pedidos, recompensas, bloqueio de duplicidade, bônus completo, renovação diária, pausa modal, limites e persistência das preferências de câmera, retomada do relógio, XP/níveis das três trilhas, ação diária gratuita, magnetismo de 4 unidades, replantio, excedente no depósito, previsão de pedidos, entrega usando estoque, três receitas persistentes após reinício, rollback sem materiais, consumo exato, preview e cancelamento, rotação, bloqueio de sobreposição, recolhimento, raio parcial do aspersor, prevenção de irrigação duplicada, evento das 06:00, visita determinística, atraso único de 2 segundos, proteção parcial/total, aviso antecipado, feedback circular, encaixe de cerca pelas extremidades, alinhamento reto e em 90 graus, colocação contínua, cancelamento sem consumo, persistência das conexões, catálogo modal de construção, busca sem acentos, categorias, paginação, estado vazio, estoque, bloqueio sem kit, transição para preview sem consumo, movimento com ID preservado, estoque invariável, rollback, tipo imutável, bloqueio de sobreposição, malha local de 42 segmentos, alternância visual, persistência da nova posição, agenda sazonal determinística, afinidade da feira de sementes, marcador de não lida, resgate único de dinheiro e itens, persistência das cartas, catálogo data-driven de 13 itens, cinco filtros, paginação 12+1, estados conhecido/oculto, memória após consumo, neutralidade econômica, migrações, save/load v18 e Console.

Helpers atuais em `.codex/`:

- `validate-storage-v5.json`;
- `pickup-v6-runtime-step1.json` e `pickup-v6-runtime-step2.json`;
- `pickup-v6-modal-step1.json`, `step2` e `step3`;
- `farm-v6-integration-step1.json` e `farm-v6-integration-step2.json`;
- `day-clock-v7-step1.json`, `step2` e `step3`;
- `farm-v7-integration-step1.json` e `farm-v7-integration-step2.json`;
- `validate-weather-v8.json`;
- `farm-v8-integration-step1.json` e `farm-v8-integration-step2.json`;
- `validate-multicrop-v8.json` e `validate-multicrop-shop-runtime.json`;
- `farm-v8-integration-step1-multicrop.json` e `farm-v8-integration-step2.json`;
- `validate-tool-upgrades-v9.json` e `validate-tool-upgrade-shop-v9.json`;
- `farm-v9-integration-step1.json` e `farm-v9-integration-step2.json`;
- `validate-journal-v10.json`;
- `journal-modal-step1.json`, `journal-modal-step2.json` e `journal-modal-step3.json`;
- `farm-v10-integration-step1.json` e `farm-v10-integration-step2.json`;
- `validate-sleep-cycle-v10.json`;
- `sleep-modal-step1.json`, `sleep-modal-step2.json` e `sleep-modal-step3.json`;
- `validate-calendar-v10.json`;
- `validate-season-affinity-v10.json`;
- `validate-daily-orders-v11.json`;
- `daily-orders-modal-step1.json`, `daily-orders-modal-step2.json` e `daily-orders-modal-step3.json`;
- `farm-v11-integration.json` e `final-sanity-v11.json`;
- `validate-energy-v12-final.json`;
- `farm-v12-integration.json`, `clean-v12-state.json`, `freeze-clean-v12.json` e `final-sanity-v12.json`;
- `validate-action-feedback.json` e `validate-action-feedback-integration-final.json`;
- `create-strawberry-assets.json`, `validate-strawberry-spring.json` e `validate-journal-four-crops.json`;
- `select-pumpkin-shop.json` e `final-sanity-strawberry-v12.json`;
- `settings-modal-step1-final.json`, `settings-modal-step2.json`, `settings-modal-step3.json` e `final-sanity-settings.json`;
- `validate-mastery-v13.json`, `mastery-modal-step1.json`, `mastery-modal-step2.json`, `mastery-modal-step3.json` e `final-sanity-mastery-v13.json`;
- `validate-portuguese-encoding-v13.json`, `validate-foundation-v13.json`, `validate-f-interactions-cozy.json` e `validate-crafting-runtime.json`;
- `Temp/farm-v13-integration.json`, `Temp/clean-v13-state.json`, `Temp/freeze-clean-v13.json`, `Temp/final-sanity-v13.json` e `Temp/final-sanity-strawberry-v13.json`;
- `Temp/validate-building-v14-fixed.json`, `Temp/validate-foundation-v14.json`, `Temp/validate-mastery-v14.json` e `Temp/clean-v14-state.json`;
- `Temp/configure-sprinkler-v14.json`, `Temp/validate-sprinkler-v14.json` e `Temp/show-sprinkler-v14.json`;
- `Temp/configure-buildable-functions-v15.json`, `Temp/validate-pests-v15.json`, `Temp/show-scarecrow-v15.json` e `Temp/clean-v15-state.json`;
- `Temp/validate-foundation-v15.json`, `Temp/validate-mastery-v15.json`, `Temp/validate-building-v15.json` e `Temp/validate-sprinkler-v15.json`;
- `Temp/configure-fence-v15.json`, `Temp/validate-fence-snap-v15.json`, `Temp/show-fence-snap-v15.json` e `Temp/recreate-crafting-assets-v15.json`;
- `Temp/configure-building-catalog-v16.json`, `Temp/validate-building-catalog-v16.json` e `Temp/show-building-catalog-v16.json`;
- `Temp/validate-building-move-grid-v16.json` e `Temp/show-building-move-grid-v16.json`;
- `Temp/validate-mailbox-v17.json`, `Temp/show-mailbox-v17.json` e `.codex/run-regression-v17.ps1`;
- `Temp/validate-inventory-tooltip-split-v18.json`, `Temp/show-inventory-tooltip-v18.json` e `.codex/run-regression-v18.ps1`;
- `Temp/validate-collection-v19.json`, `Temp/show-collection-v19.json` e `.codex/run-regression-v19.ps1`;
- `Temp/validate-inventory-organization-v20.json`, `Temp/show-inventory-organization-v20.json` e `.codex/run-regression-v20.ps1`;
- `Temp/validate-save-safety-v21.json` e `.codex/run-regression-v21.ps1`;
- `Temp/validate-crop-quality-v22.json`, `Temp/show-crop-quality-v22.json` e `.codex/run-regression-v22.ps1`;
- `Temp/validate-market-v23.json`, `Temp/show-market-v23.json` e `.codex/run-regression-v23.ps1`;
- `Temp/validate-land-v24.json`, `Temp/show-land-v24.json` e `.codex/run-regression-v24.ps1`;
- `open-inventory-visual-test.json`.

Capturas de referência desta iteração:

- `Temp/farm-inventory-background.png` — mochila e drag-and-drop;
- `Temp/farm-tutorial-v4.png` — HUD e guia Primeira Colheita.
- `Temp/farm-storage-v5.png` — comparação e transferência mochila/depósito.
- `Temp/farm-pickup-toast-v6.png` — pickups no mundo e feedback de aquisição.
- `Temp/farm-day-clock-dusk-v7-clean.png` — relógio e iluminação às 19:00.
- `Temp/farm-rain-weather-v8-final.png` — chuva, previsão e leitura visual do cenário.
- `Temp/farm-multicrop-shop-v8.png` — catálogo de cenoura e três culturas Synty nos canteiros.
- `Temp/farm-tool-upgrade-v9.png` — melhoria L2 da enxada, custo, hotbar e linha de três destacada.
- `Temp/farm-journal-v10.png` — diário com estados resgatado, disponível e em progresso.
- `Temp/farm-sleep-confirm-v10.png` — confirmação de descanso às 21:00, com contexto de relógio e clima.
- `Temp/farm-calendar-v10.png` — HUD completo no Outono 2/7, com relógio, clima e hotbar legíveis.
- `Temp/farm-season-affinity-v10.png` — catálogo da abóbora no Outono, exibindo bônus sazonal antes da compra.
- `Temp/farm-daily-orders-v11.png` — quadro com estados entregue, disponível e incompleto, recompensa e bônus diário.
- `Temp/farm-energy-exhausted-v12.png` — energia zerada, aviso de cansaço e ferramenta L3 reduzida a um canteiro.
- `Temp/farm-action-feedback.png` — cinco efeitos simultâneos sobre a grade, com paletas distintas e material URP.
- `Temp/farm-strawberry-spring.png` — morango no inventário/hotbar, Primavera e grade de referência com estágios S/M/L.
- `Temp/farm-settings-menu.png` — opções locais com acentuação, valores, inversão vertical e leitura do cenário ao fundo.
- `Temp/farm-mastery-v13.png` — três trilhas no nível máximo, benefícios horizontais e atalhos do HUD.
- `Temp/farm-cozy-day.png` e `Temp/farm-cozy-night-pass2.png` — paleta viva sem névoa e piso de legibilidade noturna.
- `Temp/farm-crafting-v13.png` — bancada modal com três receitas data-driven e estados de materiais.
- `Temp/farm-building-v14.png` — espantalho, aspersor e cerca colocados no cenário pelo sistema de construção.
- `Temp/farm-sprinkler-v14.png` — área circular de 5 metros, seis canteiros regados e mensagem do evento matinal.
- `Temp/farm-scarecrow-v15.png` — aviso antecipado dos corvos e raio amarelo de 6,5 metros do espantalho.
- `Temp/farm-fence-snap-v15.png` — encaixe dourado entre seções de cerca, incluindo continuidade e canto de 90 graus.
- `Temp/farm-building-catalog-v16.png` — catálogo modal com busca, categorias, três cartões, custo e estoque disponível.
- `Temp/farm-building-move-grid-v16.png` — preview de movimento, malha local de 0,5 unidade e instruções de confirmação/rollback.
- `Temp/farm-mailbox-v17.png` — caixa postal modal com agenda sazonal, lista de cartas e anexo da feira de sementes.
- `Temp/farm-inventory-tooltip-v18.png` — mochila com quatro categorias e tooltip contextual de sementes.
- `Temp/farm-collection-v19.png` — livro modal com progresso, cinco filtros e cartões conhecidos/ocultos.
- `Temp/farm-inventory-organization-v20.png` — mochila filtrada por projetos, ordenação explícita e tooltip contextual.
- `Temp/farm-crop-quality-v22.png` — pilhas Normal/Prata/Ouro, cores, símbolos, ordem e valor unitário contextual.
- `Temp/farm-market-v23.png` — caixote ampliado com cotação Pico, previsão, três preços de qualidade e sementes.
- `Temp/farm-land-v24.png` — terreno N2 com 15 canteiros e compra antecipada do N3 no caixote.

## Decisões de direção

1. A mochila é individual por enquanto; um depósito será um sistema separado. Isso evita misturar autoridade multiplayer com a primeira implementação local.
2. A hotbar guarda atalhos, não itens. Consumir a última unidade remove automaticamente o atalho correspondente, evitando ações com quantidade zero.
3. Telas modais bloqueiam ações do personagem. A regra será reutilizada por loja completa, depósito, diário e menus.
4. O onboarding registra ações em qualquer ordem, respeitando a curiosidade do jogador.
5. Energia cria escolhas e bônus de planejamento; zero reduz ferramentas de área a um canteiro, mas não bloqueia ações nem encerra o dia abruptamente.
6. Dormir é uma decisão explícita na cama; culturas secas não avançam e cada manhã gera clima e save coerentes.
7. Pedidos diários pagam acima da venda comum para orientar metas curtas, sem tornar o caixote de comércio inútil.
8. Geração diária é determinística e sorteia três culturas distintas entre as quatro disponíveis; o bônus exige concluir o quadro inteiro, criando variedade sem aleatoriedade irreproduzível.
9. Cada estação possui uma cultura de afinidade: morango, milho, abóbora e cenoura formam o primeiro conjunto sazonal completo.
10. Preferências de câmera pertencem à instalação local, não ao mundo salvo. Isso permite trocar de save sem perder conforto e prepara a futura separação de volumes por categoria.
11. Domínio recompensa prática real e resolve atritos do ciclo: continuidade, informação e ligação entre sistemas. Não existe multiplicador universal de preço ou rendimento; Colheita influencia qualidade apenas em combinação transparente com a estação.
12. Estações nunca abrem apenas por proximidade. `F` expressa intenção de interagir; `Esc` ou um botão explícito encerram o modal.
13. Noite e clima devem mudar a atmosfera sem comprometer navegação. Legibilidade do cenário é um requisito funcional, não apenas estético.
14. Crafting consome recursos de forma atômica; construção consome o kit somente após uma confirmação espacial válida.
15. A ocupação de construções vem de dados próprios, não dos colliders dos assets, para manter regras previsíveis entre pacotes diferentes.
16. Equipamentos automáticos atuam em um evento explícito de manhã e usam a mesma área exibida ao jogador; automação nunca deve esconder alcance ou contabilizar efeito duplicado.
17. Pragas criam planejamento sem apagar progresso: calendário visível, consequência pequena, alvo determinístico e proteção espacial clara.
18. Cercas usam encaixe previsível pelas extremidades e permitem fluxo contínuo; o jogador recebe confirmação visual antes de gastar cada kit.
19. O catálogo mostra projetos mesmo sem estoque para comunicar possibilidades; selecionar e confirmar são etapas distintas, portanto navegar nunca consome recursos.
20. Mover é uma edição do mesmo objeto, não uma venda e recompra: identidade e estoque permanecem invariáveis, e cancelar restaura o estado anterior.
21. A caixa postal concentra eventos de médio prazo sem exigir NPCs completos. Agenda e cartas são derivadas do dia, enquanto somente leitura e resgate ocupam espaço no save.
22. Ler e resgatar são ações separadas. Isso mantém a notificação honesta, permite guardar anexos e torna o ganho de recursos explicitamente transacional.
23. Dividir uma transferência é mais útil do que criar pilhas duplicadas dentro da mesma mochila: metade fica de cada lado sem introduzir identidade artificial de slots no save.
24. Coleções registram posse histórica, não disponibilidade atual. O livro informa e incentiva exploração sem se transformar em outra fonte obrigatória de recompensas.
25. Filtros são uma lente temporária sobre a mochila; ordenar é uma ação explícita e persistente. Assim, explorar categorias nunca muda o save, enquanto a organização escolhida pelo jogador sobrevive ao carregamento.
26. Progresso nunca depende de uma única escrita bem-sucedida: a troca do arquivo é atômica e o último estado confirmado permanece como backup recuperável.
27. Qualidade deve recompensar planejamento, não sorte ou trabalho repetitivo. Pedidos gastam as menores primeiro e Ouro exige estação correta mais domínio máximo, mantendo a decisão sob controle do jogador.
28. Mercado cria uma decisão entre liquidez e espera, não uma loteria: sequência determinística, amanhã visível e depósito protegido tornam o risco legível e reversível.
29. Expansão acrescenta capacidade sem invalidar trabalho anterior: canteiros originais não se movem, novos índices são estáveis e cada compra informa exatamente custo e resultado.

## Próximos ciclos recomendados

1. Expandir o catálogo com decoração e estruturas funcionais somente depois de definir seus ciclos de jogo, evitando conteúdo puramente visual sem propósito.
2. Adicionar áudio quando houver um pacote sonoro licenciado, mantendo volumes por categoria configuráveis.
3. Animar ferramentas quando houver clips adequados.
4. Revisar cada modal e menu para garantir que não pause o relógio ou o mundo.
5. Depósito e estufa.
6. Automação leve e máquinas — apenas após o núcleo de cultivo estar divertido.

Steam P2P e sincronização remota continuam fora destes ciclos, mas as regras de
gameplay precisam ser compatíveis com uma fazenda de 1–4 pessoas. Em especial,
nenhum modal ou menu individual deve pausar o relógio ou o mundo.
