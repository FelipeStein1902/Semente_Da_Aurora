# Semente da Aurora: Nível 1 (Vale das Cinzas)

Projeto Unity **6000.0.8f1** (URP) do 1º bimestre, feito com base no `gdd.pdf`.

## Como abrir

1. No Unity Hub, clique em **Add → Add project from disk** e escolha a pasta `SementeDaAurora`.
2. Abra o projeto com o editor **6000.0.8f1**.
3. Abra `Assets/Cenas/MenuInicial.unity` e aperte **Play**.

As cenas, prefabs, materiais, tags e o Build Settings já estão prontos no repositório.
Na primeira abertura a Unity só reconstrói a pasta `Library` (pode demorar alguns minutos).

> Se o Unity perguntar sobre o *Active Input Handling*, escolha **Input Manager (Old)** ou **Both**.
> Os scripts usam o `Input` clássico, como nos tutoriais da disciplina.

## Controles

| Ação | Comando |
|---|---|
| Andar | W, A, S, D |
| Correr | Shift (segurar) |
| Pular | Espaço |
| Câmera | Mouse |
| Menu/pausa | Esc ou botão "Menu" |
| Concluir o nível (só no editor) | F9 |

## Estrutura

```
Assets/
  Scripts/        scripts do jogo (documentados com ///)
  Editor/         TrocarAssets e LimparPacks (ferramentas de editor já usadas na troca dos modelos)
  Cenas/          MenuInicial, Introducao, Nivel1
  Prefabs/        casas, moinho, poço, barris, árvores, runas...
  Prefabs/Mudas/  5 flores/mudas do vetor de spawn
  Materiais/      materiais URP Lit, partículas e céus
  Animacoes/      Bran.controller: estados Parado, Andando, Correndo, Pulando, Caindo
  Audio/          musica.wav, queda.wav, impacto.wav, restauracao.wav (placeholders)
  Resources/      AudioManager.prefab (sons configurados)
```

| Script | O que faz |
|---|---|
| `PlayerController` | Andar/correr (`transform.Translate`), pulo (`AddForce` Impulse), gravidade ×2 na queda, 50% de controle no ar, detecção de chão, som e poeira ao pousar |
| `PlayerAnimacao` | Envia `Velocidade`, `NoChao` e `VelocidadeVertical` para o Animator |
| `CameraTerceiraPessoa` | Segue o jogador, gira com o mouse e se aproxima quando há obstáculos |
| `GameManager` | Fase atual, troca de cenas com fade, `CompletarFase()`, `ReiniciarJogo()` |
| `UIManager` | HUD "Fase X / 5", pausa (Continuar/Reiniciar/Sair), texto das runas, tela de nível concluído |
| `ZonaAbismo` | Trigger (Tag `Abismo`): som de queda e volta ao nível 1 |
| `PlataformaReflorescente` | Na primeira vez que o jogador pousa, troca o material de cinza para verde e cria mudas |
| `SpawnerMudas` | Vetor `GameObject[]` + `Random.Range` + `Instantiate` (`SpawnarMuda`) |
| `MoinhoDeVento` | Pás com `Rigidbody` + `HingeJoint` girando por `AddTorque` |
| `RodaDagua` | Roda d'água com `transform.Rotate`, ligada ao despertar a Nascente |
| `NascenteFinal` | Ponto final (Tag `Final`): explosão de luz, água, apaga a fumaça, roda gira, árvores ficam verdes, céu clareia |
| `PedraRunica` | Sistema de ajuda (Tag `Runa`): mostra/esconde o texto |
| `AudioManager` | Música em loop e efeitos (`TocarSom`) |
| `MenuInicial`, `TelaIntroducao`, `CameraOrbita` | Menu inicial, falas da Cena 1 e câmera girando no fundo do menu |

## O nível 1

O caminho vai da praça (oeste) até a Nascente (leste), sempre subindo:

1. **Praça da Vila**: chão firme, 5 runas do tutorial, casas, moinho, poço, roda d'água parada.
2. **Ponte Partida**: plataformas largas e próximas sobre o rio seco; runa de história.
3. **Colina dos Moinhos**: plataformas mais afastadas. **Atalho:** degraus à esquerda levam à ponta de
   uma pá do moinho grande; ela sobe, você escorrega até o eixo e pula para a plataforma de saída.
   **Segredo:** atrás do moinho, um caminho leva à **pedra rúnica dourada**.
4. **Chaminé da Forja**: plataformas pequenas e distantes (~4,2 m). É **preciso correr** antes de pular.
5. **Nascente do Vale**: tocar a fonte restaura o vale e mostra a tela de nível concluído.

Alcance do pulo com os valores padrão: cerca de 4 m andando e 7 m correndo, no mesmo nível.
Para ajustar a dificuldade, mova as plataformas na cena ou mude `forcaPulo`, `velocidadeAndar` e
`velocidadeCorrer` no `PlayerController`.

## Modelos e sons: o que trocar

Os modelos vêm dos packs da Asset Store em `Assets/` (Polytope Studio, Farland Skies, GVOZDY etc.)
e os sons ainda são placeholders gerados por código. A lógica não depende deles, então dá para
trocar sem mexer nos scripts:

- **Bran (Mixamo):** coloque o modelo como filho de `Bran/Modelo` (ou troque o `Modelo` inteiro),
  mantenha o `Animator` com `Assets/Animacoes/Bran.controller` e arraste os clipes do Mixamo para os
  estados *Parado, Andando, Correndo, Pulando e Caindo*. Apague as cápsulas do placeholder.
- **Casas, árvores, barris etc.:** abra o prefab em `Assets/Prefabs` e troque o conteúdo pelo modelo
  real. Todas as instâncias nas cenas são atualizadas.
- **Flores/mudas:** troque os prefabs em `Assets/Prefabs/Mudas` ou adicione mais no vetor
  `prefabsMudas` do objeto `SpawnerMudas` na cena.
- **Sons:** troque os clipes no prefab `Assets/Resources/AudioManager`.
- **Textos das runas:** cada `PedraRunica` na cena tem o campo `texto` no Inspector.

## Tags

`Player`, `Plataforma`, `Abismo`, `Final`, `Runa`.
