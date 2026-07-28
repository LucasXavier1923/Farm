# Visão futura — especialização e economia global

Este documento preserva ideias de produto para depois da base cooperativa 3D.
Nada aqui é requisito da fase atual e não deve introduzir Steamworks, Supabase
ou rede no protótipo antes da hora.

## Papel no jogo

Cada fazenda é uma instância cooperativa de até quatro pessoas. A economia
global conecta essas instâncias de modo assíncrono: ela cria decisões de longo
prazo e sensação de mundo vivo sem exigir mundo aberto compartilhado.

## 1. Especialização estrita

Cada personagem recebe um orçamento limitado de pontos de habilidade. As trilhas
iniciais candidatas são Agricultura, Ferraria, Pesca e Exploração/Mineração.

- Uma pessoa não alcança o pico de todas as trilhas na mesma temporada.
- Uma equipe se beneficia ao dividir funções e trocar produtos localmente.
- A fazenda solo continua capaz de completar o loop básico; especialização é
  vantagem de eficiência e acesso a projetos avançados, não uma punição que
  impeça o jogador de cultivar ou progredir.
- Reespecialização, caso exista, deve ter custo e tempo claros para preservar
  decisões sem prender permanentemente uma pessoa iniciante.

Antes de implementar, definir: quantidade total de pontos, marcos por trilha,
benefícios exclusivos, limites de troca local e como o progresso é persistido.

## 2. Mercado Global assíncrono

O mercado usa ordens, não troca direta entre jogadores conectados:

1. Vendedor publica item, quantidade, preço e prazo.
2. O backend reserva os itens na mesma transação que cria a ordem.
3. Comprador executa a ordem; o backend debita ouro, transfere itens e paga o
   vendedor de forma atômica.
4. Ordens expiradas devolvem itens reservados ao proprietário.

O cliente nunca modifica ouro, inventário, preço ou saldo de uma ordem por
conta própria. Esse recurso exigirá Supabase/PostgreSQL, autenticação, RLS,
Edge Functions idempotentes, trilha de auditoria e proteção contra duplicação.

O Mercado da Comunidade Steam continua separado: se existir, será limitado a
cosméticos sem poder de gameplay.

## 3. Clima e eventos globais

O backend publicará uma agenda de eventos com início, fim, regiões/escopo e
efeitos econômicos. Possibilidades: seca reduzindo certa oferta, praga elevando
demanda por insumos de proteção, ou safra excepcional reduzindo preços.

Princípios:

- previsões e duração devem ser legíveis dentro do jogo;
- nenhum evento pode destruir progresso ou tornar a fazenda iniciante inviável;
- o clima visual da instância pode continuar local, enquanto o modificador de
  economia é global e autoritativo;
- multiplicadores e preços são calculados no backend, nunca no cliente.

## 4. Contratos entre jogadores

Contratos são missões publicadas por jogadores com entrega verificável.

- O criador define recurso, quantidade, recompensa e prazo.
- A recompensa fica em escrow ao publicar; não pode ser gasta duas vezes.
- A entrega é validada de forma atômica contra o inventário do executor.
- Cancelamento e expiração obedecem regras explícitas e auditáveis.
- Contratos não podem exigir itens impossíveis ou burlar limites de mercado.

## Ordem de implementação futura

1. Finalizar o core cooperativo local e a política de sono/tempo da sessão.
2. Implementar identidade de jogador, fazenda e inventário autoritativo.
3. Criar mercado de ordens simples, sem eventos ou contratos.
4. Adicionar especializações persistentes e balanceadas.
5. Publicar eventos globais de leitura e seus modificadores econômicos.
6. Adicionar contratos com escrow e auditoria.

Cada etapa deve ter telemetria, testes de transação e ferramentas de suporte
antes de liberar a próxima.
