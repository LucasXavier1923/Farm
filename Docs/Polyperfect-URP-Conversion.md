# Conversão Polyperfect para URP

Data da conversão: 26/07/2026

## Escopo

- Pasta tratada: `Assets/polyperfect`
- Pacote encontrado: `Poly Universal Pack`
- Cenas de demonstração: 9
- Materiais encontrados: 94
- Render pipeline do projeto: Universal Render Pipeline (URP)

## Conversão realizada

Foram atualizados 93 materiais incompatíveis por meio dos conversores oficiais da Unity:

| Quantidade | Shader original | Shader URP |
|---:|---|---|
| 77 | `Standard (Specular setup)` | `Universal Render Pipeline/Lit` |
| 9 | `Standard` | `Universal Render Pipeline/Lit` |
| 4 | `Nature/SpeedTree` | `Universal Render Pipeline/Nature/SpeedTree7` |
| 3 | `Particles/Standard Unlit` | `Universal Render Pipeline/Particles/Unlit` |

O material `Skybox/Procedural` foi mantido sem alteração porque continua compatível.

## Validação

- 94 materiais válidos e nenhum usando `Hidden/InternalErrorShader`.
- 9 de 9 cenas com todos os materiais dependentes usando shaders suportados.
- 218 referências de materiais verificadas entre as cenas.
- A cena `07 Farm` foi restaurada como cena ativa, sem alterações pendentes.
- Console da Unity verificado ao final: zero erros e zero avisos.

## Observação sobre a cena Graveyard

Ao abrir `05 Graveyard`, a própria cena informa uma referência antiga para um prefab de terreno que não veio no pacote:

- objeto: `terrain`
- GUID ausente: `489c11a966bff66408570083e87a7937`

Isso não está relacionado aos materiais rosas e não impede a conversão dos shaders. A referência não foi removida automaticamente para preservar a cena original comprada. Caso essa cena seja usada no jogo, o terreno deve ser substituído por outro prefab ou recriado.

## Cenas verificadas

1. `01 All Models`
2. `02 Steampunk`
3. `03 Survival`
4. `04 Mumuland`
5. `05 Graveyard`
6. `06 Movie Set`
7. `07 Farm`
8. `08 Fantasy Battlefield`
9. `09 Poly Cars Pack`
