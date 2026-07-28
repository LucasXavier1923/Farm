# Arquitetura futura — backend autoritativo para inventário e plantio

Data: 26/07/2026

> Este documento registra uma direção técnica futura. A direção ativa está em
> `GDD.md` e `Systems-Foundation.md`: consolidar a base cooperativa 3D.
> O conteúdo abaixo não substitui o save local nem bloqueia o core loop atual.

## Status

### Implementation update — 26 July 2026

This update supersedes the historical coverage note below. The in-memory
`IFarmBackend` mock now authoritatively confirms `PrepareSoil`, `PlantSeed`,
`WaterTile`, `HarvestTile`, and hotbar assignment, selection, and slot swaps.

For harvests, the mock privately owns the crop-ready timestamp, seasonal yield
bonus, quality, inventory-capacity check, and automatic replant seed cost. The
Unity scene only applies the returned inventory/tile snapshots and harvest
result after the command succeeds. Replayed command IDs remain idempotent.

World time, visual crop progression, and weather remain host-local simulation
for this phase. A future Steam transport must replicate confirmed host
snapshots/results; non-host peers must not mutate a tile or inventory locally.

Há um mock e contratos assíncronos em C# como prova de conceito para uma futura
integração autoritativa. Eles devem ser mantidos isolados, sem forçar a migração
dos sistemas de gameplay enquanto o loop cooperativo ainda está em produção.

### Cobertura atual do mock

O mock valida e confirma `PrepareSoil`, `PlantSeed` e `SetHotbar`. Compra/venda
e votos de sono já possuem fronteiras de sessão idempotentes próprias. Rega,
crescimento e colheita continuam locais no protótipo atual; não devem ser
tratados como sincronizados até receberem snapshots de estágio/tempo de cultivo
e resultados de colheita confirmados pelo host. Essa limitação é intencional e
registrada para evitar que uma futura integração Steam assuma autoridade que o
mock ainda não fornece.

## Decisão de produto

O jogo continua uma experiência de fazenda **3D em terceira pessoa com câmera isométrica livre**, usando o protótipo atual como base jogável. A referência a Stardew Valley vale para o ritmo acolhedor, o loop de cultivo e a clareza dos sistemas — não para transformar o projeto em 2D.

O jogo terá dois modos de conexão distintos:

- uma fazenda instanciada, em co-op de até quatro pessoas via Steam P2P;
- uma economia global assíncrona, validada por serviços remotos.

Não haverá mundo aberto compartilhado. Movimento, física e apresentação pertencem à instância da fazenda. Dinheiro, inventário, habilidades, cultivos persistentes e operações de mercado pertencem ao backend.

Nesta fase, Steamworks, lobbies, Supabase, autenticação e mercado global **não serão implementados**. A meta é uma fatia vertical de inventário e plantio que usa a mesma fronteira de autoridade que será usada por eles.

## Regra de autoridade

O cliente nunca calcula nem persiste um resultado econômico. Ele envia uma intenção com um identificador idempotente e aplica ao visual somente o estado confirmado na resposta.

```text
UI / input
  -> pedido assíncrono
  -> IFarmBackend
  -> validação autoritativa
  -> snapshot confirmado
  -> estado de apresentação e UI
```

O mock é a implementação atual de `IFarmBackend`. No futuro, o adaptador Supabase chamará Edge Functions; o contrato C# e a UI não devem conhecer HTTP, SQL ou chaves de acesso.

## Primeiro fluxo obrigatório: plantar

1. O jogador seleciona uma semente e clica em um canteiro preparado.
2. A apresentação mostra apenas um estado temporário de processamento; não consome a semente nem cria a planta.
3. O cliente envia `PlantSeedRequest` com jogador, fazenda, canteiro, semente e `commandId` único.
4. O backend valida identidade, posse da semente, estado do canteiro e duplicação do comando.
5. Em caso de sucesso, responde com o snapshot atualizado do inventário e do canteiro.
6. Só então o cliente atualiza a hotbar, a mochila e a representação visual do cultivo.
7. Em caso de falha, nada muda no mundo; a UI exibe a razão devolvida pelo backend.

## Contratos C# planejados

