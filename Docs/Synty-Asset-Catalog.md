# Catálogo Synty / Polygon Farm

Inventário inicial dos assets relevantes para as mecânicas de fazenda. Os caminhos abaixo são caminhos do projeto Unity.

## Situação atual das ferramentas

O prop `Player/Hoe_Test` foi uma prova visual: foi instanciado permanentemente como filho do `Player`, por isso fica sempre visível, não troca por ferramenta e não acompanha a mão/animação. Ele deve ser substituído por um sistema de equipamento que instancie apenas a ferramenta ativa no osso da mão direita.

O projeto contém `Assets/Synty/Tools/SyntyPropBoneTool/`, com configurações para `POLYGONRig_01`. Esse é o recurso recomendado para conectar props ao rig; a configuração exata de posição/rotação deve ser ajustada visualmente depois de equipar cada prop.

## Ferramentas de mão

Todos possuem prefab em `Assets/PolygonFarm/Prefabs/Props/`.

| Uso | Prefab |
| --- | --- |
| Enxada | `SM_Prop_Tool_Hoe_01.prefab` |
| Regador | `SM_Prop_Watering_Can_01.prefab` |
| Pá | `SM_Prop_Tool_Spade_01.prefab` |
| Ancinho | `SM_Prop_Tool_Rake_01.prefab` |
| Foice | `SM_Prop_Tool_Scythe_01.prefab` |
| Machado | `SM_Prop_Tool_Axe_01.prefab` |
| Forquilha | `SM_Prop_Tool_Pitchfork_01.prefab` |
| Forquilha de feno | `SM_Prop_Tool_Hayfork_01.prefab` |
| Balde | `SM_Prop_Tool_Bucket_01.prefab` |

## Culturas e colheitas

Os modelos ficam em `Assets/PolygonFarm/Models/` e, quando disponíveis, há prefabs correspondentes em `Assets/PolygonFarm/Prefabs/Props/`. O padrão de estágio é `_S`, `_M`, `_L` e `_Group`.

- Abóbora: `SM_Prop_Pumpkin_01`, `SM_Prop_Pumpkin_White_01`, `SM_Prop_Pumpkin_Italian_01`.
- Cenoura: `SM_Prop_Carrot_01`, `_S`, `_M`, `_L`, `_Group`.
- Batata, tomate, morango, repolho, beterraba, brócolis, feijão, aspargo, pimenta, pepino, cebola, beringela, melancia e diferentes abóboras.
- Itens de venda/estoque: caixas `SM_Prop_Box_<Cultura>_01`, sacos de grãos e pacotes de sementes `SM_Prop_SeedPacket_01/02`.

## Efeitos e máquinas

- Coleta de trigo: `Assets/PolygonFarm/Prefabs/FX/FX_Wheat_Collection_01.prefab`.
- Veículos: `SM_Veh_Harvester_01`, `SM_Veh_Pickup_01` e `SM_Veh_Attach_Planter_01` em `Assets/PolygonFarm/Prefabs/Vehicles/`.
- Irrigação e cenário: `SM_Prop_Sprinkler_01`, `SM_Prop_Sprinkler_Hose_01`, poço, torre d'água e moinho.

## Animações instaladas

Fontes: `Assets/Synty/AnimationBaseLocomotion/` e `Assets/Synty/AnimationIdles/`.

- Disponíveis e já utilizados: idle, walk, run e jump para Polygon masculino.
- Não encontradas nestes dois pacotes: animações de enxada, regador, plantio, colheita, pegar objeto ou trabalho rural.
- Há idles sociais/expressivos, como `PickNose`; eles não são adequados às mecânicas da fazenda.

## Próxima implementação recomendada

1. Trocar o prop fixo por um `ToolEquipmentController` com um slot na mão direita.
2. Equipar visualmente enxada, sementes, regador e mãos vazias conforme a ferramenta selecionada.
3. Adquirir ou criar animações de ação compatíveis com `POLYGONRig_01` antes de ligar a interação do canteiro a elas.
4. Usar os modelos de cultura `_S`, `_M` e `_L` no lugar das primitivas do teste de cultivo.