```csharp
public interface IFarmBackend
{
    Task<InventorySnapshot> GetInventoryAsync(CancellationToken cancellationToken);
    Task<CommandResult<HotbarSnapshot>> AssignHotbarAsync(
        AssignHotbarRequest request, CancellationToken cancellationToken);
    Task<PlantSeedResult> PlantSeedAsync(
        PlantSeedRequest request, CancellationToken cancellationToken);
}
```

`InventorySnapshot`, `HotbarSnapshot` e `FarmTileSnapshot` serão DTOs imutáveis: dados para leitura e apresentação, nunca referências para `GameObject` ou `MonoBehaviour`.

`CommandResult<T>` transportará `Succeeded`, `FailureCode`, mensagem apresentável com segurança e o estado confirmado. Comandos terão `commandId` para que repetição de rede não duplique consumo ou plantio.

## Mock de backend

O mock terá sua própria cópia privada do estado autoritativo em memória e introduzirá atraso configurável. Ele deverá:

- recusar semente inexistente, quantidade insuficiente, canteiro não preparado e comando repetido;
- consumir exatamente uma semente em uma transação em memória;
- criar o cultivo somente ao confirmar o consumo;
- devolver snapshots completos após cada comando;
- permitir simular falhas e latência para validar a UI;
- não usar `PlayerPrefs`, JSON de save ou o `FarmGameState` legado como fonte de verdade.

Reiniciar o mock reinicia a sessão de teste. Isso é intencional nesta etapa: persistência virá do backend remoto.

## UI e drag-and-drop

Drag-and-drop continua puramente visual durante o gesto. Ao soltar um item na hotbar, a UI entra em estado pendente e chama `AssignHotbarAsync`; ao soltar uma entrada da própria hotbar em outra, chama `SwapHotbarSlotsAsync`. O atalho ou a organização só muda quando o backend devolver sucesso. Enquanto houver pedido pendente, o slot de origem/destino não aceita uma segunda operação conflitante. Para a fase de protótipo, um item arrastado do depósito é transferido para a mochila antes de a atribuição ser validada; o Mock recebe o espelho da mochila para validar a posse.

O mesmo padrão vale para plantar: mostrar carregamento/feedback, aguardar `await`, aplicar resultado, atualizar a UI a partir do snapshot.

## Fronteira futura com Supabase

Quando a integração começar:

- Unity usará apenas a chave pública apropriada e o token do jogador autenticado; uma chave `service_role` nunca entra no cliente.
- Cada operação sensível será uma Edge Function curta e idempotente, por exemplo `farm-command`.
- A função valida identidade, dono/membro da fazenda e versão/estado esperado antes de alterar PostgreSQL.
- A transação do banco consome item e altera canteiro no mesmo commit.
- As tabelas expostas usarão RLS; o cliente não terá permissão direta para mudar dinheiro, inventário ou estado de cultivo.

Edge Functions são adequadas para centralizar endpoints autenticados e validação no servidor; a documentação atual também recomenda operações curtas e idempotentes. [Documentação oficial](https://supabase.com/docs/guides/functions)

## Migração do protótipo existente

O protótipo 3D atual contém `FarmGameState` e `FarmSaveSystem`, que armazenam inventário e dinheiro localmente e mutam o estado de forma síncrona. Eles não podem ser usados como fonte de verdade na nova fatia.

Para preservar o protótipo atual, a primeira implementação será integrada à cena 3D existente de forma incremental. A camada autoritativa substitui a fonte de verdade de inventário e plantio, enquanto câmera, personagem, grade, modelos e apresentação 3D continuam sendo reutilizados. Depois que o fluxo estiver validado, os sistemas legados serão migrados por domínio — inventário primeiro, depois plantio, colheita, comércio e progresso.

## Fora desta fatia

- Steamworks, lobby, sincronização de outros jogadores e P2P;
- Supabase real, autenticação, banco, Edge Functions e RLS;
- mercado global e compra/venda entre jogadores;
- crescimento, rega, colheita, crafting, moeda e progressão;
- migração completa do protótipo 3D.

## Domínios futuros que exigem autoridade remota

Especializações com pontos limitados, ordens do Mercado Global, modificadores
econômicos de eventos globais e contratos com recompensa em garantia pertencem
ao backend. Eles serão projetados depois do core cooperativo local, conforme
`Docs/Future-Global-Economy.md`; nenhum deles deve ser implementado por save
local ou por valores decididos pelo cliente.
